using System.Windows;

namespace PersonalTools
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    #pragma warning disable CA1515 // 符合WPF框架要求，需要保持public访问修饰符
    public partial class App : Application
    {
        #pragma warning restore CA1515
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            // 应用程序启动时的初始化代码可以放在这里
        }
    }

}