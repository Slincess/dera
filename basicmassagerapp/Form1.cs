#pragma warning disable
using System.IO;
using System.Text.Json;
using System.IO.Pipes;
using System.Diagnostics;
using basicmessagerapp;
using System.Linq;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using Image = SixLabors.ImageSharp.Image;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using System.Diagnostics.Eventing.Reader;
using System.Collections;
using System.Timers;
namespace basicmessagerapp
{
    public partial class Form1 : Form
    {
        List<Networking> networks = new();
        NetworkingVariables Networkingvariables;
        public Networking currentUsedNetwork;
        public List<ServerBtns> Servers = new();
        private List<UnConnectedServer> UnConnectedServers = new();

        public UserInfo Info = new();

        public Form1()
        {
            InitializeComponent();
            LoadInfo();
            foreach (var item in Servers)
            {
                item.main = this;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

            var tasks = new List<Task>();
            foreach (var item in networks)
            {
                if (item.IsClientConnected)
                    tasks.Add(item.disconnect());
            }

            Task.WhenAll(tasks).Wait();
        }

        private void textBox1_Enter(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                currentUsedNetwork.SendMessage(textBox1.Text);
                textBox1.Text = "";
                if(currentUsedNetwork.serverbtn.file_panel.Controls.Count != 0)
                {
                    currentUsedNetwork.serverbtn.message_list.Location = new System.Drawing.Point
                    {
                        Y = currentUsedNetwork.serverbtn.message_list.Location.Y + currentUsedNetwork.serverbtn.file_panel.Size.Height,
                        X = currentUsedNetwork.serverbtn.message_list.Location.X
                    };
                    currentUsedNetwork.serverbtn.file_panel.Controls.Clear();
                    currentUsedNetwork.serverbtn.selected_image = null;
                }
            }
        }

        private void LoadInfo()
        {
            string AppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\simac";
            if (Directory.Exists(AppDataPath) && Path.Exists(Path.Combine(AppDataPath, "SimacJson.json")))
            {
                HandleServers();
            }
            else
            {
                string newJson = JsonSerializer.Serialize(Info);
                if (!Path.Exists(Path.Combine(AppDataPath, "SimacJson.json")))
                    Directory.CreateDirectory(AppDataPath);

                File.WriteAllText(@$"{AppDataPath}\SimacJson.json", newJson);
            }
        }

        private void SaveInfo(UserInfo info)
        {
            string AppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\simac";
            if (Directory.Exists(AppDataPath) && Path.Exists(Path.Combine(AppDataPath, "SimacJson.json")))
            {
                string infojson = JsonSerializer.Serialize(info);

                File.WriteAllText(Path.Combine(AppDataPath, "SimacJson.json"), infojson);
            }
            else
            {
                UserInfo infoNew = new();
                string newJson = JsonSerializer.Serialize(infoNew);
                if (!Path.Exists(Path.Combine(AppDataPath, "SimacJson.json")))
                    Directory.CreateDirectory(AppDataPath);

                File.WriteAllText(@$"{AppDataPath}\SimacJson.json", newJson);
            }
        }

        private void HandleServers()
        {
            string AppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\simac";
            string json = File.ReadAllText(Path.Combine(AppDataPath, "SimacJson.json"));
            try
            {
                Info = JsonSerializer.Deserialize<UserInfo>(json) ?? new UserInfo();
            }
            catch (JsonException ex)
            {
                MessageBox.Show($"Invalid JSON: {ex.Message}");
                Info = new UserInfo();
            }
            Info = JsonSerializer.Deserialize<UserInfo>(json) ?? new UserInfo();
            NameBox.Text = Info.LastName;
            NameText.Text = Info.LastName;
            if (Info.ServerIPs.Count > 0)
            {
                foreach (var item in Info.ServerIPs)
                {
                   _ = CreateServer(item);
                }
            }

            HandleUnConnectedServers();
            
        }

        private void HandleUnConnectedServers()
        {
            var aTimer = new System.Timers.Timer(2000);
            aTimer.Elapsed += TryConnecting;
            aTimer.AutoReset = true;
            aTimer.Enabled = true;
        }

        private async void TryConnecting(Object source, ElapsedEventArgs e)
        {
            if (UnConnectedServers.Count != 0)
            {
                try
                {
                    foreach (var i in UnConnectedServers)
                    {
                        try
                        {
                            Debug.WriteLine("trying to reconnect");
                            bool success = await i.btn.networking.Connect(i.server.IP, i.server.Port);
                            if (success) { UnConnectedServers.Remove(i); this.Invoke(() => i.btn.btn.Enabled = true); Debug.WriteLine("suc"); break; }
                            else { Debug.WriteLine("failed"); }

                        }
                        catch (Exception a)
                        {
                            Debug.WriteLine(a + "tryConnecting");
                        }

                    }
                }
                catch
                {
                }
            }
            else {return; }
        }

