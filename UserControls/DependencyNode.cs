using PersonalTools.PEAnalyzer.Models;
using PersonalTools.PEAnalyzer.Parsers;
using PersonalTools.Utils;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;

namespace PersonalTools.UserControls
{
    /// <summary>
    /// 依赖树节点：惰性解析依赖 DLL 以展开下一层，并缓存其 PEInfo 供导入/导出展示。
    /// 根节点为已打开文件本身；子节点为其依赖，按需解析路径并解析 PE。
    /// </summary>
    internal sealed class DependencyNode : INotifyPropertyChanged
    {
        private const int MaxDepth = 16;

        private readonly HashSet<string> ancestors; // 本支已访问的完整路径（忌大小写），用于环检测
        private readonly int depth;
        private readonly bool isCyclic;
        private readonly bool isPlaceholder;
        private bool parseFailed; // 已定位到路径但解析失败：区别于"无依赖"，用于 Display 标记（改后经 INPC 刷新）
        private Task? loadTask; // 首次加载任务；并发/二次调用共享同一个，防竞态（仅 UI 线程访问，无需加锁）

        public event PropertyChangedEventHandler? PropertyChanged;

        public string Name { get; }
        public string? FullPath { get; }
        public PEInfo? Info { get; private set; }
        public ObservableCollection<DependencyNode> Children { get; } = [];
        public bool IsLoaded { get; private set; }
        public bool IsExpanded { get; set; }

        public string Display => isPlaceholder ? Name
            : isCyclic ? $"{Name} (循环依赖)"
            : FullPath == null ? $"{Name} (未找到)"
            : parseFailed ? $"{Name} (解析失败)"
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
                // 占位子节点：使节点显示“+”，展开时由 EnsureLoadedAsync 替换为真实子节点。depth 取 depth+1 与替换后的真实子节点层级一致
                Children.Add(new DependencyNode("加载中...", null, null, ancestors, depth + 1, false, isPlaceholder: true));
            }
            else if (!isCyclic && !isPlaceholder && FullPath != null && depth >= MaxDepth)
            {
                // 达最大展开深度但本可继续解析：插一个提示占位节点使“截断”对用户可见，
                // 而非误显示为无依赖的叶子。EnsureLoadedAsync 对 depth>=MaxDepth 直接返回，保留此提示、不再下钻。
                Children.Add(new DependencyNode("(超出最大展开深度)", null, null, ancestors, depth + 1, false, isPlaceholder: true));
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

        /// <summary>首次展开或双击时调用：解析自身（若需要）并构建真实子节点。可重复/并发调用（幂等）。</summary>
        public Task EnsureLoadedAsync()
        {
            if (isPlaceholder)
            {
                return Task.CompletedTask;
            }

            if (depth >= MaxDepth)
            {
                return Task.CompletedTask; // 达最大深度：保留“(超出最大展开深度)”提示子节点，不再下钻解析
            }

            // 并发/二次调用返回同一个加载任务：双击折叠节点会同时触发 Expanded 与 MouseDoubleClick 两个处理器，
            // 二者都调本方法。此前"先置 IsLoaded=true 再 await 后台解析"会让后触发者提前看到完成态却读到
            // Info==null（导入/导出面板空白，需再双击一次）。改为共享同一 loadTask，后触发者 await 的是同一次真实加载。
            // 事件均在 UI 线程触发，故 ??= 无需加锁。
            return loadTask ??= LoadCoreAsync();
        }

        private async Task LoadCoreAsync()
        {
            // 解析自身 PE 以取得其依赖与导入/导出。解析为 IO/CPU 密集，移到后台线程避免卡 UI；
            // await 默认回到 UI 线程后再修改 Children（ObservableCollection 绑定 TreeView，须在 UI 线程变更）。
            if (Info == null && FullPath != null)
            {
                string path = FullPath;
                Info = await Task.Run(() => TryParsePE(path)).ConfigureAwait(true);
            }

            Children.Clear(); // 移除占位节点
            if (Info == null)
            {
                if (FullPath != null)
                {
                    // 已定位到路径却解析失败（受保护/损坏）：标记失败态使用户可区分“无依赖”与“解析失败”。
                    parseFailed = true;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Display)));
                }

                IsLoaded = true; // 解析失败也算"已加载"（无子节点），避免重复触发
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

            // Children 构建完成后再置位：确保并发 await 者在任务完成时 Info 与 Children 均已就绪
            IsLoaded = true;
        }

        // 后台线程解析 PE：吞掉 IO/权限/参数异常返回 null，由调用方按 Info==null 处理；失败记日志便于事后排查。
        private static PEInfo? TryParsePE(string path)
        {
            try
            {
                return PEParser.ParsePEFile(path);
            }
            catch (IOException ex)
            {
                AppLogger.Log($"依赖 PE 解析失败(IO): {path} - {ex.Message}");
                return null;
            }
            catch (UnauthorizedAccessException ex)
            {
                AppLogger.Log($"依赖 PE 解析失败(权限): {path} - {ex.Message}");
                return null;
            }
            catch (ArgumentException ex)
            {
                AppLogger.Log($"依赖 PE 解析失败(参数): {path} - {ex.Message}");
                return null;
            }
            catch (InvalidDataException ex)
            {
                // 畸形/非 PE 依赖 DLL：ParsePEFile 抛 InvalidDataException，优雅降级为“无子节点”返回 null
                AppLogger.Log($"依赖 PE 解析失败(畸形): {path} - {ex.Message}");
                return null;
            }
        }
    }
}
