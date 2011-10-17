using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using System.IO;

namespace ServerUI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        internal string AutostartMap { get; private set; }

        public App()
        {
            AutostartMap = null;
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // If a map file name was specified as a command line parameter,
            // set it as an autostart map.
            if (e.Args.Length > 0)
            {
                if (File.Exists(e.Args[0]))
                {
                    AutostartMap = e.Args[0];
                }
            }
        }
    }
}
