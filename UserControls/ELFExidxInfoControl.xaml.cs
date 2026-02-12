using System.Windows.Controls;

namespace PersonalTools.UserControls
{
    /// <summary>
    /// Interaction logic for ELFExidxInfoControl.xaml
    /// </summary>
    #pragma warning disable CA1515 // 符合WPF框架要求，需要保持public访问修饰符
    public partial class ELFExidxInfoControl : UserControl
    {
        #pragma warning restore CA1515
        public ELFExidxInfoControl()
        {
            InitializeComponent();
        }

        public void SetExidxInfo(string exidxInfo)
        {
            ExidxInfoTextBox.Text = exidxInfo;
        }
    }
}