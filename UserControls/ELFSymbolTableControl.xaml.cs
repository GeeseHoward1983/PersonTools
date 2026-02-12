using PersonalTools.ELFAnalyzer.Models;
using System.Windows.Controls;

namespace PersonalTools.UserControls
{
    #pragma warning disable CA1515 // 符合WPF框架要求，需要保持public访问修饰符
    public partial class ELFSymbolTableControl : UserControl
    {
        #pragma warning restore CA1515

        public ELFSymbolTableControl()
        {
            InitializeComponent();
        }

        internal void SetSymbolTableData(List<ELFSymbolTableInfo> symbolTable)
        {
            ELFSymbolTableDataGrid.ItemsSource = symbolTable;
        }
    }
}