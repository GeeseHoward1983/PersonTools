using PersonalTools.ELFAnalyzer.Models;
using System.Collections.Generic;
using System.Windows.Controls;

namespace PersonalTools.UserControls
{
    #pragma warning disable CA1515 // 符合WPF框架要求，需要保持public访问修饰符
    public partial class ELFProgramHeaderControl : UserControl
    {
        #pragma warning restore CA1515

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