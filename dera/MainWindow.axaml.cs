using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security;
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
using SixLabors.ImageSharp;
using SkiaSharp;
using Image = Avalonia.Controls.Image;
using Color = Avalonia.Media.Color;
using Avalonia.Media.TextFormatting.Unicode;
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
            message_text_box.KeyDown += Message_text_box_KeyDown;
            file_import_button.Click += FIleImportButtonClicked;
        }

        private void FIleImportButtonClicked(object? sender, RoutedEventArgs e)
        {
            ImageSelectBrn_Click();
        }

        private void Message_text_box_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !string.IsNullOrEmpty(message_text_box.Text))
            {
                currentUsedNetwork.SendMessage(message_text_box.Text);
                message_text_box.Text = "";
            }
        }

        List<Networking> networks = new();
        NetworkingVariables Networkingvariables;
        public Networking currentUsedNetwork;
        public List<ServerBtns> Servers = new();
        private List<UnConnectedServer> UnConnectedServers = new();
        public UserInfo Info = new();
        public ServerBtns UsedServerBtn;

        private void TitleBar_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
                BeginMoveDrag(e);
        }

        private void LoadInfo()
        {
            string AppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\simac";
            if (Directory.Exists(AppDataPath) && Path.Exists(Path.Combine(AppDataPath, "dera.json")))
            {
                HandleServers();
            }
            else
            {
                string newJson = JsonSerializer.Serialize(Info);
                if (!Path.Exists(Path.Combine(AppDataPath, "dera.json")))
                    Directory.CreateDirectory(AppDataPath);

                File.WriteAllText(@$"{AppDataPath}\dera.json", newJson);
            }

            if (!Directory.Exists(Info.fileSavedPath))
            {
                Directory.CreateDirectory(Info.fileSavedPath);
            }
        }//

        private void SaveInfo(UserInfo info)
        {
            string AppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\simac";
            if (Directory.Exists(AppDataPath) && Path.Exists(Path.Combine(AppDataPath, "dera.json")))
            {
                string infojson = JsonSerializer.Serialize(info);

                File.WriteAllText(Path.Combine(AppDataPath, "dera.json"), infojson);
            }
            else
            {
                UserInfo infoNew = new();
                string newJson = JsonSerializer.Serialize(infoNew);
                if (!Path.Exists(Path.Combine(AppDataPath, "dera.json")))
                    Directory.CreateDirectory(AppDataPath);

                File.WriteAllText(@$"{AppDataPath}\dera.json", newJson);
            }
        }//

        private void HandleServers()
        {
            string AppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\simac";
            string json = File.ReadAllText(Path.Combine(AppDataPath, "dera.json"));
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
                            Dispatcher.UIThread.Post(() => { btn.IsEnabled = true; });
                        }
                        else
                        {
                            UnConnectedServer newUCS = new UnConnectedServer();
                            newUCS.server = Server;
                            newUCS.btn = serverbtn;
                            UnConnectedServers.Add(newUCS);
                        }

                    }
                    catch (Exception ex)
                    {
                        UnConnectedServer newUCS = new UnConnectedServer();
                        newUCS.server = Server;
                        newUCS.btn = serverbtn;
                        UnConnectedServers.Add(newUCS);
                        Debug.WriteLine(ex);
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
           Dispatcher.UIThread.InvokeAsync(() => feedback_text.Text = Feedback);
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

        private async void ImageSelectBrn_Click()
        {
            try
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

                await Dispatcher.UIThread.InvokeAsync(() =>
                {

                    if (!file_panel.IsVisible)
                        file_panel.IsVisible = true;
                });

                currentUsedNetwork.serverbtn.selected_image_name = fileName;

                CreatePicturePreview(fileName);
            }
            catch (Exception ex) 
            {
                Debug.Write(ex);
            }
        }

        private void CreatePicturePreview(string Picture)
        {
            try
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    Image NewPreview = new();
                    try
                    {
                        NewPreview.Width = 220;
                        NewPreview.Margin = new(0, 10, 0, 84);
                        file_panel.Children.Add(NewPreview);
                        NewPreview.Source = new Bitmap(Picture);
                    }
                    catch (DirectoryNotFoundException ex)
                    {

                        Debug.WriteLine(ex);
                    }
                });


                using var ms = new MemoryStream();

                using (var image = SixLabors.ImageSharp.Image.Load(currentUsedNetwork.serverbtn.selected_image_name))
                {
                    image.SaveAsPng(ms, new SixLabors.ImageSharp.Formats.Png.PngEncoder());
                    currentUsedNetwork.serverbtn.selected_image = ms.ToArray();
                    ms.Position = 0;
                }
            }
            catch (Exception ex) 
            {
                Debug.WriteLine(ex);
            }
        }

        public void SelectServerbtn(ServerBtns serverbtn)
        {
            try
            {
                if (UsedServerBtn != null) { serverbtn.IsOpened = false; }
                serverbtn.IsOpened = true;
                UsedServerBtn = serverbtn;
                currentUsedNetwork = UsedServerBtn.networking;
            }
            catch (Exception ex) 
            {
                Debug.WriteLine(ex);
            }
        }
    }
}