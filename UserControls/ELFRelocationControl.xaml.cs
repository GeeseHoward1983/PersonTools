using PersonalTools.ELFAnalyzer.Models;
using System.Windows.Controls;

namespace PersonalTools.UserControls
{
    public partial class ELFRelocationControl : UserControl
    {
        public ELFRelocationControl()
        {
            InitializeComponent();
        }

        internal void SetRelaDynData(List<ELFRelocationInfo> relaDynTable)
        {
            ELFRelaDynDataGrid.ItemsSource = relaDynTable;
        }
    }
}