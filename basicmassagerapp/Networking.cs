#pragma warning disable
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace basicmessagerapp
{
    public class Networking
    {
        public Form1 Main;

        public TcpClient client { get; private set; }
        private NetworkStream stream;
        CancellationTokenSource cts;

        private byte[] message;
        private List<DataPacks> datapacks = new();
        private DataPacks MyData;

        public Task response;

        public bool IsClientConnected = false;
        private int messagesCount = 0;

        public ServerBtns serverbtn;

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

        public void getmessages()
        {

            while (client.Connected)
            {
                byte[] response_byte = new byte[15000000];
                int response_int = 0;
                try
                {
                    response_int = stream.Read(response_byte);
                }
                catch (Exception)
                {
                    disconnect();
                }
                string response_string = Encoding.UTF8.GetString(response_byte, 0, response_int);
                if (response_int == 0)
                {
                    break;
                }
                if (messagesCount == 0)
                {
                    messagesCount++;
                    SV_Messages Sv_messages = JsonSerializer.Deserialize<SV_Messages>(response_string);
                    Debug.Write(response_string);
                    try
                    {
                        if (Sv_messages.SV_allMessages != null)
                        {
                            foreach (var item in Sv_messages.SV_allMessages)
                            {
                                Debug.WriteLine(item.Message);
                                serverbtn.MessageList_Add(item.Sender + ": " + item.Message);
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
                        if (serverbtn.CCUPanel.InvokeRequired)
                        {
                            serverbtn.CCUPanel.Invoke(() => serverbtn.CCUPanel.Controls.Clear());
                        }
                        serverbtn.CCUPanel.Controls.Clear();
                        Users CurrentUsers = JsonSerializer.Deserialize<Users>(response_string);
                        if (CurrentUsers.SV_CCU != null)
                        {
                            foreach (var item in CurrentUsers.SV_CCU)
                            {

                                serverbtn.CCUList_add(item.CL_Name);
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
                            serverbtn.MessageList_Add(response_DataPacks.Sender + ": " + response_DataPacks.Message);

                            try
                            {
                                byte[] image_byte = Convert.FromBase64String(response_DataPacks.Picture);
                                Bitmap bitmap;
                                using var ms = new MemoryStream(image_byte);
                                bitmap = new Bitmap(ms);

                                Image image;
                                image = bitmap;

                                serverbtn.MessageListAdd_img(image);
                            }
                            catch (Exception e)
                            {
                                Debug.WriteLine(e);

                            }

                        }
                    }
                }
            }
        }

        public void SendMessage(string Message)
        {
            if (!String.IsNullOrWhiteSpace(Main.Info.LastName))
            {
                DataPacks data = new();
                data.Sender = Main.Info.LastName;
                data.Message = Message;

                string dataJson = JsonSerializer.Serialize(data);


                try
                {
                    message = Encoding.UTF8.GetBytes(dataJson);
                    stream.Write(message, 0, message.Length);
                    Debug.WriteLine(Main.currentUsedNetwork.serverbtn.Selectedimage);
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                    disconnect();
                }
            }
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