        public void HandleConnectionLostServer(ServerBtns btn, Server server)
        {
            this.Invoke(() =>
            {
                btn.networking.client = null;
                btn.btn.Enabled = false;
                Debug.WriteLine("Btn disabled");
                btn.ClosePanels();
                Debug.WriteLine("Btn panel closed");
                UnConnectedServer NewUCS = new UnConnectedServer();
                NewUCS.server = server;
                NewUCS.btn = btn;
                UnConnectedServers.Add(NewUCS);
                Debug.WriteLine("added to list");
                HandleUnConnectedServers();
            });

        }

        private async Task<bool> CreateServer(Server Server)
        {
            Button btn = new Button
            {
                BackColor = System.Drawing.Color.FromArgb(64, 65, 68),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11F),
                ForeColor = SystemColors.Control,
                Location = new System.Drawing.Point(3, 3),
                Name = Server.IP,
                Size = new System.Drawing.Size(54, 36),
                TabIndex = 0,
                Text = Server.IP,
                UseVisualStyleBackColor = false,
                Enabled = false
            };
            ServerBtns serverbtn = new();
            serverbtn.networking.Main = this;
            serverbtn.server_list_index = serverbtn.server_list_index + 1;
            serverbtn.networking.serverbtn = serverbtn;
            serverbtn.btn = btn;
            try
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        bool success = await serverbtn.networking.Connect(Server.IP, Server.Port);
                        if (success)
                        { 
                            this.Invoke(() => btn.Enabled = true); 
                        }
                        else 
                        {
                            UnConnectedServer newUCS = new UnConnectedServer();
                            newUCS.server = Server;
                            newUCS.btn = serverbtn;
                            UnConnectedServers.Add(newUCS); 
                        }
                        
                    }
                    catch
                    {
                        UnConnectedServer newUCS = new UnConnectedServer();
                        newUCS.server = Server;
                        newUCS.btn = serverbtn;
                        UnConnectedServers.Add(newUCS);
                        throw;
                    }
                });
            }
            catch (Exception)
            {
                return false;
            }
            serverbtn.CCU_panel = new FlowLayoutPanel
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left,
                AutoScroll = true,
                BackColor = System.Drawing.Color.FromArgb(17, 17, 19),
                BorderStyle = BorderStyle.FixedSingle,
                FlowDirection = FlowDirection.TopDown,
                Location = new System.Drawing.Point(75, 47),
                Name = "CCUPANEL",
                Padding = new Padding(10),
                Size = new System.Drawing.Size(181, 495),
                TabIndex = 10,
                WrapContents = false,
                Visible = false
            };

            serverbtn.message_list = new FlowLayoutPanel
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                AutoScroll = true,
                BackColor = System.Drawing.Color.FromArgb(25, 26, 29),
                BorderStyle = BorderStyle.FixedSingle,
                FlowDirection = FlowDirection.TopDown,
                Location = new System.Drawing.Point(262, 12),
                Name = "messagelist",
                RightToLeft = RightToLeft.No,
                Size = new System.Drawing.Size(683, 562),
                TabIndex = 9,
                WrapContents = false,
                Visible = false
            };

            serverbtn.file_panel = new()
            {
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = System.Drawing.Color.FromArgb(25, 26, 29),
                Location = new System.Drawing.Point(272, 420),
                Name = "FilePanel",
                Size = new System.Drawing.Size(613, 156),
                TabIndex = 18,
                Visible = false,
            };

            this.Controls.Add(serverbtn.message_list);
            this.Controls.Add(serverbtn.CCU_panel);
            this.Controls.Add(serverbtn.file_panel);

            btn.Click += serverbtn.ServerButtonClicked;
            servers.Controls.Add(btn);
            Servers.Add(serverbtn);

            return true;
        }

        public void ReturnErrorText(string ErrorText)
        {
            this.Invoke((Delegate)(() =>
            {
                currentUsedNetwork.serverbtn.MessageListAdd($"CLIENT: {ErrorText}");
            }));
        }

        public void UpdateUI()
        {
            NameBox.Enabled = !currentUsedNetwork.IsClientConnected;
            textBox1.Enabled = !currentUsedNetwork.IsClientConnected;
        }

        private async void ConnectBtn_Click(object sender, EventArgs e)
        {
            ConnectionFeedBackClear();
            if (CheckAlreadyJoined(IPbox.Text, PORTBOX.Text) && !String.IsNullOrWhiteSpace(Info.LastName))
            {
                int port;
                if (int.TryParse(PORTBOX.Text, out port))
                {
                    Networking network = new();
                    networks.Add(network);
                    Server ConnectedServer = new();
                    ConnectedServer.IP = IPbox.Text;
                    ConnectedServer.Port = port;
                    if (await CreateServer(ConnectedServer))
                    {
                        ConnectFeedback("Connected!");
                        Info.ServerIPs.Add(ConnectedServer);
                        servers.Controls.Add(new Button
                        {
                            BackColor = System.Drawing.Color.FromArgb(64, 65, 68),
                            FlatStyle = FlatStyle.Flat,
                            Font = new Font("Segoe UI", 11F),
                            ForeColor = SystemColors.Control,
                            Location = new System.Drawing.Point(3, 3),
                            Name = IPbox.Text,
                            Size = new System.Drawing.Size(54, 36),
                            TabIndex = 0,
                            Text = IPbox.Text,
                            UseVisualStyleBackColor = false
                        });
                        SaveInfo(Info);
                    }
                    else
                    {
                        ConnectFeedback("couldnt connect, wrong Ip or port");
                    }
                }
            }
            else if (String.IsNullOrWhiteSpace(Info.LastName)) { ConnectFeedback("name is missing"); }
            else { ConnectFeedback("Ip or Port Missing"); }
        }

        private void ConnectFeedback(string Feedback)
        {
            this.Invoke(() =>
            {
                Label ConnectionFeedBackText = new();
                ConnectionFeedBackText.AutoSize = true;
                ConnectionFeedBackText.Font = new Font("Segoe UI", 10F);
                ConnectionFeedBackText.ForeColor = SystemColors.Control;
                ConnectionFeedBackText.Location = new System.Drawing.Point(3, 3);
                ConnectionFeedBackText.Margin = new Padding(3, 3, 3, 0);
                ConnectionFeedBackText.Name = "ConnectionFeedBackText";
                ConnectionFeedBackText.Size = new System.Drawing.Size(45, 19);
                ConnectionFeedBackText.TabIndex = 0;
                ConnectionFeedBackText.Text = Feedback;
                ConnectionFeedback.Controls.Add(ConnectionFeedBackText);
            });
        }

        private bool CheckAlreadyJoined(string ip, string portString)
        {
            int port;
            bool suc;
            if (suc = int.TryParse(portString, out port))
            {
                foreach (var item in Info.ServerIPs)
                {
                    if (ip != item.IP && port != item.Port)
                    {
                        continue;
                    }
                    else
                    {
                        return true;
                    }
                }
                return true;
            }
            else
            {
                ConnectFeedback("invalid Port");
                return false;
            }
        }

        public void ConnectionFeedBackClear()
        {
            ConnectionFeedback.Controls.Clear();
        }

        private void NameBox_TextChanged(object sender, EventArgs e)
        {
            Info.LastName = NameBox.Text;
            SaveInfo(Info);
        }

        private void ProfileEdit_Click(object sender, EventArgs e)
        {
            ProfilePanel.Visible = !ProfilePanel.Visible;
            ServerConnectPanel.Visible = false;
        }

        private void CloseProfile_Click(object sender, EventArgs e)
        {
            NameText.Text = Info.LastName;
            ProfilePanel.Visible = false;
            ServerConnectPanel.Visible = false;
        }

        private void AddServer_Click(object sender, EventArgs e)
        {
            ProfilePanel.Visible = false;
            ServerConnectPanel.Visible = !ServerConnectPanel.Visible;
        }

        private void ImageSelectBrn_Click(object sender, EventArgs e)
        {
            var fileContent = string.Empty;
            var filePath = string.Empty;

            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                Stream fileStream = null;
                if (openFileDialog.ShowDialog() == DialogResult.OK && (fileStream = openFileDialog.OpenFile()) != null)
                {
                    string fileName = openFileDialog.FileName;
                    using (fileStream)
                    {
                        if (!FilePanel.Visible) currentUsedNetwork.serverbtn.file_panel.Visible = true;

                        currentUsedNetwork.serverbtn.message_list.Location = new System.Drawing.Point
                        {
                            Y = currentUsedNetwork.serverbtn.message_list.Location.Y - currentUsedNetwork.serverbtn.file_panel.Size.Height,
                            X = currentUsedNetwork.serverbtn.message_list.Location.X
                        };
                        currentUsedNetwork.serverbtn.selected_image_name = openFileDialog.FileName;
                        CreatePicturePreview(fileName);
                        
                    }
                }
            }
        }

        private void CreatePicturePreview(string Picture)
        {
            PictureBox NewPreview = new()
            {
                Location = new System.Drawing.Point(3, 3),
                Name = "pictureBox1",
                Size = new System.Drawing.Size(150, 153),
                SizeMode = PictureBoxSizeMode.Zoom,
                TabIndex = 0,
                TabStop = false,
                Margin = new Padding(15, 3, 15, 3),
                BackColor = System.Drawing.Color.FromArgb(25, 23, 29),
            };

            using var ms = new MemoryStream();

            using (var image = Image.Load(Picture))
            {
                image.SaveAsPng(ms, new SixLabors.ImageSharp.Formats.Png.PngEncoder());
                currentUsedNetwork.serverbtn.selected_image = ms.ToArray();
                ms.Position = 0;
                NewPreview.Image = System.Drawing.Image.FromStream(ms);
                //here Upload before sending
            }
            currentUsedNetwork.serverbtn.file_panel.Controls.Add(NewPreview);
        }

        public System.Drawing.Image ImageSharpToSystemDrawing(SixLabors.ImageSharp.Image imgSharp)
        {
            using var ms = new MemoryStream();
            imgSharp.Save(ms, new PngEncoder());
            ms.Position = 0;
            return System.Drawing.Image.FromStream(ms);
        }
    }
}

