using PersonalTools.ELFAnalyzer.Models;
using System.Collections.Generic;
using System.Windows.Controls;

namespace PersonalTools.UserControls
{
    public partial class ELFSectionHeaderControl : UserControl
    {
        public ELFSectionHeaderControl()
        {
            InitializeComponent();
        }

        internal void SetSectionHeadersData(List<ELFSectionHeaderInfo> sectionHeaders)
        {
            ELFSectionHeaderDataGrid.ItemsSource = sectionHeaders;
        }
    }
}