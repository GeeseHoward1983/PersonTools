using System.Windows.Controls;

namespace PersonalTools.UserControls
{
    public partial class ELFVersionDependencyInfoControl : UserControl
    {
        public ELFVersionDependencyInfoControl()
        {
            InitializeComponent();
        }

        public void SetVersionDependencyInfo(string versionDependencyInfo)
        {
            ELFVersionDependencyInfoTextBlock.Text = versionDependencyInfo;
        }
    }
}