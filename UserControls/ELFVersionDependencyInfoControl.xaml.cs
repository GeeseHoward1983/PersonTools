using System.Windows.Controls;

namespace PersonalTools.UserControls
{
    #pragma warning disable CA1515 // 符合WPF框架要求，需要保持public访问修饰符
    public partial class ELFVersionDependencyInfoControl : UserControl
    {
        #pragma warning restore CA1515

        public ELFVersionDependencyInfoControl()
        {
            InitializeComponent();
        }

        public void SetVersionDependencyInfo(string versionDependencyInfo)
        {
            ELFVersionDependencyInfoTextBox.Text = versionDependencyInfo;
        }
    }
}