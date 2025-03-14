using System.Linq;
using System.Windows;
using LagerInsights.Models;

namespace LagerInsights;

/// <summary>
///     Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private void App_Startup(object sender, StartupEventArgs e)
    {
        for (var i = 0; i != e.Args.Length; ++i)
        {
            string[] lognames = { "/log", "/logging", "/v", "/verbose" };
            if (lognames.Contains(e.Args[i].ToLower())) Globals.VERBOSE_LOGGING = true;
        }
    }
}