using System.Globalization;
using System.Runtime.InteropServices;

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

        /// <summary>用 Word 打开文档、更新全部域与目录、保存关闭。成功返回 true。</summary>
        public static bool TryUpdateFields(string docxPath)
        {
            Type? wordType = Type.GetTypeFromProgID("Word.Application");
            if (wordType == null)
            {
                return false; // 未安装 Word
            }

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
                doc.Close(WdDoNotSaveChanges);
                doc = null;
                app.Quit();
                app = null;
                return true;
            }
#pragma warning disable CA1031 // COM 互操作可抛出多种不可预知异常，统一兜底以免影响导出主流程
            catch (Exception ex)
            {
                Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"调用 Word 更新域失败: {ex.Message}"));
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
