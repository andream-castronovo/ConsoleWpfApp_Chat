using Microsoft.SqlServer.Server;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleWpfApp_Chat
{
    public class Client
    {
        static int id = 0;

        public Client(Socket handler, string nickname = null)
        {
            Handler = handler;
            id += 1;
            ID = id;

            if (nickname == null)
                nickname = "Anon_" + ID;

            Nickname = nickname;
        }

        public string Nickname { get; set; } 
        public int ID { get; }
        public Socket Handler { get; }
        public Thread Thread { get; set;  }

    }
}
