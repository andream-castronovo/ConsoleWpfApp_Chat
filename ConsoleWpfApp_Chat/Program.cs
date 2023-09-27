using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleWpfApp_Chat
{
    internal class Program
    {
        // Andrea Maria Castronovo 
        //          5°I
        // 

        static int port = 11000;
        static string ip = "10.1.0.7";
        static int maxClient = 10;
        static string quit_string = "c - l - o - s - i - n - g - 2 - 2 - 3 - 4 - 3";
        static void Main(string[] args)
        {
            Console.Title = "Andrea Maria Castronovo - 5°I - 27/09/2023";
            Console.WriteLine("Programma Server");

            // Lista di client da gestire
            List<Client> clients = new List<Client>();

            #region Definizione Socket di ascolto
            
            IPAddress ipAddress = IPAddress.Parse(ip);

            // Un end point è la combinazione di ip e porta
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, port);

            // Creo la Socket che ascolterà
            Socket listener = new Socket(
                ipAddress.AddressFamily,
                SocketType.Stream,
                ProtocolType.Tcp
            );

            listener.Bind(localEndPoint); // Associo la socket all'endpoint
            listener.Listen(maxClient); // Ascolta fino a 10 client al massimo, non bloccante
           
            #endregion


            try
            {
                while (true)
                {
                    try
                    {
                        Client c = new Client(listener.Accept());
                        c.Thread = new Thread(() => GestioneClient(c));
                        c.Thread.Start();
                        clients.Add(c);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Errore " + e.ToString());
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

        }

        static void GestioneClient(Client client)
        {
            Console.WriteLine("Client con id {0} connesso", client.ID);
        }
    }
}
