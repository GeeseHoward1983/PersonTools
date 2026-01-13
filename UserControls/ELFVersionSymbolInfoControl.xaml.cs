using System.Windows.Controls;

namespace PersonalTools.UserControls
{
    public partial class ELFVersionSymbolInfoControl : UserControl
    {
        public ELFVersionSymbolInfoControl()
        {
            InitializeComponent();
        }

        public void SetVersionSymbolInfo(string versionSymbolInfo)
        {
            ELFVersionSymbolInfoTextBox.Text = versionSymbolInfo;
        }
    }
}