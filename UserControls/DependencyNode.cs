using PersonalTools.PEAnalyzer.Models;
using PersonalTools.PEAnalyzer.Parsers;
using System.Collections.ObjectModel;
using System.IO;

namespace PersonalTools.UserControls
{
    /// <summary>
    /// 依赖树节点：惰性解析依赖 DLL 以展开下一层，并缓存其 PEInfo 供导入/导出展示。
    /// 根节点为已打开文件本身；子节点为其依赖，按需解析路径并解析 PE。
    /// </summary>
    internal sealed class DependencyNode
    {
        private const int MaxDepth = 16;

        private readonly HashSet<string> ancestors; // 本支已访问的完整路径（忌大小写），用于环检测
        private readonly int depth;
        private readonly bool isCyclic;
        private readonly bool isPlaceholder;

        public string Name { get; }
        public string? FullPath { get; }
        public PEInfo? Info { get; private set; }
        public ObservableCollection<DependencyNode> Children { get; } = [];
        public bool IsLoaded { get; private set; }
        public bool IsExpanded { get; set; }

        public string Display => isPlaceholder ? Name
            : isCyclic ? $"{Name} (循环依赖)"
            : FullPath == null ? $"{Name} (未找到)"
            : Name;

        // 能否展开：可解析、未成环、未超深度。决定是否预置占位子节点以显示“+”
        private bool CanExpand => !isCyclic && !isPlaceholder && FullPath != null && depth < MaxDepth;

        private DependencyNode(string name, string? fullPath, PEInfo? info, HashSet<string> ancestors, int depth, bool isCyclic, bool isPlaceholder = false)
        {
            Name = name;
            FullPath = fullPath;
            Info = info;
            this.ancestors = ancestors;
            this.depth = depth;
            this.isCyclic = isCyclic;
            this.isPlaceholder = isPlaceholder;

            if (CanExpand)
            {
                // 占位子节点：使节点显示“+”，展开时由 EnsureLoaded 替换为真实子节点。depth 取 depth+1 与替换后的真实子节点层级一致
                Children.Add(new DependencyNode("加载中...", null, null, ancestors, depth + 1, false, isPlaceholder: true));
            }
        }

        /// <summary>为已打开文件创建根节点（PEInfo 已就绪）。</summary>
        public static DependencyNode CreateRoot(PEInfo info)
        {
            string full = info.FilePath;
            HashSet<string> anc = new(StringComparer.OrdinalIgnoreCase);
            bool hasPath = !string.IsNullOrEmpty(full);
            if (hasPath)
            {
                anc.Add(full);
            }

            return new DependencyNode(
                hasPath ? Path.GetFileName(full) : "(未知)",
                hasPath ? full : null,
                info,
                anc,
                0,
                isCyclic: false);
        }

        /// <summary>首次展开或双击时调用：解析自身（若需要）并构建真实子节点。可重复调用（幂等）。</summary>
        public void EnsureLoaded()
        {
            if (IsLoaded || isPlaceholder)
            {
                return;
            }
            IsLoaded = true;

            // 解析自身以取得其依赖与导入/导出
            if (Info == null && FullPath != null)
            {
                try
                {
                    Info = PEParser.ParsePEFile(FullPath);
                }
                catch (IOException)
                {
                    Info = null;
                }
                catch (UnauthorizedAccessException)
                {
                    Info = null;
                }
                catch (ArgumentException)
                {
                    Info = null;
                }
            }

            Children.Clear(); // 移除占位节点
            if (Info == null)
            {
                return;
            }

            // 按本 PE 的位数对依赖做位数优先解析（32位优先 SysWOW64，64位优先 System32）
            bool? targetIs64Bit = PEParserUtils.Is64Bit(Info.OptionalHeader) ? true
                : PEParserUtils.Is32Bit(Info.OptionalHeader) ? false
                : null;

            string? baseDir = FullPath != null ? Path.GetDirectoryName(FullPath) : null;
            foreach (DependencyInfo dep in Info.Dependencies)
            {
                string? childPath = DependencyResolver.Resolve(dep.Name, baseDir, targetIs64Bit);
                bool cyclic = childPath != null && ancestors.Contains(childPath);

                HashSet<string> childAncestors = new(ancestors, StringComparer.OrdinalIgnoreCase);
                if (childPath != null)
                {
                    childAncestors.Add(childPath);
                }

                Children.Add(new DependencyNode(dep.Name, childPath, null, childAncestors, depth + 1, cyclic));
            }
        }
    }
}
