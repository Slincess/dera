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
//todo:
//join more servers
//change between servers and still have the chats
//SERVER: send only last 30 messages
//collect last 30 message in catch or maybe create panel for every server.
namespace basicmessagerapp
{
    public partial class Form1 : Form
    {
        List<Networking> networks = new();
        NetworkingVariables Networkingvariables;
        public Networking currentUsedNetwork;
        public List<ServerBtns> Servers = new();

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
                if(currentUsedNetwork.serverbtn.FilePanel.Controls.Count != 0)
                {
                    currentUsedNetwork.serverbtn.messagelist.Location = new System.Drawing.Point
                    {
                        Y = currentUsedNetwork.serverbtn.messagelist.Location.Y + currentUsedNetwork.serverbtn.FilePanel.Size.Height,
                        X = currentUsedNetwork.serverbtn.messagelist.Location.X
                    };
                    currentUsedNetwork.serverbtn.FilePanel.Controls.Clear();
                    currentUsedNetwork.serverbtn.Selectedimage = null;
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

        private void ServerButtonClicked(object? sender, EventArgs e)
        {

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
            serverbtn.ServerListIndex = serverbtn.ServerListIndex + 1;
            serverbtn.networking.serverbtn = serverbtn;
            try
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        bool success = await serverbtn.networking.Connect(Server.IP, Server.Port);
                        if (success) { this.Invoke(() => btn.Enabled = true); };
                    }
                    catch
                    {

                        throw;
                    }
                });
            }
            catch (Exception)
            {
                return false;
            }
            serverbtn.CCUPanel = new FlowLayoutPanel
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

            serverbtn.messagelist = new FlowLayoutPanel
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

            serverbtn.FilePanel = new()
            {
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = System.Drawing.Color.FromArgb(25, 26, 29),
                Location = new System.Drawing.Point(272, 420),
                Name = "FilePanel",
                Size = new System.Drawing.Size(613, 156),
                TabIndex = 18,
                Visible = false,
            };

            this.Controls.Add(serverbtn.messagelist);
            this.Controls.Add(serverbtn.CCUPanel);
            this.Controls.Add(serverbtn.FilePanel);

            btn.Click += serverbtn.ServerButtonClicked;
            servers.Controls.Add(btn);
            Servers.Add(serverbtn);

            return true;
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

        public void ReturnErrorText(string ErrorText)
        {
            this.Invoke((Delegate)(() =>
            {
                currentUsedNetwork.serverbtn.MessageList_Add($"CLIENT: {ErrorText}");
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
            ProfilePanel.Visible = true;
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
            ServerConnectPanel.Visible = true;
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
                        if (!FilePanel.Visible) currentUsedNetwork.serverbtn.FilePanel.Visible = true;

                        currentUsedNetwork.serverbtn.messagelist.Location = new System.Drawing.Point
                        {
                            Y = currentUsedNetwork.serverbtn.messagelist.Location.Y - currentUsedNetwork.serverbtn.FilePanel.Size.Height,
                            X = currentUsedNetwork.serverbtn.messagelist.Location.X
                        };

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
                currentUsedNetwork.serverbtn.Selectedimage = ms.ToArray();

                ms.Position = 0;
                NewPreview.Image = System.Drawing.Image.FromStream(ms);
            }
            currentUsedNetwork.serverbtn.FilePanel.Controls.Add(NewPreview);
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
    public int ServerListIndex;
    public Networking networking = new();
    public FlowLayoutPanel CCUPanel;
    public FlowLayoutPanel messagelist;
    public FlowLayoutPanel FilePanel;
    public Form1 main;
    public byte[] Selectedimage;
    private void ClosePanels()
    {
        CCUPanel.Visible = false;
        messagelist.Visible = false;
    }
    private void OpenPanels()
    {
        CCUPanel.Visible = true;
        messagelist.Visible = true;
        if(FilePanel.Controls.Count > 0) { FilePanel.Visible = true; }
    }
    public void CCUList_add(string name)
    {
        if (CCUPanel.InvokeRequired)
        {
            CCUPanel.Invoke(() => CCUList_add(name));
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
        CCUPanel.Controls.Add(UserPanel);
       
    }

    public void MessageListAdd_img(System.Drawing.Image imga)
    {
        if (messagelist.InvokeRequired)
        {
            messagelist.Invoke(new Action(() => MessageListAdd_img(imga)));
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
        messagelist.Controls.Add(pictureBox);
    }

    public void MessageList_Add(string text)
    {

        if (messagelist.InvokeRequired)
        {
            messagelist.Invoke(new Action(() => MessageList_Add(text)));
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
            messagelist.Controls.Add(message);
            messagelist.ScrollControlIntoView(messagelist.Controls[messagelist.Controls.Count - 1]);
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