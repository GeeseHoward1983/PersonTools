using PersonalTools.ELFAnalyzer.Models;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace PersonalTools.UserControls
{
    #pragma warning disable CA1515 // 符合WPF框架要求，需要保持public访问修饰符
    public partial class ELFPltRelocationControl : UserControl
    {
        #pragma warning restore CA1515
        public ELFPltRelocationControl()
        {
            InitializeComponent();
        }

        internal void SetRelaPltData(List<ELFRelocationInfo> relaPltTable)
        {
            ELFRelaPltDataGrid.ItemsSource = relaPltTable;
        }
    }
}