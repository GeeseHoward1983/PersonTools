# 测试项目和编译产物只写入系统临时目录，避免被主项目默认文件匹配纳入编译。
# 运行：./scripts/Test-PEExportAndVersion.ps1 -Configuration Release
[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug',
    [string]$SamplePath = ''
)

$ErrorActionPreference = 'Stop'
$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if ([string]::IsNullOrWhiteSpace($SamplePath)) {
    $SamplePath = Join-Path $repoRoot 'bin/Release/net10.0-windows7.0/TGFVProcessAPI.dll'
}
$sample = (Resolve-Path -LiteralPath $SamplePath).Path
$testDir = Join-Path ([System.IO.Path]::GetTempPath()) ('PersonTools-PE-Regression-' + [Guid]::NewGuid().ToString('N'))
$utf8 = [System.Text.UTF8Encoding]::new($false)

$project = @'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0-windows7.0</TargetFramework>
    <UseWPF>true</UseWPF>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>
'@

# PEReader 独立读取导出表，以 FileVersionInfo 对照嵌入版本资源。
# 界面测试使用透明窗口，完成后自动关闭；版本对照排除外置 MUI 文件影响。
$source = @'
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

internal static class Program
{
    private static Assembly assembly = null!;
    private static string sample = null!;
    [STAThread]
    private static int Main(string[] args)
    {
        assembly = Assembly.LoadFrom(args[0]);
        sample = args[1];
        (string Name, Action Run)[] tests =
        [
            ("TGFVProcessAPI export names and RVAs", () => CheckExports(sample)),
            ("TGFVProcessAPI version strings", () => CheckVersions(sample)),
            ("x64 Windows DLL exports", () => CheckExports(Path.Combine(Environment.SystemDirectory, "kernel32.dll"))),
            ("x86 Windows DLL exports", () => CheckExports(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "SysWOW64", "kernel32.dll"))),
            ("Windows DLL version strings", () => CheckVersions(Path.Combine(Environment.SystemDirectory, "kernel32.dll"))),
            ("WPF displays sample export names and version strings", CheckView),
            ("PE32 named, ordinal-only, forwarded and empty exports", () => WithFile(ExportFixture(false), CheckExports)),
            ("PE32+ named, ordinal-only, forwarded and empty exports", () => WithFile(ExportFixture(true), CheckExports)),
            ("Unicode version strings and valid-then-broken language leaf", () => WithFile(VersionFixture(false), CheckVersionFixture)),
            ("String-only version resource survives a broken language leaf", () => WithFile(VersionFixture(true), CheckVersionFixture)),
            ("Truncated version resource stays within its declared data size", CheckTruncatedVersion),
            ("Certificate table is read from its file offset", CheckCertificate),
            ("Unsigned sample is not reported as a broken certificate", CheckUnsigned)
        ];
        int failures = 0;
        foreach (var test in tests)
        {
            try { test.Run(); Console.WriteLine("PASS " + test.Name); }
            catch (Exception ex) { failures++; Console.WriteLine("FAIL " + test.Name + ": " + ex.Message); }
        }
        Console.WriteLine($"{tests.Length - failures}/{tests.Length} tests passed.");
        return failures == 0 ? 0 : 1;
    }
    private static object Get(object value, string name) => value.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.GetValue(value)!;
    private static object Parse(string path) => assembly.GetType("PersonalTools.PEAnalyzer.Parsers.PEParser")!.GetMethod("ParsePEFile", BindingFlags.Static | BindingFlags.Public)!.Invoke(null, [path])!;
    private static void Equal<T>(T expected, T actual, string field)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException($"{field}: expected {JsonSerializer.Serialize(expected)}, got {JsonSerializer.Serialize(actual)}");
    }
    private static List<(int Ordinal, uint RVA, string Name)> ExpectedExports(string path)
    {
        using FileStream stream = File.OpenRead(path);
        using PEReader pe = new(stream, PEStreamOptions.LeaveOpen);
        using BinaryReader reader = new(stream, Encoding.ASCII, leaveOpen: true);
        var directory = pe.PEHeaders.PEHeader!.ExportTableDirectory;
        long Offset(uint rva)
        {
            foreach (var section in pe.PEHeaders.SectionHeaders)
                if (rva >= section.VirtualAddress && rva < (long)section.VirtualAddress + Math.Max(section.VirtualSize, section.SizeOfRawData))
                    return section.PointerToRawData + rva - section.VirtualAddress;
            throw new InvalidDataException($"Unmapped RVA {rva:X}");
        }
        string ReadString(uint rva)
        {
            stream.Position = Offset(rva);
            List<byte> bytes = [];
            for (byte b; (b = reader.ReadByte()) != 0;) bytes.Add(b);
            return Encoding.ASCII.GetString(bytes.ToArray());
        }
        stream.Position = Offset((uint)directory.RelativeVirtualAddress) + 16;
        uint baseOrdinal = reader.ReadUInt32(), count = reader.ReadUInt32(), namedCount = reader.ReadUInt32();
        uint functionTable = reader.ReadUInt32(), nameTable = reader.ReadUInt32(), ordinalTable = reader.ReadUInt32();
        Dictionary<int, string> names = [];
        for (int i = 0; i < namedCount; i++)
        {
            stream.Position = Offset(nameTable) + i * 4;
            uint nameRva = reader.ReadUInt32();
            stream.Position = Offset(ordinalTable) + i * 2;
            int index = reader.ReadUInt16();
            names[index] = ReadString(nameRva);
        }
        List<(int, uint, string)> result = [];
        for (int i = 0; i < count; i++)
        {
            stream.Position = Offset(functionTable) + i * 4;
            uint rva = reader.ReadUInt32();
            if (rva == 0) continue;
            int ordinal = checked((int)baseOrdinal + i);
            string name = names.GetValueOrDefault(i, $"Ordinal_{ordinal}");
            if (rva >= directory.RelativeVirtualAddress && rva < (long)directory.RelativeVirtualAddress + directory.Size)
                name += $" (forwarded -> {ReadString(rva)})";
            result.Add((ordinal, rva, name));
        }
        return result;
    }
    private static void CheckExports(string path)
    {
        List<object> actual = ((IEnumerable)Get(Parse(path), "ExportFunctions")).Cast<object>().ToList();
        var expected = ExpectedExports(path);
        Equal(expected.Count, actual.Count, "Export count");
        for (int i = 0; i < actual.Count; i++)
        {
            Equal(expected[i].Ordinal, (int)Get(actual[i], "Ordinal"), $"Export[{i}].Ordinal");
            Equal(expected[i].RVA, (uint)Get(actual[i], "RVA"), $"Export[{i}].RVA");
            Equal(expected[i].Name, (string)Get(actual[i], "Name"), $"Export[{i}].Name");
        }
        Console.WriteLine($"  Verified {actual.Count} exports in {Path.GetFileName(path)}.");
    }
    private static void CheckVersions(string path)
    {
        // Read the embedded resource only: system-directory files may merge an external MUI file.
        string copy = Path.Combine(Path.GetTempPath(), $"PersonTools-version-{Guid.NewGuid():N}.dll");
        File.Copy(path, copy);
        FileVersionInfo expected;
        try { expected = FileVersionInfo.GetVersionInfo(copy); } finally { File.Delete(copy); }
        object actual = Get(Parse(path), "AdditionalInfo");
        (string Property, string? Expected)[] fields =
        [
            ("FileVersion", expected.FileVersion), ("ProductVersion", expected.ProductVersion),
            ("CompanyName", expected.CompanyName), ("ProductName", expected.ProductName),
            ("FileDescription", expected.FileDescription), ("OriginalFileName", expected.OriginalFilename),
            ("InternalName", expected.InternalName), ("LegalCopyright", expected.LegalCopyright),
            ("LegalTrademarks", expected.LegalTrademarks)
        ];
        foreach (var field in fields)
            Equal(field.Expected ?? "", (string)Get(actual, field.Property), field.Property);
        Console.WriteLine($"  Version {Get(actual, "FileVersion")}, company {Get(actual, "CompanyName")}, product {Get(actual, "ProductName")}.");
    }
    private static void WithFile(byte[] bytes, Action<string> test)
    {
        string path = Path.Combine(Path.GetTempPath(), $"PersonTools-fixture-{Guid.NewGuid():N}.dll");
        try { File.WriteAllBytes(path, bytes); test(path); } finally { File.Delete(path); }
    }
    private static void U16(byte[] b, int p, ushort v) => BitConverter.GetBytes(v).CopyTo(b, p);
    private static void U32(byte[] b, int p, uint v) => BitConverter.GetBytes(v).CopyTo(b, p);
    private static byte[] MinimalPE(bool is64Bit = true)
    {
        byte[] b = new byte[0x1200];
        U16(b, 0, 0x5a4d); U32(b, 60, 0x80); U32(b, 0x80, 0x4550);
        U16(b, 0x84, is64Bit ? (ushort)0x8664 : (ushort)0x14c); U16(b, 0x86, 1);
        ushort optionalSize = is64Bit ? (ushort)240 : (ushort)224;
        U16(b, 0x94, optionalSize); U16(b, 0x96, 0x2102);
        int o = 0x98;
        U16(b, o, is64Bit ? (ushort)0x20b : (ushort)0x10b);
        if (is64Bit) BitConverter.GetBytes(0x140000000UL).CopyTo(b, o + 24); else U32(b, o + 28, 0x400000);
        U32(b, o + 32, 0x1000); U32(b, o + 36, 0x200); U32(b, o + 56, 0x2000); U32(b, o + 60, 0x200);
        U16(b, o + 68, 3); U32(b, o + (is64Bit ? 108 : 92), 16);
        int section = o + optionalSize;
        Encoding.ASCII.GetBytes(".rdata").CopyTo(b, section);
        U32(b, section + 8, 0x1000); U32(b, section + 12, 0x1000);
        U32(b, section + 16, 0x1000); U32(b, section + 20, 0x200); U32(b, section + 36, 0x40000040);
        return b;
    }
    private static byte[] ExportFixture(bool is64Bit)
    {
        byte[] b = MinimalPE(is64Bit);
        int directory = 0x98 + (is64Bit ? 112 : 96);
        U32(b, directory, 0x1400); U32(b, directory + 4, 0x100);
        int export = 0x600;
        U32(b, export + 16, 7); U32(b, export + 20, 5); U32(b, export + 24, 3);
        U32(b, export + 28, 0x1440); U32(b, export + 32, 0x1460); U32(b, export + 36, 0x1470);
        uint[] functions = [0x1010, 0x1020, 0x14b0, 0, 0x1510];
        for (int i = 0; i < functions.Length; i++) U32(b, 0x640 + i * 4, functions[i]);
        U32(b, 0x660, 0x1480); U32(b, 0x664, 0x1490); U32(b, 0x668, 0x14a0);
        U16(b, 0x670, 0); U16(b, 0x672, 2); U16(b, 0x674, 4);
        Encoding.ASCII.GetBytes("Before\0").CopyTo(b, 0x680);
        Encoding.ASCII.GetBytes("Forwarded\0").CopyTo(b, 0x690);
        Encoding.ASCII.GetBytes("After\0").CopyTo(b, 0x6a0);
        Encoding.ASCII.GetBytes("other.Target\0").CopyTo(b, 0x6b0);
        b[0x210] = b[0x220] = b[0x710] = 0xCC;
        return b;
    }
    private static byte[] Node(string key, byte[] value, ushort valueLength, ushort type, params byte[][] children)
    {
        using MemoryStream stream = new();
        using BinaryWriter writer = new(stream, Encoding.Unicode);
        writer.Write((ushort)0); writer.Write(valueLength); writer.Write(type);
        writer.Write(Encoding.Unicode.GetBytes(key + "\0"));
        while (stream.Position % 4 != 0) writer.Write((byte)0);
        writer.Write(value);
        foreach (byte[] child in children)
        {
            while (stream.Position % 4 != 0) writer.Write((byte)0);
            writer.Write(child);
        }
        byte[] bytes = stream.ToArray(); U16(bytes, 0, checked((ushort)bytes.Length)); return bytes;
    }
    private static byte[] VersionFixture(bool stringOnly)
    {
        byte[] Pair(string key, string value) => Node(key, Encoding.Unicode.GetBytes(value + "\0"), checked((ushort)(value.Length + 1)), 1);
        byte[] table = Node("040904b0", [], 0, 1,
            Pair("CompanyName", "示例公司"), Pair("ProductName", "中文产品®"),
            Pair("FileVersion", "1.2.3.4"), Pair("ProductVersion", "5.6.7.8"), Pair("LegalCopyright", "© 示例"));
        byte[] strings = Node("StringFileInfo", [], 0, 1, table);
        byte[] translation = Node("Translation", BitConverter.GetBytes(0x04b00409U), 4, 0);
        byte[] variables = Node("VarFileInfo", [], 0, 1, translation);
        byte[] fixedInfo = new byte[52];
        U32(fixedInfo, 0, 0xFEEF04BD); U32(fixedInfo, 4, 0x10000);
        U32(fixedInfo, 8, 0x10002); U32(fixedInfo, 12, 0x30004);
        U32(fixedInfo, 16, 0x50006); U32(fixedInfo, 20, 0x70008);
        byte[] version = Node("VS_VERSION_INFO", stringOnly ? [] : fixedInfo, stringOnly ? (ushort)0 : (ushort)52, 0, strings, variables);
        byte[] b = MinimalPE(); int directory = 0x98 + 112 + 16;
        U32(b, directory, 0x1300); U32(b, directory + 4, (uint)(0x300 + version.Length));
        int resource = 0x500;
        U16(b, resource + 14, 1); U32(b, resource + 16, 16); U32(b, resource + 20, 0x80000020);
        U16(b, resource + 0x20 + 14, 1); U32(b, resource + 0x30, 1); U32(b, resource + 0x34, 0x80000040);
        U16(b, resource + 0x40 + 14, 2);
        U32(b, resource + 0x50, 0x409); U32(b, resource + 0x54, 0x80);
        U32(b, resource + 0x58, 0x804); U32(b, resource + 0x5c, 0x90);
        U32(b, resource + 0x80, 0x1600); U32(b, resource + 0x84, (uint)version.Length);
        U32(b, resource + 0x90, 0xdeadbeef); U32(b, resource + 0x94, 100);
        version.CopyTo(b, 0x800);
        return b;
    }
    private static void CheckVersionFixture(string path)
    {
        object info = Get(Parse(path), "AdditionalInfo");
        Equal("1.2.3.4", (string)Get(info, "FileVersion"), "FileVersion after malformed sibling");
        Equal("5.6.7.8", (string)Get(info, "ProductVersion"), "ProductVersion");
        Equal("示例公司", (string)Get(info, "CompanyName"), "Unicode company");
        Equal("中文产品®", (string)Get(info, "ProductName"), "Unicode product");
        Equal("© 示例", (string)Get(info, "LegalCopyright"), "Unicode copyright");
        if (string.IsNullOrEmpty((string)Get(info, "TranslationInfo"))) throw new InvalidOperationException("Translation information is missing.");
    }
    private static void CheckTruncatedVersion()
    {
        byte[] b = VersionFixture(false); U32(b, 0x584, 24);
        WithFile(b, path =>
        {
            object info = Get(Parse(path), "AdditionalInfo");
            Equal("", (string)Get(info, "CompanyName"), "Cannot read beyond resource size");
            if ((string)Get(info, "FileVersion") == "1.2.3.4") throw new InvalidOperationException("Read a version past its declared resource size.");
        });
    }
    private static void CheckCertificate()
    {
        byte[] b = MinimalPE().Concat(new byte[16]).ToArray();
        int directory = 0x98 + 112 + 32;
        U32(b, directory, 0x1200); U32(b, directory + 4, 16);
        U32(b, 0x1200, 16); U16(b, 0x1204, 0x200); U16(b, 0x1206, 2);
        WithFile(b, path =>
        {
            object info = Get(Parse(path), "AdditionalInfo");
            Equal(true, (bool)Get(info, "IsSigned"), "Certificate entry presence");
            if (!((string)Get(info, "CertificateInfo")).Contains("PKCS#7")) throw new InvalidOperationException("Certificate table was not read from its file offset.");
        });
    }
    private static void CheckUnsigned()
    {
        object info = Get(Parse(sample), "AdditionalInfo");
        Equal(false, (bool)Get(info, "IsSigned"), "Unsigned sample");
        Equal("未包含数字签名", (string)Get(info, "CertificateInfo"), "Absent certificate message");
    }
    private static void CheckView()
    {
        _ = Application.Current ?? new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        UserControl control = (UserControl)Activator.CreateInstance(assembly.GetType("PersonalTools.UserControls.PEAnalyzerControl")!)!;
        Window window = new() { Content = control, Width = 1200, Height = 900, Opacity = 0, ShowInTaskbar = false };
        Dispatcher dispatcher = Dispatcher.CurrentDispatcher;
        DispatcherFrame frame = new();
        Exception? error = null;
        DispatcherUnhandledExceptionEventHandler handler = (_, e) => { error = e.Exception; e.Handled = true; frame.Continue = false; };
        dispatcher.UnhandledException += handler;
        DispatcherTimer timer = new() { Interval = TimeSpan.FromMilliseconds(20) };
        var deadline = DateTime.UtcNow.AddSeconds(15);
        timer.Tick += (_, _) =>
        {
            if (((TreeView)control.FindName("DependencyTree")).Items.Count > 0
                && ((DataGrid)control.FindName("IconsDataGrid")).ItemsSource != null) frame.Continue = false;
            else if (DateTime.UtcNow >= deadline) { error = new TimeoutException("UI load timeout."); frame.Continue = false; }
        };
        try
        {
            window.Show();
            dispatcher.BeginInvoke(new Action(() => control.GetType().GetMethod("LoadPEFile")!.Invoke(control, [sample])));
            timer.Start(); Dispatcher.PushFrame(frame);
            if (error != null) throw error;
            var expected = ExpectedExports(sample);
            var grid = (DataGrid)control.FindName("ExportFunctionsGrid");
            Equal(expected.Count, grid.Items.Count, "Displayed export count");
            for (int i = 0; i < expected.Count; i++) Equal(expected[i].Name, (string)Get(grid.Items[i], "Name"), $"Displayed export[{i}]");
            var panel = (StackPanel)control.FindName("AdditionalInfoPanel");
            string text = string.Join("\n", panel.Children.Cast<GroupBox>().SelectMany(group => ((StackPanel)group.Content).Children.Cast<TextBlock>()).Select(block => block.Text));
            var version = FileVersionInfo.GetVersionInfo(sample);
            foreach (string value in new[] { version.FileVersion!, version.CompanyName!, version.ProductName!, version.FileDescription! })
                if (!text.Contains(value, StringComparison.Ordinal)) throw new InvalidOperationException($"Displayed additional info is missing {value}.");
        }
        finally { timer.Stop(); dispatcher.UnhandledException -= handler; window.Close(); }
    }
}

'@

dotnet build (Join-Path $repoRoot 'PersonalTools.csproj') -c $Configuration -v minimal
if ($LASTEXITCODE -ne 0) { throw '主项目编译失败，无法运行回归测试。' }
$assemblyPath = Join-Path $repoRoot "bin/$Configuration/net10.0-windows7.0/PersonalTools.dll"
New-Item -ItemType Directory -Path $testDir | Out-Null
$projectPath = Join-Path $testDir 'Regression.csproj'
[System.IO.File]::WriteAllText($projectPath, $project, $utf8)
[System.IO.File]::WriteAllText((Join-Path $testDir 'Program.cs'), $source, $utf8)
dotnet run --project $projectPath -c $Configuration -- $assemblyPath $sample
$exitCode = $LASTEXITCODE
Write-Verbose "测试项目保留在临时目录以便排查：$testDir"
exit $exitCode
