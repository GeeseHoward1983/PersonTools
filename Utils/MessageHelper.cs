using System.Windows;

namespace PersonalTools.Utils
{
    /// <summary>
    /// 统一的消息框封装：固定按钮/图标与默认标题，消除各控件里重复的 MessageBox.Show 四参数模板。
    /// </summary>
    internal static class MessageHelper
    {
        /// <summary>错误提示（红色叉图标）。标题默认“错误”。</summary>
        public static void ShowError(string message, string title = "错误")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        /// <summary>一般信息提示（蓝色 i 图标）。标题默认“提示”。</summary>
        public static void ShowInfo(string message, string title = "提示")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>警告提示（黄色感叹号图标）。标题默认“提示”。</summary>
        public static void ShowWarning(string message, string title = "提示")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
