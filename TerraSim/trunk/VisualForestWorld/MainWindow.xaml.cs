using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TerraSim;
using TerraSim.ManualClient;
using TerraSim.Network;
using TerraSim.Simulation;

namespace VisualForestWorld
{
    enum Item
    {
        Amanita, Mushroom, OrangeFlower, Narcissus, Deer,
        BlueFlower, PurpleFlower, Feeder, Actor
    }
    struct Percept { public string name, property, value; }
    struct Entity
    {
        public string name, type;
        public int x, y;
        public List<Entity> contents;
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const int hexWidth = 50;
        const double hexAngle = Math.PI * 2 / 6;
        readonly double hexSpacingX, hexSpacingY;
        ManualClient client = new ManualClient();
        MemoryStream stringStream = new MemoryStream();
        JsonTextWriter writer = null;
        StreamReader reader = null;
        RenderTargetBitmap buffer;
        DrawingVisual drawingVisual = new DrawingVisual();
        Dictionary<Item, BitmapImage> bitmaps = new Dictionary<Item, BitmapImage>();
        Dictionary<string, Brush> tileBrushes = new Dictionary<string, Brush>();
        HashSet<string> tileTypeNames = new HashSet<string>();
        const string folder = "images\\";
        const int width = 40;
        int agentId = -1;
        string agentName = string.Empty;
        Point screenMidpoint;
        Random rand = new Random();

        public MainWindow()
        {
            InitializeComponent();
            client.Connected += (s, a)
                => { btnConnect.Content = "Disconnect"; };
            client.Disconnected += (s, a)
                => { btnConnect.Content = "Connect"; };
            client.SettingsReceived += OnSettingsRecieved;
            client.ActionListReceived += OnActionListUpdate;
            client.PerceptReceived += OnPerceptReceived;
            hexSpacingX = Math.Sin(hexAngle) * hexWidth * 2;
            hexSpacingY = 1.5 * hexWidth;
            LoadImages();
            tileTypeNames.Add("grass");
            tileTypeNames.Add("grove");
            tileTypeNames.Add("gravel");
            tileTypeNames.Add("rock");
            tileTypeNames.Add("water");
            tileTypeNames.Add("sand");
            screenMidpoint = new Point((int)(imgView.Width / 2),
                (int)(imgView.Height / 2));
            writer = new JsonTextWriter(new StreamWriter(stringStream));
            reader = new StreamReader(stringStream);
        }

        private void LoadImages()
        {
            bitmaps[Item.Actor] = new BitmapImage(
                new Uri(folder + "agent.PNG", UriKind.Relative));
            bitmaps[Item.Amanita] = new BitmapImage(
                new Uri(folder + "ama.PNG", UriKind.Relative));
            bitmaps[Item.BlueFlower] = new BitmapImage(
                new Uri(folder + "fb.PNG", UriKind.Relative));
            bitmaps[Item.Deer] = new BitmapImage(
                new Uri(folder + "deer.PNG", UriKind.Relative));
            bitmaps[Item.Feeder] = new BitmapImage(
                new Uri(folder + "feed.PNG", UriKind.Relative));
            bitmaps[Item.Mushroom] = new BitmapImage(
                new Uri(folder + "m.PNG", UriKind.Relative));
            bitmaps[Item.Narcissus] = new BitmapImage(
                new Uri(folder + "narc.PNG", UriKind.Relative));
            bitmaps[Item.OrangeFlower] = new BitmapImage(
                new Uri(folder + "fo.PNG", UriKind.Relative));
            bitmaps[Item.PurpleFlower] = new BitmapImage(
                new Uri(folder + "fp.PNG", UriKind.Relative));
            tileBrushes["grass"] = new SolidColorBrush(Colors.LimeGreen);
            tileBrushes["grove"] = new SolidColorBrush(Colors.DarkGreen);
            tileBrushes["gravel"] = new SolidColorBrush(Colors.LightGray);
            tileBrushes["rock"] = new SolidColorBrush(Colors.DarkGray);
            tileBrushes["sand"] = new SolidColorBrush(Colors.SandyBrown);
            tileBrushes["water"] = new SolidColorBrush(Colors.RoyalBlue);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            buffer = new RenderTargetBitmap((int)imgView.Width,
                (int)imgView.Height, 96, 96, PixelFormats.Pbgra32);
            imgView.Source = buffer;
        }
                
