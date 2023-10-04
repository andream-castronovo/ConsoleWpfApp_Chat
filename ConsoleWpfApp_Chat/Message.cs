using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleWpfApp_Chat
{
    public class Message
    {
        public Client Author { get; set; }
        // Client, DateTime, string
        public DateTime SendTime { get; set; }
        public string Content { get; set; } 

        public Message(Client author, string content) 
        { 
            Author = author;
            Content = content.Replace("<EOF>", "");
            SendTime = DateTime.Now;
        }
    }
}
