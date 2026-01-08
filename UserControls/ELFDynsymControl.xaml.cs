using PersonalTools.ELFAnalyzer.Models;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace PersonalTools.UserControls
{
    public partial class ELFDynsymControl : UserControl
    {
        public ELFDynsymControl()
        {
            InitializeComponent();
        }

        public void SetDynsymData(List<ELFSymbolTableInfo> dynsymTable)
        {
            ELFDynsymDataGrid.ItemsSource = dynsymTable;
        }
    }
}