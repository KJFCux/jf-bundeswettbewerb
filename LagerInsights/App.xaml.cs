using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using LagerInsights.IO;
using LagerInsights.Models;

namespace LagerInsights
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        void App_Startup(object sender, StartupEventArgs e)
        {
            for (int i = 0; i != e.Args.Length; ++i)
            {
                string[] lognames = { "/log", "/logging", "/v", "/verbose" };
                if (lognames.Contains(e.Args[i].ToLower()))
                {
                    Globals.VERBOSE_LOGGING = true;
                }
            }
        }
    }
}
