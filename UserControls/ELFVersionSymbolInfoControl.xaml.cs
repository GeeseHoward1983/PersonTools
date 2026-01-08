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
            ELFVersionSymbolInfoTextBlock.Text = versionSymbolInfo;
        }
    }
}