using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Cryptography;

namespace PersonalTools.MarkdownToWord;

/// <summary>预留同目录的短结果名；只在独占事务流中核对并提交原文件，从而消除检查与覆盖之间的竞态。</summary>
internal static class WordFieldFile
{
    private const int ShareDenyWrite = 0x20;
    private const int ExclusiveTransaction = 0x10000 | 0x10 | 2; // STGM_TRANSACTED | STGM_SHARE_EXCLUSIVE | STGM_READWRITE
    private const int OnlyIfCurrent = 2; // STGC_ONLYIFCURRENT

    internal static byte[] Fingerprint(string source)
    {
        using var input = File.OpenRead(source);
        return SHA256.HashData(input);
    }

    internal static string ReserveOutput(string source) => new OutputNames(source).Reserve();

    // 跳过已用名称；如果并发创建又抢先一步，CreateNew 安全失败，不会清理别人的文件。
    private sealed class OutputNames(string source)
    {
        private readonly int stemLength = Math.Clamp(Path.GetFileNameWithoutExtension(source).Length, 1, 8);
        public string Reserve() => CreateEmpty(Candidates().First(Available));
        private IEnumerable<string> Candidates() => Enumerable.Range(0, 64).Select(Next);
        private string Next(int _) => Path.Combine(Path.GetDirectoryName(source)!, Path.GetRandomFileName()[..stemLength] + ".docx");
        private bool Available(string path) => !string.Equals(source, path, StringComparison.OrdinalIgnoreCase) && !Path.Exists(path);

        private static string CreateEmpty(string path)
        {
            File.Open(path, FileMode.CreateNew).Dispose();
            return path;
        }
    }

    internal static bool Commit(string source, string result, byte[] expected) =>
        new PendingCommit(result, expected).Apply(source);

    private sealed class PendingCommit(string result, byte[] expected)
    {
        public bool Apply(string source) => new ShellStream(source, ExclusiveTransaction).Use(CompareAndReplace);
        private bool CompareAndReplace(IStream target) => Matches(target) && Replace(target);
        private bool Matches(IStream target) => expected.AsSpan().SequenceEqual(StreamDigest.Hash(target));
        private bool Replace(IStream target) => new ShellStream(result, ShareDenyWrite).Use(new Replacement(target).CopyAndCommit);
    }

    private sealed class Replacement(IStream target)
    {
        public bool CopyAndCommit(IStream input) => LengthOf(input) > 0 && Publish(input);

        private static long LengthOf(IStream input)
        {
            input.Stat(out STATSTG stat, 1); // STATFLAG_NONAME
            return stat.cbSize;
        }

        private bool Publish(IStream input)
        {
            Copy(input);
            return Commit();
        }

        private void Copy(IStream input)
        {
            RewindAndTruncate();
            input.CopyTo(target, long.MaxValue, IntPtr.Zero, IntPtr.Zero);
        }

        private void RewindAndTruncate()
        {
            target.Seek(0, 0, IntPtr.Zero);
            target.SetSize(0);
        }

        private bool Commit()
        {
            target.Commit(OnlyIfCurrent);
            return true;
        }
    }

    private sealed class ShellStream(string path, int mode) : IDisposable
    {
        private readonly IStream stream = OpenShellStream(path, mode);
        public void Dispose() => Marshal.ReleaseComObject(stream);
        public bool Use(Func<IStream, bool> action)
        {
            using (this) { return action(stream); }
        }
    }

    // IStream.Read 通过指针返回读取长度，复用一个缓冲区，避免把大文档读进内存。
    private sealed class StreamDigest(IStream stream) : IDisposable
    {
        private readonly byte[] buffer = new byte[65536];
        private readonly IntPtr bytesRead = Marshal.AllocCoTaskMem(sizeof(int));
        private int Count => Marshal.ReadInt32(bytesRead);
        public void Dispose() => Marshal.FreeCoTaskMem(bytesRead);

        public static byte[] Hash(IStream stream)
        {
            using var reader = new StreamDigest(stream);
            return reader.ComputeHash();
        }

        private byte[] ComputeHash()
        {
            using var hash = NewHash();
            return Finish(hash);
        }

        private static IncrementalHash NewHash() => IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

        private byte[] Finish(IncrementalHash hash)
        {
            AppendContents(hash);
            return hash.GetHashAndReset();
        }

        private void AppendContents(IncrementalHash hash)
        {
            while (Read()) hash.AppendData(buffer, 0, Count);
        }

        private void ReadBuffer() => stream.Read(buffer, buffer.Length, bytesRead);

        private bool Read()
        {
            ReadBuffer();
            return Count != 0;
        }
    }

    private static IStream OpenShellStream(string path, int mode) =>
        CheckOpen(SHCreateStreamOnFileEx(path, mode, 0, false, IntPtr.Zero, out IStream stream), stream);

    private static IStream CheckOpen(int status, IStream stream)
    {
        Marshal.ThrowExceptionForHR(status);
        return stream;
    }

    // STGM_TRANSACTED 将未提交的数据留在 Shell 临时流中；出错时 Release 即回滚。
    [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
    private static extern int SHCreateStreamOnFileEx(string path, int mode, int attributes, bool create, IntPtr template, out IStream stream);
}
