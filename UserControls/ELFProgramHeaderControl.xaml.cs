using PersonalTools.ELFAnalyzer.Models;
using System.Collections.Generic;
using System.Windows.Controls;

namespace PersonalTools.UserControls
{
    public partial class ELFProgramHeaderControl : UserControl
    {
        public ELFProgramHeaderControl()
        {
            InitializeComponent();
        }

        internal void SetProgramHeadersData(List<ProgramHeaderInfo> programHeaders)
        {
            ELFProgramHeaderDataGrid.ItemsSource = programHeaders;
        }
    }
}