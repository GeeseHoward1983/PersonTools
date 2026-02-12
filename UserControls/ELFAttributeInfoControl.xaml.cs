using System.Windows.Controls;

namespace PersonalTools.UserControls
{
    /// <summary>
    /// ELFAttributeInfoControl.xaml 的交互逻辑
    /// </summary>
    #pragma warning disable CA1515 // 符合WPF框架要求，需要保持public访问修饰符
    public partial class ELFAttributeInfoControl : UserControl
    {
        #pragma warning restore CA1515
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