using System.Windows;
using PersonalTools.Enums;

namespace PersonalTools.Utils
{
    /// <summary>
    /// 统一的消息框封装：固定按钮/图标与默认标题，消除各控件里重复的 MessageBox.Show 四参数模板。
    /// 默认标题按 <see cref="GlobalState.CurrentLanguageType"/> 本地化（title 传 null 时生效），
    /// 避免英文/繁体环境下正文已本地化而对话框标题恒为简体中文。
    /// </summary>
    internal static class MessageHelper
    {
        /// <summary>错误提示（红色叉图标）。title 为 null 时使用本地化的“错误”。</summary>
        public static void ShowError(string message, string? title = null)
        {
            MessageBox.Show(message, title ?? ErrorTitle(), MessageBoxButton.OK, MessageBoxImage.Error);
        }

        /// <summary>一般信息提示（蓝色 i 图标）。title 为 null 时使用本地化的“提示”。</summary>
        public static void ShowInfo(string message, string? title = null)
        {
            MessageBox.Show(message, title ?? NoticeTitle(), MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>警告提示（黄色感叹号图标）。title 为 null 时使用本地化的“提示”。</summary>
        public static void ShowWarning(string message, string? title = null)
        {
            MessageBox.Show(message, title ?? NoticeTitle(), MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private static string ErrorTitle() => GlobalState.CurrentLanguageType switch
        {
            LanguageType.English => "Error",
            LanguageType.TraditionalChinese => "錯誤",
            _ => "错误",
        };

        private static string NoticeTitle() => GlobalState.CurrentLanguageType switch
        {
            LanguageType.English => "Notice",
            _ => "提示", // 简体与繁体的“提示”字形一致
        };
    }
}
