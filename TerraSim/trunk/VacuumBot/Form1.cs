using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Windows.Forms;
using TerraSim.Simulation;
using TerraSim.VacuumBot;

namespace VacuumBot
{
    public partial class Form1 : Form
    {
        SimulationCore core = new SimulationCore();
        ISimulationContentProvider logicProvider = new VacuumBotContentProvider();
        World world = null;
        Dictionary<string, Bitmap> sprites = new Dictionary<string, Bitmap>(StringComparer.OrdinalIgnoreCase);
        int spriteWidth, spriteHeight;
        Bitmap buffer = null;
        Graphics graphics = null;
        object @lock = new object();
        HashSet<Tuple<int, int>> shownPanels = new HashSet<Tuple<int, int>>();
        Brush transparent1 = new SolidBrush(Color.FromArgb(180, 0, 0, 0)), 
            transparent2 = new SolidBrush(Color.FromArgb(120, 0, 0, 0));

        MyClient client;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            StartServer();
            LoadImages();

            buffer = new Bitmap(pictureBox1.Width, pictureBox1.Height);
            graphics = Graphics.FromImage(buffer);

            //button1_Click(this, EventArgs.Empty);
        }

        private void StartServer()
        {
            TraceListener listener = new TextWriterTraceListener(Console.Out);
            listener.TraceOutputOptions = TraceOptions.DateTime;
            Trace.Listeners.Add(listener);

            WorldSettings settings = WorldSettings.Default;
            settings.WeatherEnabled = false;
            using (var map = new FileStream("vacuum_map.json", FileMode.Open, FileAccess.Read))
            {
                world = logicProvider.LoadMap(map, settings);
            }
            core.Start(ServerSettings.Default, world);
        }

        private void LoadImages()
        {
            string[] names = new[] { "user_agent", "clean", "dirt", "wall" };
            foreach (var name in names)
            {
                sprites[name] = new Bitmap(string.Format(@"img\{0}.gif", name));
                sprites[name].MakeTransparent(Color.White);
            }
            spriteHeight = sprites[names[0]].Height;
            spriteWidth = sprites[names[0]].Width;
        }

        private void DrawWorld()
        {
            lock (@lock)
            {
                Grid grid = world.Map;
                graphics.Clear(Color.Wheat);
                string type;
                var todo = new List<Action>();
                for (int i = 0; i < world.Map.Height; i++)
                {
                    for (int j = 0; j < world.Map.Width; j++)
                    {
                        foreach (var obj in grid[i, j].Objects)
                        {
                            type = obj.Type;
                            if (type == string.Empty)
                            {
                                throw new Exception("Unknown object type.");
                            }
                            if (type == "agent") //Draw agents at the end
                            {
                                var ii = i;
                                var jj = j;
                                var type2 = type;
                                todo.Add(() =>
                                {
                                    graphics.DrawImage(sprites[type2],
                                        ii * spriteHeight, jj * spriteWidth,
                                        spriteHeight, spriteWidth);
                                });
                            }
                            else
                            {
                                graphics.DrawImage(sprites[type],
                                    i * spriteHeight, j * spriteWidth,
                                    spriteHeight, spriteWidth);
                            }
                        }
                        var t = new Tuple<int, int>(i, j);
                        if (!shownPanels.Contains(t))
                        {
                            graphics.FillRectangle(transparent2,
                                        i * spriteHeight, j * spriteWidth,
                                        spriteHeight, spriteWidth);
                        }
                    }
                }
                foreach (var a in todo)
                {
                    a();
                }
            }
        }

        private void AddToShownPanels(int x, int y)
        {
            var t = new Tuple<int, int>(x, y);
            shownPanels.Add(t);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (client != null)
            {
                client.Disconnect();
            }
            core.Stop();
        }

        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            DrawWorld();
            if (buffer == null)
            {
                e.Graphics.Clear(Color.Beige);
                return;
            }
            e.Graphics.Clear(Color.Red);
            e.Graphics.DrawImageUnscaled(buffer, 0, 0);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            client = new MyClient();
            client.PositionSeen += AddToShownPanels;
            client.Connect(IPAddress.Loopback, SimulationCore.DefaultPort);
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            pictureBox1.Invalidate();
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
        }

    }
}
