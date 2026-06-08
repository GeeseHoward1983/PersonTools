using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PersonalTools.UserControls
{
    /// <summary>
    /// 哈希算法行的视图模型：供 HashComputeControl 用 ItemsControl 数据模板渲染，
    /// 取代为每个算法单独命名的输入框/单选/结果控件（从而压低 XAML 生成代码的复杂度）。
    /// </summary>
    internal sealed class HashAlgorithmRow : INotifyPropertyChanged
    {
        public string Name { get; }
        public Func<byte[], byte[]> HashFunc { get; }

        public string Header => $"{Name}计算";
        public string InputPrompt => $"输入要计算{Name}的值：";
        public string CalcButtonText => $"计算{Name}";
        public string ResultPrompt => $"{Name}结果：";

        private string inputText = string.Empty;
        public string InputText
        {
            get => inputText;
            set
            {
                if (inputText != value)
                {
                    inputText = value;
                    OnPropertyChanged();
                }
            }
        }

        // 是否按十六进制解析输入（绑定到 Hex 单选；String 单选靠同面板互斥自动取反）
        public bool IsHexMode { get; set; }

        private string result = "等待计算...";
        public string Result
        {
            get => result;
            set
            {
                if (result != value)
                {
                    result = value;
                    OnPropertyChanged();
                }
            }
        }

        public HashAlgorithmRow(string name, Func<byte[], byte[]> hashFunc)
        {
            Name = name;
            HashFunc = hashFunc;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
