using PersonalTools.ELFAnalyzer.Models;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace PersonalTools.UserControls
{
    public partial class ELFDynamicSectionControl : UserControl
    {
        public ELFDynamicSectionControl()
        {
            InitializeComponent();
        }

        public void SetDynamicSectionData(List<ELFDynamicSectionInfo> dynamicSection)
        {
            ELFDynamicSectionDataGrid.ItemsSource = dynamicSection;
        }
    }
}