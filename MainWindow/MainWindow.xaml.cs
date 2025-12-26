using MyTool.PEAnalyzer.Models;
using System.Windows;

namespace MyTool
{
    public partial class MainWindow : Window
    {
        private PEInfo? currentPEInfo = null;

        public MainWindow()
        {
            InitializeComponent();
            InitializeCRCAlgorithmComboBox();
            InitializeSHA3AlgorithmComboBox();
            InitializeAesComboBoxes(); // 初始化AES下拉框
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }
    }
}