public struct UnConnectedServer 
{
    public Server server;
    public ServerBtns btn;
};

public class UserInfo
{
    public List<Server> ServerIPs { get; set; } = new();
    public string LastName { get; set; } = "anonym";
}

public class Server
{
    public string IP { get; set; }
    public int Port { get; set; }
}

public class ServerBtns
{
    public int server_list_index;
    public Networking networking = new();
    public FlowLayoutPanel CCU_panel;
    public FlowLayoutPanel message_list;
    public FlowLayoutPanel file_panel;
    public Form1 main;
    public byte[] selected_image;
    public string selected_image_name;
    public Button btn;

    public void ClosePanels()
    {
        CCU_panel.Visible = false;
        message_list.Visible = false;
    }
    public void OpenPanels()
    {
        CCU_panel.Visible = true;
        message_list.Visible = true;
        if(file_panel.Controls.Count > 0) { file_panel.Visible = true; }
    }
    public void CCUListAdd(string name)
    {
        if (CCU_panel.InvokeRequired)
        {
            CCU_panel.Invoke(() => CCUListAdd(name));
            return;
        }

        FlowLayoutPanel UserPanel = new();
        UserPanel.BackColor = System.Drawing.Color.FromArgb(44, 44, 47);
        UserPanel.Location = new System.Drawing.Point(13, 13);
        UserPanel.Padding = new Padding(10);
        UserPanel.Size = new System.Drawing.Size(157, 45);
        UserPanel.TabIndex = 0;

        Label UserNameLable = new();
        UserNameLable.Font = new Font("Segoe UI", 13F);
        UserNameLable.Text = name;
        UserNameLable.ForeColor = System.Drawing.Color.White;
        UserPanel.Controls.Add(UserNameLable);
        CCU_panel.Controls.Add(UserPanel);
       
    }

