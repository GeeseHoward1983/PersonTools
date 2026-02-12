using PersonalTools.ELFAnalyzer.Models;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace PersonalTools.UserControls
{
    #pragma warning disable CA1515 // 符合WPF框架要求，需要保持public访问修饰符
    public partial class ELFDynsymControl : UserControl
    {
        #pragma warning restore CA1515

        public ELFDynsymControl()
        {
            InitializeComponent();
        }

        internal void SetDynsymData(List<ELFSymbolTableInfo> dynsymTable)
        {
            ELFDynsymDataGrid.ItemsSource = dynsymTable;
        }
    }
}