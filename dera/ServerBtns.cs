using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

namespace dera
{
    public class ServerBtns
    {
        
        public struct messages_cach()
        {
            public DataPacks DataP;
            public string? imagePath;
        }


        public int server_list_index;
        public Networking networking = new();
        public MainWindow main;
        public byte[]? selected_image;
        public string? selected_image_name;
        public Button btn;
        public List<messages_cach> cached_messages = new();
        public List<string> cached_CCU = new();
        public bool IsOpened = false;
        public void LoadServer()
        {
            main.SelectServerbtn(this);
            LoadCCU();
            LoadChannels();
            LoadMessages();
        }

        private async Task CCUListAdd(string name)
        {

            TextBlock UserNameLable = new();
            UserNameLable.Text = name;
            UserNameLable.Foreground = Brushes.White;

            Button UserPanel = new();
            UserPanel.Width = 159;
            UserPanel.Height = 38;
            UserPanel.Margin = new(4, 10, 0, 0);
            UserPanel.Background = new SolidColorBrush(Color.Parse("#404144"));
            UserPanel.Content = UserNameLable;

           await Dispatcher.UIThread.InvokeAsync(() => main.CCU_panel.Children.Insert(0, UserPanel));

        }

        private async Task MessageListAdd(messages_cach data)
        {
            try
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    TextBlock message = new();
                    message.Text = data.DataP.Sender + ": " + data.DataP.Message;
                    message.FontSize = 15;

                    if (message.Text.Contains("SERVER:"))
                    {
                        message.Foreground = new SolidColorBrush(Color.Parse("#6FA8A8"));
                    }
                    else
                    {
                        message.Foreground = new SolidColorBrush(Color.Parse("#FFFFFF"));
                    }
                    Dispatcher.UIThread.InvokeAsync(() => main.messages_panel.Children.Insert(0, message));
                    if (data.imagePath != null)
                    {
                        Image pictureBox = new()
                        {
                            Width = 450,
                            Height = 200,
                            Source = new Bitmap(data.imagePath),
                            Stretch = Stretch.Uniform
                        };
                        Dispatcher.UIThread.InvokeAsync(() => main.messages_panel.Children.Insert(0, pictureBox));
                    }
                });
            }
            catch (Exception ex) 
            {
                Debug.WriteLine(ex);
            }
        }

        public async Task MessageAdd(DataPacks datapack, string? imagePath = null)
        {
            try
            {
                messages_cach m_cach = new();
                m_cach.DataP = datapack;
                m_cach.imagePath = imagePath;
                cached_messages.Add(m_cach);


                if (IsOpened)
                {
                    await MessageListAdd(m_cach);
                    Debug.WriteLine("a");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        public async Task CCUAdd(string name)
        {
            string CCU = name;
            cached_CCU.Add(CCU);
            if (IsOpened)
            {
               await CCUListAdd(name);
                
            }
        }

        public void ServerButtonClicked(object? sender, EventArgs e)
        {
            LoadServer();
        }

        public async Task LoadCCU()
        {
           await Dispatcher.UIThread.InvokeAsync(() => main.CCU_panel.Children.Clear());
            foreach (var i in cached_CCU) 
            {
               await CCUListAdd(i);
            }
        }

        public void LoadChannels()
        {

        }

        public async Task LoadMessages()
        {
          await Dispatcher.UIThread.InvokeAsync(() => main.messages_panel.Children.Clear());
            foreach (var i in cached_messages)
            {
               await MessageListAdd(i);
            }
        }
        
    }
}
