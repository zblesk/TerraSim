using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Newtonsoft.Json;
using TerraSim.ForestWorld;
using TerraSim.Simulation;

namespace ServerUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private class TextBoxTraceListener : TraceListener
        {
            private TextBox output;

            public TextBoxTraceListener(TextBox output)
            {
                this.Name = "Trace";
                this.output = output;
            }

            public override void Write(string message)
            {
                output.Dispatcher.Invoke(new Action(() =>
                    {
                        output.Text += message;
                    }));
            }

            public override void WriteLine(string message)
            {
                output.Dispatcher.Invoke(new Action(() =>
                {
                    Write(message + '\n');
                }));
            }
        }
        
        private const string settingsPath = "server_settings.json";
        private TextBoxTraceListener listener;
        private SimulationCore core;
        private ServerSettings settings;
        private World world;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            listener = new TextBoxTraceListener(tbTrace);
            listener.TraceOutputOptions = TraceOptions.DateTime;
            Trace.Listeners.Add(listener);
            using (var settFile = new StreamReader(new FileStream(settingsPath, FileMode.Open)))
            {
                settings = JsonConvert.DeserializeObject<ServerSettings>(settFile.ReadToEnd());
                tbPort.Text = settings.NetworkPort.ToString();
                tbDayLength.Text = settings.DayCycleLength.ToString();
            }
            var startupMap = (Application.Current as App).AutostartMap;
            if (startupMap != null) 
            {
                LoadMap(startupMap);
                btnStart_Click(this, new RoutedEventArgs());
            }
        }

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            if (core == null)
            {
                core = new SimulationCore();
            }
            if (!core.IsRunning)
            {
                core.Start(settings, world);
                btnStart.Content = "Stop";
            }
            else
            {
                core.Stop();
                btnStart.Content = "Start";
            }
        }

        private void btnLoadMap_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.FileName = "map";
            dialog.DefaultExt = ".json";
            dialog.Filter = "JSON map files (.json)|*.json";
            bool? result = dialog.ShowDialog();
            if (result == true)
            {
                LoadMap(dialog.FileName);
            }
        }

        private void LoadMap(string fileName)
        {
            ForestWorldLogicProvider logicProvider = new ForestWorldLogicProvider();
            using (var map = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                string name;
                world = logicProvider.LoadMap(map, out name);
                tbMapName.Text = name;
            }
            btnStart.IsEnabled = true;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (core != null)
            {
                core.Stop();
            }
            settings.NetworkPort = int.Parse(tbPort.Text);
            settings.DayCycleLength = int.Parse(tbDayLength.Text);
            using (var settFile = new StreamWriter(new FileStream(settingsPath, FileMode.Create)))
            {
                settFile.Write(JsonConvert.SerializeObject(settings, Formatting.Indented));
            }
        }

    }
}