    public void MessageListAdd_Img(System.Drawing.Image imga)
    {
        if (message_list.InvokeRequired)
        {
            message_list.Invoke(new Action(() => MessageListAdd_Img(imga)));
            return;
        }

        PictureBox pictureBox = new()
        {
            Name = "pictureBox1",
            Size = new System.Drawing.Size(420, 210),
            TabIndex = 19,
            TabStop = false,
            Image = imga,
            SizeMode = PictureBoxSizeMode.Zoom
        };
        message_list.Controls.Add(pictureBox);
    }

    public void MessageListAdd(string text)
    {

        if (message_list.InvokeRequired)
        {
            message_list.Invoke(new Action(() => MessageListAdd(text)));
            return;
        }

        Label message = new();
        message.Text = text;
        message.AutoSize = true;
        message.Font = new Font("Segoe UI", 12F);
        if (text.Contains("SERVER:"))
        {
            message.ForeColor = System.Drawing.Color.FromArgb(111, 168, 168);
        }
        else
        {
            message.ForeColor = System.Drawing.Color.White;
        }
            message_list.Controls.Add(message);
            message_list.ScrollControlIntoView(message_list.Controls[message_list.Controls.Count - 1]);
    }
    public void ServerButtonClicked(object? sender, EventArgs e)
    {
        foreach (var item in main.Servers)
        {
            item.ClosePanels();
        }
        main.currentUsedNetwork = networking;
        OpenPanels();
    }
}


///I dont really know if anyone gonna see this but I feel like I wanna write this here and maybe someone tells me
///what to do.
///So there is a girl called sara, we have been talking for a while now. I really like her and I think she likes me 
///back and thats good but the problem is It seems like I need to leave the country im living right now, I dont know
///how to tell this do I go cold and dont hurt her? or do I tell her and she decides if she wanna go throught the 
///though time with me. I dont know I really dont know tmrw (11.5) it should be clear if I have to leave or not
///and I feel so guilty. yea thats it if you find this and wanna give an advice pls contact me discord: slincess