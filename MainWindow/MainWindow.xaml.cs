using PersonalTools.UserControls;
using System.Windows;

namespace PersonalTools
{
    #pragma warning disable CA1515 // 符合WPF框架要求，需要保持public访问修饰符
    public partial class MainWindow : Window
    {
        #pragma warning restore CA1515
        public MainWindow()
        {
            InitializeComponent();
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            // 配置 PE / ELF 两个文件 tab 宿主（共享同一个 FileTabHostControl）
            PEHost.FileFilter = "PE Files (*.exe;*.dll;*.sys)|*.exe;*.dll;*.sys|All files (*.*)|*.*";
            PEHost.EmptyHintText = "拖入或打开 PE 文件 (*.exe / *.dll / *.sys)";
            PEHost.AnalyzerFactory = () => new PEAnalyzerControl();

            ELFHost.FileFilter = "Executable and Linkable Format files (*.elf)|*.elf|All files (*.*)|*.*";
            ELFHost.EmptyHintText = "拖入或打开 ELF 文件 (*.elf / *.so / 可执行文件)";
            ELFHost.AnalyzerFactory = () => new ELFAnalyzerControl();
        }
    }
}