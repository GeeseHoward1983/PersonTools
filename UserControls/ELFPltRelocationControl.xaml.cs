using PersonalTools.ELFAnalyzer.Models;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace PersonalTools.UserControls
{
    public partial class ELFPltRelocationControl : UserControl
    {
        public ELFPltRelocationControl()
        {
            InitializeComponent();
        }

        public void SetRelaPltData(List<ELFRelocationInfo> relaPltTable)
        {
            ELFRelaPltDataGrid.ItemsSource = relaPltTable;
        }
    }
}