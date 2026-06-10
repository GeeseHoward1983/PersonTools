using System.IO;
using System.Text.Json;

namespace PersonalTools.MarkdownToWord.Models
{
    /// <summary>
    /// 导出样式配置的轻量持久化：读写 <c>%AppData%/PersonalTools/md2word-style.json</c>。
    /// 本项目无 Properties/Settings 基础设施，故用 System.Text.Json 自带序列化。
    /// 仅持久化「可编辑字段」，加载时在默认配置上做覆盖，对新增类别向前兼容。
    /// </summary>
    internal static class DocxStyleSettingsStore
    {
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        private static string SettingsPath
        {
            get
            {
                string dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "PersonalTools");
                return Path.Combine(dir, "md2word-style.v2.json");
            }
        }

        /// <summary>加载样式配置；文件缺失/损坏时回退默认配置。</summary>
        public static DocxStyleSettings Load()
        {
            DocxStyleSettings settings = DocxStyleSettings.CreateDefault();
            try
            {
                string path = SettingsPath;
                if (!File.Exists(path))
                {
                    return settings;
                }

                PersistedSettings? persisted = JsonSerializer.Deserialize<PersistedSettings>(File.ReadAllText(path));
                if (persisted == null)
                {
                    return settings;
                }

                settings.GenerateToc = persisted.GenerateToc;
                foreach (ContentStyleRow row in settings.Rows)
                {
                    if (persisted.Rows.TryGetValue(row.Category.ToString(), out PersistedRow? p) && p != null)
                    {
                        ApplyTo(row, p);
                    }
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
            {
                Console.WriteLine($"加载 Markdown 导出样式失败，使用默认配置: {ex.Message}");
                return DocxStyleSettings.CreateDefault();
            }

            return settings;
        }

        /// <summary>保存样式配置；失败仅记录日志，不抛出。</summary>
        public static void Save(DocxStyleSettings settings)
        {
            try
            {
                string path = SettingsPath;
                string? dir = Path.GetDirectoryName(path);
                if (dir != null)
                {
                    Directory.CreateDirectory(dir);
                }

                PersistedSettings persisted = new() { GenerateToc = settings.GenerateToc };
                foreach (ContentStyleRow row in settings.Rows)
                {
                    persisted.Rows[row.Category.ToString()] = FromRow(row);
                }

                File.WriteAllText(path, JsonSerializer.Serialize(persisted, JsonOptions));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
            {
                Console.WriteLine($"保存 Markdown 导出样式失败: {ex.Message}");
            }
        }

        private static void ApplyTo(ContentStyleRow row, PersistedRow p)
        {
            row.ChineseFont = p.ChineseFont;
            row.WesternFont = p.WesternFont;
            row.FontSizeName = p.FontSizeName;
            row.Bold = p.Bold;
            row.Underline = p.Underline;
            row.FirstLineIndentChars = p.FirstLineIndentChars;
        }

        private static PersistedRow FromRow(ContentStyleRow row) => new()
        {
            ChineseFont = row.ChineseFont,
            WesternFont = row.WesternFont,
            FontSizeName = row.FontSizeName,
            Bold = row.Bold,
            Underline = row.Underline,
            FirstLineIndentChars = row.FirstLineIndentChars,
        };

        // 仅含可编辑字段的持久化 DTO
        private sealed class PersistedSettings
        {
            public bool GenerateToc { get; set; } = true;
            public Dictionary<string, PersistedRow> Rows { get; set; } = [];
        }

        private sealed class PersistedRow
        {
            public string ChineseFont { get; set; } = "宋体";
            public string WesternFont { get; set; } = "Times New Roman";
            public string FontSizeName { get; set; } = "五号";
            public bool Bold { get; set; }
            public bool Underline { get; set; }
            public double FirstLineIndentChars { get; set; }
        }
    }
}
