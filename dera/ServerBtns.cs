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
            public DataPacks message;
            public string? imagePath;
        }


        public int server_list_index;
        public Networking networking = new();
        public MainWindow main;
        public byte[]? selected_image;
        public string? selected_image_name;
        public Button btn;
        public List<messages_cach> cached_messages = new();
        public List<string> CCU_cache = new();
        public void LoadServer()
        {
            LoadCCU();
            LoadChannels();
            LoadMessages();
        }
        public void CCUListAdd(string name)
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

           Dispatcher.UIThread.InvokeAsync(() => main.CCU_panel.Children.Insert(0,UserPanel));

        }

        public void MessageAdd(DataPacks datapack, string? imagePath = null)
        {
            messages_cach m_cach = new();
            m_cach.message = datapack;
            m_cach.imagePath = imagePath;
            cached_messages.Add(m_cach);
        }

        public void ServerButtonClicked(object? sender, EventArgs e)
        {
            LoadServer();
        }

        public void LoadCCU()
        {
            main.CCU_panel.Children.Clear();
            foreach (var i in CCU_cache) 
            {
                
            }
        }

        public void LoadChannels()
        {

        }

        public void LoadMessages()
        {

        }
        
    }
}
