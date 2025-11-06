using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Timers;
using System.Xml.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;

using dera;

namespace dera
{
    public partial class MainWindow : Window
    {


        public MainWindow()
        {
            InitializeComponent();
            LoadInfo();
            connect_button.Click += ConnectButtonClicked;
            server_add_button.Click += AddServer_Click;
            profile_edit_button.Click += ProfileEdit_Click;
            profile_save_button.Click += ProfileNameSave;
        }

        List<Networking> networks = new();
        NetworkingVariables Networkingvariables;
        public Networking currentUsedNetwork;
        public List<ServerBtns> Servers = new();
        private List<UnConnectedServer> UnConnectedServers = new();
        public UserInfo Info = new();

        /*
        private void textBox1_Enter(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                currentUsedNetwork.SendMessage(textBox1.Text);
                textBox1.Text = "";
                if (currentUsedNetwork.serverbtn.file_panel.Controls.Count != 0)
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
            */


        private void TitleBar_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            // Nur bewegen, wenn die linke Maustaste gedrückt ist
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
                BeginMoveDrag(e);
        }

        private void LoadInfo()
        {
            string AppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\simac";
            if (Directory.Exists(AppDataPath) && Path.Exists(Path.Combine(AppDataPath, "SimacJson.json")))
            {
                //HandleServers();
            }
            else
            {
                string newJson = JsonSerializer.Serialize(Info);
                if (!Path.Exists(Path.Combine(AppDataPath, "SimacJson.json")))
                    Directory.CreateDirectory(AppDataPath);

                File.WriteAllText(@$"{AppDataPath}\SimacJson.json", newJson);
            }
        }//

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
        }//

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
                Info = new UserInfo();
            }
            Info = JsonSerializer.Deserialize<UserInfo>(json) ?? new UserInfo();
            name_text_box.Text = Info.LastName;
            user_name_text.Text = Info.LastName;
            if (Info.ServerIPs.Count > 0)
            {
                foreach (var item in Info.ServerIPs)
                {
                    _ = CreateServer(item);
                }
            }

            HandleUnConnectedServers();
        }//

        private void HandleUnConnectedServers()
        {
            var aTimer = new System.Timers.Timer(2000);
            aTimer.Elapsed += TryConnecting;
            aTimer.AutoReset = true;
            aTimer.Enabled = true;
        }//

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
                            if (success) { UnConnectedServers.Remove(i); await Dispatcher.UIThread.InvokeAsync(() => i.btn.btn.IsEnabled = true); Debug.WriteLine("suc"); break; }
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
            else { return; }
        }//

        public void HandleConnectionLostServer(ServerBtns btn, Server server)
        {
            btn.networking.client = null;
            btn.btn.IsEnabled = false;
            Debug.WriteLine("Btn disabled");
            Debug.WriteLine("Btn panel closed");
            UnConnectedServer NewUCS = new UnConnectedServer();
            NewUCS.server = server;
            NewUCS.btn = btn;
            UnConnectedServers.Add(NewUCS);
            Debug.WriteLine("added to list");
            HandleUnConnectedServers();
        }//

        private async Task<bool> CreateServer(Server Server)
        {
            Button btn = new Button
            {
                Width = 60,
                Height = 58,
                Margin = new(19, 10, 0, 0),
                Background = new SolidColorBrush(Color.Parse("#404144")),
                IsEnabled = false
            };
            ServerBtns serverbtn = new();
            serverbtn.networking.Main = this;
            serverbtn.server_list_index = serverbtn.server_list_index + 1;
            serverbtn.networking.serverbtn = serverbtn;
            serverbtn.btn = btn;
            serverbtn.main = this;

            try
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        bool success = await serverbtn.networking.Connect(Server.IP, Server.Port);
                        if (success)
                        {
                            btn.IsEnabled = true;
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
                    }
                });
            }
            catch (Exception)
            {
                return false;
            }

            btn.Click += serverbtn.ServerButtonClicked;
            server_list.Children.Add(btn);
            Servers.Add(serverbtn);

            return true;
        }

        /*
        public void ReturnErrorText(string ErrorText)
        {
            this.Invoke((Delegate)(() =>
            {
                currentUsedNetwork.serverbtn.MessageListAdd($"CLIENT: {ErrorText}");
            }));
        }
            */

        /*
        public void UpdateUI()
        {
            NameBox.Enabled = !currentUsedNetwork.IsClientConnected;
            textBox1.Enabled = !currentUsedNetwork.IsClientConnected;
        }
        */

        private async void ConnectButtonClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            ConnectionFeedBackClear();
            if (CheckAlreadyJoined(IP_box.Text, PORT_box.Text) && !String.IsNullOrWhiteSpace(Info.LastName))
            {
                int port;
                if (int.TryParse(PORT_box.Text, out port))
                {
                    Networking network = new();
                    networks.Add(network);
                    Server ConnectedServer = new();
                    ConnectedServer.IP = IP_box.Text;
                    ConnectedServer.Port = port;
                    if (await CreateServer(ConnectedServer))
                    {
                        ConnectFeedback("Connected!");
                        Info.ServerIPs.Add(ConnectedServer);
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
        }//

        private void ConnectFeedback(string Feedback)
        {
            feedback_text.Text = Feedback;
        }//

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
        }//

        public void ConnectionFeedBackClear()
        {
            feedback_text.Text = "";
        }//

        private void ProfileNameSave(object? sender, Avalonia.Interactivity.RoutedEventArgs e) //
        {
            if (!string.IsNullOrEmpty(name_text_box.Text)) Info.LastName = name_text_box.Text; SaveInfo(Info); 
            profile_edit_panel.IsVisible = !profile_edit_panel.IsVisible;
            server_adding_panel.IsVisible = false;
        }

        private void ProfileEdit_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e) //
        {
            name_text_box.Text = Info.LastName;
            profile_edit_panel.IsVisible = !profile_edit_panel.IsVisible;
            server_adding_panel.IsVisible = false;
            feedback_panel.IsVisible = false;
        }

        private void AddServer_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            profile_edit_panel.IsVisible = false;
            server_adding_panel.IsVisible = !server_adding_panel.IsVisible;
            feedback_panel.IsVisible = !feedback_panel.IsVisible;
        } //

        private async void ImageSelectBrn_Click(object sender, EventArgs e)
        {
            var dialog = new FilePickerOpenOptions
            {
                Title = "Select a file",
                AllowMultiple = false
            };

            var files = await this.StorageProvider.OpenFilePickerAsync(dialog);

            if (files.Count == 0)
                return;

            var file = files[0];
            var fileName = file.Path.LocalPath;

            // --- Equivalent logic ---
            if (!file_panel.IsVisible)
                file_panel.IsVisible = true;

            currentUsedNetwork.serverbtn.selected_image_name = fileName;

            CreatePicturePreview(fileName);
        }

        private void CreatePicturePreview(string Picture)
        {
            Image NewPreview = new()
            {
                Source = new Bitmap(Picture),
                Width = 220,
                Margin = new(0, 10, 0, 84)
            };
            file_panel.Children.Add(NewPreview);
        }
    }
}