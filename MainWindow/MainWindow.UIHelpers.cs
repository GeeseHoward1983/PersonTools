using System.Windows;
using System.Windows.Controls;

namespace MyTool
{
    public partial class MainWindow : Window
    {
        private void AddHeaderInfo(string title, System.Collections.Generic.Dictionary<string, string> info)
        {
            var groupBox = new GroupBox { Header = title, Margin = new Thickness(0, 5, 0, 5) };
            var panel = new StackPanel { Margin = new Thickness(5) };

            foreach (var item in info)
            {
                var textBlock = new TextBlock
                {
                    Text = $"{item.Key}: {item.Value}",
                    Margin = new Thickness(0, 2, 0, 2),
                    TextWrapping = System.Windows.TextWrapping.Wrap
                };
                panel.Children.Add(textBlock);
            }

            groupBox.Content = panel;
            HeaderInfoPanel.Children.Add(groupBox);
        }

        private void AddAdditionalInfo(string title, System.Collections.Generic.Dictionary<string, string> info)
        {
            var groupBox = new GroupBox { Header = title, Margin = new Thickness(0, 5, 0, 5) };
            var panel = new StackPanel { Margin = new Thickness(5) };

            int itemsAdded = 0;
            foreach (var item in info)
            {
                // 添加所有信息，即使是空的也显示出来，这样用户可以看到哪些信息不存在
                var textBlock = new TextBlock
                {
                    Text = $"{item.Key}: {item.Value ?? "无"}",
                    Margin = new Thickness(0, 2, 0, 2),
                    TextWrapping = System.Windows.TextWrapping.Wrap
                };
                panel.Children.Add(textBlock);
                itemsAdded++;
            }

            // 如果没有任何信息，添加提示文本
            if (itemsAdded == 0)
            {
                var textBlock = new TextBlock
                {
                    Text = "未找到相关信息",
                    Margin = new Thickness(0, 2, 0, 2),
                    FontStyle = FontStyles.Italic,
                    Foreground = System.Windows.Media.Brushes.Gray
                };
                panel.Children.Add(textBlock);
            }

            groupBox.Content = panel;
            AdditionalInfoPanel.Children.Add(groupBox);
        }
    }
}