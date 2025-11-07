using System;
using System.Collections.Generic;
using Avalonia.Controls;
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
        public void LoadServer()
        {
            LoadCCU();
            LoadChannels();
            LoadMessages();
        }

        private void CCUListAdd(string name)
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

            Dispatcher.UIThread.InvokeAsync(() => main.CCU_panel.Children.Insert(0, UserPanel));

        }

        private void MessageListAdd(messages_cach data)
        {
            TextBlock message = new();
            message.Text = data.DataP.Sender + ": " + data. DataP.Message;
            message.FontSize = 15;

            if (message.Text.Contains("SERVER:"))
            {
                message.Foreground = new SolidColorBrush(Color.Parse("#6FA8A8"));
            }
            else
            {
                message.Foreground = new SolidColorBrush(Color.Parse("#FFFFFF"));
            }
            Dispatcher.UIThread.InvokeAsync(() => main.CCU_panel.Children.Insert(0, message));
            if (data.imagePath != null) 
            {
                Image pictureBox = new()
                {
                    Width = 450,
                    Height = 200,
                    Source = new Bitmap(data.imagePath),
                    Stretch = Stretch.Uniform
                };
                Dispatcher.UIThread.InvokeAsync(() => main.CCU_panel.Children.Insert(0, pictureBox));
            }
        }

        public void MessageAdd(DataPacks datapack, string? imagePath = null)
        {
            messages_cach m_cach = new();
            m_cach.DataP = datapack;
            m_cach.imagePath = imagePath;
            cached_messages.Add(m_cach);
        }

        public void CCUAdd(string name)
        {
            string CCU = name;
            cached_CCU.Add(CCU);
        }

        public void ServerButtonClicked(object? sender, EventArgs e)
        {
            LoadServer();
        }

        public void LoadCCU()
        {
            main.CCU_panel.Children.Clear();
            foreach (var i in cached_CCU) 
            {
                CCUListAdd(i);
            }
        }

        public void LoadChannels()
        {

        }

        public void LoadMessages()
        {
            foreach (var i in cached_messages)
            {
                MessageListAdd(i);
            }
        }
        
    }
}
