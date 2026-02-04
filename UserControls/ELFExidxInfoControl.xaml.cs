using System.Windows.Controls;

namespace PersonalTools.UserControls
{
    /// <summary>
    /// Interaction logic for ELFExidxInfoControl.xaml
    /// </summary>
    public partial class ELFExidxInfoControl : UserControl
    {
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