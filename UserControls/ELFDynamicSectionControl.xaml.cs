using PersonalTools.ELFAnalyzer.Models;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace PersonalTools.UserControls
{
    #pragma warning disable CA1515 // 符合WPF框架要求，需要保持public访问修饰符
    public partial class ELFDynamicSectionControl : UserControl
    {
        #pragma warning restore CA1515

        public ELFDynamicSectionControl()
        {
            InitializeComponent();
        }

        internal void SetDynamicSectionData(List<ELFDynamicSectionInfo> dynamicSection)
        {
            ELFDynamicSectionDataGrid.ItemsSource = dynamicSection;
        }
    }
}