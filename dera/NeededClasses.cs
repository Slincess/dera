using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using dera;

namespace dera
{
    public class NeededClasses
    {
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
        public string fileSavedPath { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\simac\files";
    }

    public class Server
    {
        public string IP { get; set; }
        public int Port { get; set; }
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

}
