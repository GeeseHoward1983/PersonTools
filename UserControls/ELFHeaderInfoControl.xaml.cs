using System.Windows.Controls;

namespace PersonalTools.UserControls
{
    public partial class ELFHeaderInfoControl : UserControl
    {
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