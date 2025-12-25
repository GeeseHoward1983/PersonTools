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
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }
    }
}