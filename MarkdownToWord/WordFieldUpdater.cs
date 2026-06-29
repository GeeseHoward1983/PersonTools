using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;

namespace PersonalTools.MarkdownToWord
{
    /// <summary>
    /// 导出后调用本机已安装的 Word（后台、晚绑定 COM）对生成的 .docx「强制更新一次」域，
    /// 使目录页码、图/表编号在生成时即填好并静态化（无需打开时自动刷新，故不依赖 updateFields）。
    /// 未安装 Word 或更新失败时返回 false，由调用方提示用户手动更新。
    /// </summary>
    internal static class WordFieldUpdater
    {
        private const int WdDoNotSaveChanges = 0;
        private const int WdAlertsNone = 0;

        // Word COM 操作总超时：遇模态弹窗(激活向导/加载项)等情形同步 COM 调用会无限阻塞，
        // 在专用线程上执行并限时等待，超时即放弃，避免导出流程永久挂死。
        private static readonly TimeSpan WordTimeout = TimeSpan.FromSeconds(60);

        /// <summary>用 Word 打开文档、更新全部域与目录、保存关闭。成功返回 true。</summary>
        public static bool TryUpdateFields(string docxPath)
        {
            Type? wordType = Type.GetTypeFromProgID("Word.Application");
            if (wordType == null)
            {
                return false; // 未安装 Word
            }

            bool result = false;
            // 专用 STA 线程执行 COM；主线程限时 Join，超时则不再等待（后台线程随进程结束回收）。
            Thread worker = new(() => result = RunUpdate(wordType, docxPath))
            {
                IsBackground = true,
            };
            worker.SetApartmentState(ApartmentState.STA);
            worker.Start();

            if (!worker.Join(WordTimeout))
            {
                // 超时通常因 Word 弹出模态对话框卡死：后台 STA 线程仍阻塞在 COM 调用，
                // 其 finally/CleanUp(含 Quit) 可能无法触发，导致残留隐藏的 WINWORD.EXE 进程
                PersonalTools.Utils.AppLogger.Log("Word 域更新超时(可能因 Word 弹出模态对话框)；后台 COM 线程仍在运行，可能残留 WINWORD.EXE 进程，建议在任务管理器中手动结束。");
                return false;
            }

            return result;
        }

        private static bool RunUpdate(Type wordType, string docxPath)
        {
            dynamic? app = null;
            dynamic? doc = null;
            try
            {
                app = Activator.CreateInstance(wordType);
                if (app == null)
                {
                    return false;
                }

                app.Visible = false;
                app.DisplayAlerts = WdAlertsNone;

                doc = app.Documents.Open(docxPath);

                // 更新全部域（含 SEQ 图/表编号），并逐个更新目录，再整体更新一次使目录反映最终分页
                doc.Fields.Update();
                int tocCount = doc.TablesOfContents.Count;
                for (int i = 1; i <= tocCount; i++)
                {
                    doc.TablesOfContents.Item(i).Update();
                }

                doc.Fields.Update();

                doc.Save();
                // 不在此手动 Close/Quit/置 null：统一交 finally 的 CleanUp 完成关闭、退出与
                // Marshal.FinalReleaseComObject，确保成功路径下 RCW 也被释放（此前置 null 会令 CleanUp 跳过释放）
                return true;
            }
#pragma warning disable CA1031 // COM 互操作可抛出多种不可预知异常，统一兜底以免影响导出主流程
            catch (Exception ex)
            {
                PersonalTools.Utils.AppLogger.Log(string.Create(CultureInfo.InvariantCulture, $"调用 Word 更新域失败: {ex.Message}"));
                return false;
            }
#pragma warning restore CA1031
            finally
            {
                CleanUp(ref doc, ref app);
            }
        }

        private static void CleanUp(ref dynamic? doc, ref dynamic? app)
        {
#pragma warning disable CA1031 // 清理阶段任何异常都吞掉，确保不残留 Word 进程
            try
            {
                if (doc != null)
                {
                    doc.Close(WdDoNotSaveChanges);
                }
            }
            catch (Exception)
            {
            }

            try
            {
                if (app != null)
                {
                    app.Quit();
                }
            }
            catch (Exception)
            {
            }
#pragma warning restore CA1031

            if (doc != null)
            {
                Marshal.FinalReleaseComObject(doc);
                doc = null;
            }

            if (app != null)
            {
                Marshal.FinalReleaseComObject(app);
                app = null;
            }
        }
    }
}
