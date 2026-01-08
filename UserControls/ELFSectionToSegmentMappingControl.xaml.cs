using System.Windows.Controls;

namespace PersonalTools.UserControls
{
    public partial class ELFSectionToSegmentMappingControl : UserControl
    {
        public ELFSectionToSegmentMappingControl()
        {
            InitializeComponent();
        }

        public void SetSectionToSegmentInfo(string mappingInfo)
        {
            ELFSectionToSegmentInfoTextBlock.Text = mappingInfo;
        }
    }
}