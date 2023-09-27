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
        
        public Client(Socket handler) 
        { 
            Handler = handler;
            id += 1;
            ID = id;
        }

        

        public int ID { get; }
        public Socket Handler { get; }
        public Thread Thread { get; set;  }

    }
}