        private void DrawHex(double center_x, double center_y, 
            DrawingContext context, Brush brush)
        {
            var pts = new LineSegment[5];
            PathGeometry pg = new PathGeometry();
            for (int i = 1; i < 6; i++)
            {
                pts[i-1] = new LineSegment(new Point(center_x + hexWidth * Math.Sin(hexAngle * i),
                    center_y + hexWidth * Math.Cos(hexAngle * i)), true);
            }
            PathFigure pf = new PathFigure(new Point(center_x + hexWidth * Math.Sin(0),
                center_y + hexWidth * Math.Cos(0)), pts, true);
            pg.Figures.Add(pf);
            context.DrawGeometry(brush, new Pen(Brushes.Black, 1), pg);
        }

        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            if (!client.IsConnected)
            {
                int port;
                if (!int.TryParse(tbPort.Text, out port))
                {
                    port = SimulationCore.DefaultPort;
                }
                cbCommand.Items.Clear();
                client.Connect(IPAddress.Parse(tbIP.Text), port);
            }
            else
            {
                client.Disconnect();
            }
        }

        private void btnExecute_Click(object sender, RoutedEventArgs e)
        {
            stringStream.SetLength(0);
            writer.WriteStartObject();
            writer.WritePropertyName("ActionName");
            writer.WriteValue(cbCommand.SelectedValue);
            writer.WritePropertyName("Arg1");
            writer.WriteValue(tbArg1.Text);
            writer.WritePropertyName("Arg2");
            writer.WriteValue(tbArg2.Text);
            writer.WriteEndObject();
            writer.Flush();
            stringStream.Position = 0;
            client.SendMessage(reader.ReadToEnd(), MessageType.Command, MessageFormat.JSON);
        }

        public void OnActionListUpdate(ManualClient sender, Message message)
        {
            var items = from it in JsonConvert.DeserializeObject<string[]>(message.Body)
                        select it;
            cbCommand.Dispatcher.Invoke(new Action(() =>
            {
                cbCommand.Items.Clear();
                foreach (var it in items)
                {
                    cbCommand.Items.Add(it);
                }
            }));
        }
        
        public void OnPerceptReceived(ManualClient sender, Message message)
        {
            var o = JsonConvert.DeserializeObject<JObject>(message.Body);
            var e = o["has_attribute"].ToList();
            tbNames.Dispatcher.Invoke(new Action(() => { ProcessPercept(ref message, e); }));
        }

        private void ProcessPercept(ref Message message, List<JToken> e)
        {
            var percepts = from en in e
                           select new Percept
                           {
                               name = (string)en[0],
                               property = (string)en[1],
                               value = (string)en[2]
                           };
            var perceived = percepts.ToArray();
            var names = ExtractNames(perceived);
            var ent = ExtractEntities(perceived, names);
            var tiles = (from t in ent
                        where tileTypeNames.Contains(t.type)
                        select t).ToArray();
            var tileHashSet = new HashSet<Entity>(tiles);
            var items = (from t in ent
                         where !tileHashSet.Contains(t) && t.name != agentName
                         select t).ToArray();
            var agent = (from t in ent
                         where t.name == agentName
                         select t).ToArray();
            var intensity = perceived.First((r) => { return r.property == "light_intensity"; });
            DrawState(tiles, items, agent[0], 
                (int)Enum.Parse(typeof(IntensityLevel), intensity.value));
        }

        private static LinkedList<Entity> ExtractEntities(Percept[] perceived, string[] names)
        {
            var ent = new LinkedList<Entity>();
            foreach (var name in names)
            {
                var type = (from p in perceived
                            where p.name == name && (p.property == "type")
                            select p.value).GetEnumerator();
                var posx = (from p in perceived
                            where p.name == name && (p.property == "position_x")
                            select p.value).GetEnumerator();
                var posy = (from p in perceived
                            where p.name == name && (p.property == "position_y")
                            select p.value).GetEnumerator();
                if (!type.MoveNext() || !posx.MoveNext() || !posy.MoveNext())
                {
                    continue;
                }
                ent.AddLast(new Entity
                {
                    name = name,
                    type = type.Current,
                    x = int.Parse(posx.Current),
                    y = int.Parse(posy.Current)
                });
            }
            return ent;
        }

