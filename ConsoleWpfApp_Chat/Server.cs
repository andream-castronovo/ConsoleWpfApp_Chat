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
    internal class Server
    {
        // Andrea Maria Castronovo 
        //          5°I
        // 

        

        static int PORT = 60100;
        static string IP = "10.1.0.7";
        static int MAX_CLIENT = 10;
        static string QUIT_STRING = "c - l - o - s - i - n - g - 2 - 2 - 3 - 4 - 3<EOF>";

        // Fare classe per gestire la lista
        static List<Client> _oldClients = new List<Client>();
        static List<Client> _clients = new List<Client>();

        static Queue<Message> _messagesToBroadcast = new Queue<Message>();


        static void Main(string[] args)
        {
            Console.Title = "Andrea Maria Castronovo - 5°I - 27/09/2023";
            Console.WriteLine("Programma Server");

            // Lista di client da gestire

            #region Definizione Socket di ascolto
            
            IPAddress ipAddress = IPAddress.Parse(IP);

            // Un end point è la combinazione di ip e porta
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, PORT);

            // Creo la Socket che ascolterà
            Socket listener = new Socket(
                ipAddress.AddressFamily,
                SocketType.Stream,
                ProtocolType.Tcp
            );

            listener.Bind(localEndPoint); // Associo la socket all'endpoint
            listener.Listen(MAX_CLIENT); // Ascolta fino a 10 client al massimo, non bloccante

            #endregion

            new Thread(UpdateClients).Start();

            try
            {
                while (true)
                {
                    try
                    {
                        Client c = new Client(listener.Accept());
                        c.Thread = new Thread(() => GestioneClient(c));
                        c.Thread.Start();
                        _clients.Add(c);
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

        static void UpdateClients()
        {
            do
            {
                while (_messagesToBroadcast.Count > 0)
                {
                    Message msg = _messagesToBroadcast.Dequeue();
                    Broadcast($"[{msg.SendTime}] <{msg.Author.Nickname}> {msg.Content}" + "<EOF>");
                }
            } while (true);
        }

        static void Broadcast(string strMsg)
        {
            foreach (Client c in _clients)
            {
                if (c.Handler.Connected)
                {
                    try
                    {
                        c.Handler.Send(Encoding.UTF8.GetBytes(strMsg));
                    }
                    catch
                    {

                    }
                }
            }
        }

        static void GestioneClient(Client client)
        {
            Console.WriteLine("Client con id {0} connesso", client.ID);
            
            byte[] bytes = new byte[1024];
            string msgFromClient;

            do
            {
                msgFromClient = "";
                
                #region RiceviDalClient
                do
                {
                    int byteRecFromServer = client.Handler.Receive(bytes);
                    msgFromClient += Encoding.UTF8.GetString(bytes, 0, byteRecFromServer);
                } while (!msgFromClient.Contains("<EOF>"));
                #endregion

                if (msgFromClient.Contains("<NICKNAME>"))
                {
                    msgFromClient = msgFromClient.Replace("<NICKNAME>", "").Replace("<EOF>","");
                    client.Nickname = msgFromClient;
                    Broadcast($"Nuovo utente nella chat: {client.Nickname}<EOF>");
                    Broadcast($"<JOIN>{client.Nickname}<EOF>");
                }
                else if (msgFromClient == QUIT_STRING)
                {
                    client.Handler.Shutdown(SocketShutdown.Both);
                    client.Handler.Close();
                }
                else
                {
                    _messagesToBroadcast.Enqueue(
                        new Message(client, msgFromClient)
                    );
                }

            } while (client.Handler.Connected);

            Console.WriteLine("Client con id {0} disconnesso", client.ID);
            Broadcast($"Un utente è uscito dalla chat: {client.Nickname}<EOF>");
            Broadcast($"<QUIT>{client.Nickname}<EOF>");
        }
    }
}
