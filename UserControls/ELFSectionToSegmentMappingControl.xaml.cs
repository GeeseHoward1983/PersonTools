using System.Windows.Controls;

namespace PersonalTools.UserControls
{
    #pragma warning disable CA1515 // 符合WPF框架要求，需要保持public访问修饰符
    public partial class ELFSectionToSegmentMappingControl : UserControl
    {
        #pragma warning restore CA1515

        public ELFSectionToSegmentMappingControl()
        {
            InitializeComponent();
        }

        public void SetSectionToSegmentInfo(string mappingInfo)
        {
            ELFSectionToSegmentInfoTextBox.Text = mappingInfo;
        }
    }
}