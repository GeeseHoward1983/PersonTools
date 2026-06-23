using System.IO;
using System.Text.Json;

namespace PersonalTools.MarkdownToWord.Models
{
    /// <summary>
    /// 导出样式配置的轻量持久化：读写 <c>%AppData%/PersonalTools/md2word-style.v2.json</c>。
    /// 本项目无 Properties/Settings 基础设施，故用 System.Text.Json 自带序列化。
    /// 仅持久化「可编辑字段」，加载时在默认配置上做覆盖，对新增类别向前兼容。
    /// </summary>
    internal static class DocxStyleSettingsStore
    {
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        private static string SettingsDir => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PersonalTools");

        private static string SettingsPath => Path.Combine(SettingsDir, "md2word-style.v2.json");

        // 旧版本文件名（v2 之前）：仅用于一次性迁移，不再写入
        private static string LegacySettingsPath => Path.Combine(SettingsDir, "md2word-style.json");

        /// <summary>加载样式配置；文件缺失/损坏时回退默认配置。</summary>
        public static DocxStyleSettings Load()
        {
            DocxStyleSettings settings = DocxStyleSettings.CreateDefault();
            try
            {
                string path = SettingsPath;
                if (File.Exists(path))
                {
                    ApplyPersisted(settings, File.ReadAllText(path));
                    return settings;
                }

                // v2 文件不存在：尝试从旧文件一次性迁移，避免老用户升级后自定义样式静默丢失。
                // 迁移成功后落盘 v2，旧文件保留（不删，便于回滚）。
                string legacy = LegacySettingsPath;
                if (File.Exists(legacy))
                {
                    if (ApplyPersisted(settings, File.ReadAllText(legacy)))
                    {
                        Save(settings);
                    }
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
            {
                PersonalTools.Utils.AppLogger.Log($"加载 Markdown 导出样式失败，使用默认配置: {ex.Message}");
                return DocxStyleSettings.CreateDefault();
            }

            return settings;
        }

        // 把一份 JSON 持久化内容覆盖到 settings 上；解析出有效内容返回 true（供迁移判定是否落盘）
        private static bool ApplyPersisted(DocxStyleSettings settings, string json)
        {
            PersistedSettings? persisted = JsonSerializer.Deserialize<PersistedSettings>(json);
            if (persisted == null)
            {
                return false;
            }

            settings.GenerateToc = persisted.GenerateToc;
            // persisted.Rows 可能因 JSON 显式 "Rows": null 反序列化为 null，需判空避免 NRE 逃逸出本 catch
            if (persisted.Rows != null)
            {
                foreach (ContentStyleRow row in settings.Rows)
                {
                    if (persisted.Rows.TryGetValue(row.Category.ToString(), out PersistedRow? p) && p != null)
                    {
                        ApplyTo(row, p);
                    }
                }
            }

            return true;
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
                PersonalTools.Utils.AppLogger.Log($"保存 Markdown 导出样式失败: {ex.Message}");
            }
        }

        private static void ApplyTo(ContentStyleRow row, PersistedRow p)
        {
            // 对字符串字段做空值兜底：JSON 中显式 null（或被篡改/旧版本文件）会令反序列化得到 null，
            // 直接赋值会污染 OOXML 字体属性，故 null 时保留 row 现有默认值
            row.ChineseFont = string.IsNullOrEmpty(p.ChineseFont) ? row.ChineseFont : p.ChineseFont;
            row.WesternFont = string.IsNullOrEmpty(p.WesternFont) ? row.WesternFont : p.WesternFont;
            row.FontSizeName = string.IsNullOrEmpty(p.FontSizeName) ? row.FontSizeName : p.FontSizeName;
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
