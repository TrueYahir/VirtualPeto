using System.Configuration;
using System.Data;
using System.Windows;
using System.Windows.Threading;
using System.IO;
using System.Threading.Tasks;

namespace VirtualPeto;
public partial class App : Application
{
    public App()
    {
        DispatcherUnhandledException += App_DispatcherUnhandledException;

        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            File.WriteAllText("crash.txt", e.ExceptionObject.ToString());
        };

        TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            File.WriteAllText("crash.txt", e.Exception.ToString());
        };
    }

    private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        File.WriteAllText("crash.txt", e.Exception.ToString());
        MessageBox.Show(e.Exception.ToString());
    }
}

