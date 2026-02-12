using System.Windows.Controls;

namespace PersonalTools.UserControls
{
    #pragma warning disable CA1515 // 符合WPF框架要求，需要保持public访问修饰符
    public partial class ELFHeaderInfoControl : UserControl
    {
        #pragma warning restore CA1515

        public ELFHeaderInfoControl()
        {
            InitializeComponent();
        }

        public void SetELFHeaderInfo(string headerInfo)
        {
            ELFHeaderInfoTextBox.Text = headerInfo;
        }

        public void SetInterpreterInfo(string interpreter)
        {
            if (!string.IsNullOrEmpty(interpreter))
            {
                ELFHeaderInfoTextBox.Text += $"\n\nInterpreter:\n{interpreter}\n";
            }
        }
    }
}