        private string[] ExtractNames(Percept[] perceived)
        {
            var names = (from p in perceived
                         select p.name).Distinct().ToArray();
            string allNames = "";
            for (int i = 0; i < names.Length - 1; i++)
            {
                allNames += "{0}, ".Form(names[i]);
            }
            allNames += names[names.Length - 1];
            tbNames.Text = allNames;
            return names;
        }

        private void DrawState(Entity[] tiles, Entity[] items, Entity agent, int lightIntensity)
        {
            if (buffer == null)
            {
                return;
            }
            var part = 200 / (int)IntensityLevel.Full;
            var color = part * lightIntensity;
            agent = PrepareHexes(tiles, items, agent);
            using (DrawingContext drawingContext = drawingVisual.RenderOpen())
            {
                var wholeScreen = new Rect(0, 0, imgView.Width, imgView.Height);
                drawingContext.DrawRectangle(Brushes.White, new Pen(),
                    wholeScreen);
                int agentOffset = agent.y % 2;
                foreach (var hex in tiles)
                {
                    DrawHex(hex, drawingContext, agentOffset);
                }
                drawingContext.DrawRectangle(new SolidColorBrush(
                    Color.FromArgb(100, 255, 255, (byte)(255 - color))),
                    new Pen(), wholeScreen);
                var bmp = bitmaps[Item.Actor];
                drawingContext.DrawImage(bmp, new Rect(screenMidpoint.X - 20,
                    screenMidpoint.Y - 20, bmp.Width * 0.7, bmp.Height * 0.7));
            }
            buffer.Render(drawingVisual);
        }

        private static Entity PrepareHexes(Entity[] tiles, Entity[] items, Entity agent)
        {
            for (int i = 0; i < tiles.Length; i++)
            {
                tiles[i].contents = new List<Entity>(from ent in items
                                                     where ent.x == tiles[i].x
                                                     && ent.y == tiles[i].y
                                                     select ent);
                tiles[i].x -= agent.x;
                tiles[i].y -= agent.y;
            }
            return agent;
        }

        private void DrawHex(Entity hex, DrawingContext context, int offset)
        {
            var offset_x = screenMidpoint.X + (hex.x * hexSpacingX)
                    + (((Math.Abs(hex.y) + 1) % 2 == 1) ? hexSpacingX * 0.5 : 0);
            var offset_y = screenMidpoint.Y + (hex.y * hexSpacingY);
            DrawHex(offset_x, offset_y,
                context, tileBrushes[hex.type]);
            int min = (int)(hexWidth * 0.8 / 2);
            foreach (var obj in hex.contents)
            {
                var bmp = GetBitmap(obj.type);
                context.DrawImage(bmp, 
                    new Rect((int)(offset_x + rand.Next(-min, min)),
                        (int)(offset_y + rand.Next(-min, min)), 
                        bmp.Width / 2, bmp.Height / 2));
            }
        }

        private BitmapImage GetBitmap(string type)
        {
            switch (type)
            {
                case ("amanita"): 
                case ("galerina"): return  bitmaps[Item.Amanita];
                case ("redpine"):
                case ("white"):
                case ("truffle"): return bitmaps[Item.Mushroom];
                case ("animal_feeder"): return bitmaps[Item.Feeder];
                case ("agent"): return bitmaps[Item.Actor];
                case ("fern"): return bitmaps[Item.PurpleFlower];
                case ("snowdrop"): return bitmaps[Item.BlueFlower];
                case ("narcissus"): return bitmaps[Item.Narcissus];
                case ("datura"): return bitmaps[Item.OrangeFlower];
                case ("animal"): return bitmaps[Item.Deer];
            }
            return null;
        }

        public void OnSettingsRecieved(ManualClient sender, Message message)
        {
            agentId = int.Parse(message.Body.Split('=')[1].Trim());
            agentName = "agent_{0}".Form(agentId);
        }
    }
}
