using System.Windows.Controls;

namespace PersonalTools.UserControls
{
    /// <summary>
    /// ELFAttributeInfoControl.xaml 的交互逻辑
    /// </summary>
    public partial class ELFAttributeInfoControl : UserControl
    {
        public ELFAttributeInfoControl()
        {
            InitializeComponent();
        }

        public void SetAttributeInfo(string attributeInfo)
        {
            AttributeInfoTextBlock.Text = attributeInfo;
        }
    }
}