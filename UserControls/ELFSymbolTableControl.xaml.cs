using PersonalTools.ELFAnalyzer.Models;
using System.Windows.Controls;

namespace PersonalTools.UserControls
{
    public partial class ELFSymbolTableControl : UserControl
    {
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