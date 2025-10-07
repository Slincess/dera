#pragma warning disable
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace basicmessagerapp
{
    public class Networking
    {
        public Form1 Main;

        public TcpClient? client;
        private NetworkStream stream;
        CancellationTokenSource cts;

        private byte[] message;
        private List<DataPacks> datapacks = new();
        private DataPacks MyData;

        public Task response;

        public bool IsClientConnected = false;
        private int messagesCount = 0;

        public ServerBtns serverbtn;
        private static readonly HttpClient client_http = new HttpClient();

        private Server ThisServer = new();
        public async Task<bool> Connect(string ip, int port)
        {
            try
            {
                byte[] name = new byte[5000];
                name = Encoding.UTF8.GetBytes(Main.Info.LastName);
                client = new TcpClient(ip, port);
                stream = client.GetStream();
                cts = new CancellationTokenSource();
                response = Task.Run(() => getmessages());
                stream.Write(name, 0, name.Length);
                ThisServer.IP = ip;
                ThisServer.Port = port;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task disconnect()
        {
            if (IsClientConnected && stream != null)
            {

                var disconnectedSignal = new DataPacks
                {
                    Sender = "ADMIN",
                    Message = "__DISCONNECT__"
                };

                string json = JsonSerializer.Serialize(disconnectedSignal);
                byte[] buffer = Encoding.UTF8.GetBytes(json);

                try
                {
                    await stream.WriteAsync(buffer, 0, buffer.Length);
                }
                catch
                {
                    client.Close();
                    return;
                }

                await Task.Delay(100);
                stream.Close();
                client.Close();
            }
        }

        private bool NameCheck()
        {
            if (String.IsNullOrWhiteSpace(Main.Info.LastName) || Main.Info.LastName == "ADMIN")
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public async Task getmessages()
        {

            while (client.Connected)
            {
                byte[] response_byte = new byte[15000000];
                int response_int = 0;
                try
                {
                    response_int = stream.Read(response_byte);
                }
                catch
                {
                }
                string response_string = Encoding.UTF8.GetString(response_byte, 0, response_int);
                if (response_int == 0)
                {
                    Main.HandleConnectionLostServer(serverbtn, ThisServer);
                    client = null;
                    break;
                }
                if (messagesCount == 0)
                {
                    messagesCount++;
                    SV_Messages Sv_messages = JsonSerializer.Deserialize<SV_Messages>(response_string);
                    Debug.Write(response);
                    try
                    {
                        if (Sv_messages.SV_allMessages != null)
                        {
                            foreach (var item in Sv_messages.SV_allMessages)
                            {
                                Debug.WriteLine(item.Message);
                                serverbtn.MessageListAdd(item.Sender + ": " + item.Message);
                                if(item.Picture != null)
                                {
                                    try
                                    {
                                        string picturePath = await GetPicture(item.Picture, Path.Combine(@"D:\\wa\", item.Picture + ".png")); // returns path on disk
                                        Debug.WriteLine("there is pictures");
                                        System.Drawing.Image img = System.Drawing.Image.FromFile(picturePath);
                                        serverbtn.MessageListAdd_Img(img);
                                    }
                                    catch(Exception e)
                                    {
                                        Debug.WriteLine(e);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.Write(e);
                    }
                }
                else
                {
                    if (response_string.Contains("SV_CCU"))
                    {
                        if (serverbtn.CCU_panel.InvokeRequired)
                        {
                            serverbtn.CCU_panel.Invoke(() => serverbtn.CCU_panel.Controls.Clear());
                        }
                        serverbtn.CCU_panel.Controls.Clear();
                        Users CurrentUsers = JsonSerializer.Deserialize<Users>(response_string);
                        if (CurrentUsers.SV_CCU != null)
                        {
                            foreach (var item in CurrentUsers.SV_CCU)
                            {

                                serverbtn.CCUListAdd(item.CL_Name);
                            }
                        }
                    }
                    if (response_string.Contains("Message"))
                    {
                        Debug.WriteLine(response_string);
                        DataPacks response_DataPacks = JsonSerializer.Deserialize<DataPacks>(response_string);
                        if (response_DataPacks.Message == "__KICK__" && response_DataPacks.Sender == "__SERVER__")
                        {
                            disconnect();
                        }
                        else
                        {
                            serverbtn.MessageListAdd(response_DataPacks.Sender + ": " + response_DataPacks.Message);
                            
                            if(response_DataPacks.Picture != null)
                            {
                                string picturePath = await GetPicture(response_DataPacks.Picture, Path.Combine(@"D:\\wa\" , response_DataPacks.Picture + ".png")); // returns path on disk
                                Debug.WriteLine("there is pictures");
                                System.Drawing.Image img = System.Drawing.Image.FromFile(picturePath);
                                serverbtn.MessageListAdd_Img(img);
                            }

                        }
                    }
                }
            }
        }

        public async Task SendMessage(string Message)
        {
            if (!String.IsNullOrWhiteSpace(Main.Info.LastName))
            {
                DataPacks data = new();
                data.Sender = Main.Info.LastName;
                data.Message = Message;


                if(Main.currentUsedNetwork.serverbtn.selected_image != null)
                {
                    try
                    {
                        string key = await UploadPicture();
                        if (key != null)
                        {
                            data.Picture = key;
                        }

                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e);
                    }
                }

                string dataJson = JsonSerializer.Serialize(data);

                try
                {
                    message = Encoding.UTF8.GetBytes(dataJson);
                    stream.Write(message, 0, message.Length);
                    Debug.WriteLine(Main.currentUsedNetwork.serverbtn.selected_image);
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e + "problem in sending messages");
                    disconnect();
                }
            }
        }

        async Task<string> UploadPicture()
        {
            using (var form = new MultipartFormDataContent())
            {
                var png_content = new ByteArrayContent(Main.currentUsedNetwork.serverbtn.selected_image);
                png_content.Headers.ContentType = MediaTypeHeaderValue.Parse("image/png");
                form.Add(png_content, "png", Main.currentUsedNetwork.serverbtn.selected_image_name);
                var response = await client_http.PostAsync("http://192.168.178.20:5001/api/UploadImage", form);

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    string key = JsonSerializer.Deserialize<string>(json);
                    Debug.WriteLine("Student data uploaded successfully!");
                    return key;
                }
                else
                {
                    Debug.WriteLine($"Failed to upload data: {response.StatusCode}");
                    return null;
                }
            }
        }

        public async Task<string> GetPicture(string key, string savePath)
        {
            var form = new MultipartFormDataContent();
            form.Add(new StringContent(key), "key"); // send key as form field

            try
            {
                var response = await client_http.PostAsync("http://192.168.178.20:5001/api/GetImage", form);
                try
                {
                    if (response.IsSuccessStatusCode)
                    {
                        byte[] imageBytes = await response.Content.ReadAsByteArrayAsync();
                        await File.WriteAllBytesAsync(savePath, imageBytes);
                        return savePath;
                    }
                    else
                    {
                        Debug.WriteLine($"Failed to get image: {response.StatusCode}");
                        return null;
                    }
                }
                catch (Exception a)
                {
                    Debug.WriteLine(a + " there is a problem with proceccing");
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e + " there is problem with response");
            }
            return savePath;
            Debug.WriteLine("asking for photos");
        }
    }
}

public struct NetworkingVariables
{
    public UserInfo info;
    public TcpClient client;
    public NetworkStream? stream;
}

public class CL_UserPack
{
    public int CL_ID { get; set; }
    public string? CL_Name { get; set; }
}
public class Users
{
    public List<CL_UserPack> SV_CCU { get; set; }
}
public class DataPacks
{
    public string? Sender { get; set; }
    public string? Message { get; set; }
    public string? Picture { get; set; }
}

public class SV_Messages
{
    public List<DataPacks> SV_allMessages { get; set; }
}