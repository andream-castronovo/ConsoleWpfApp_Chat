﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Contracts;
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

        static int PORT = 11000;
        static string IP = "192.168.1.61";
        static int MAX_CLIENT = 10;
        static string QUIT_STRING = "<CLOSE><EOF>";

        // Fare classe per gestire la lista
        static List<Client> _oldClients = new List<Client>();
        static List<Client> _clients = new List<Client>();

        // inserire lock per la coda dei messaggi

        static object _lockQueue = new object();
        static Queue<Message> _messagesToBroadcast = new Queue<Message>();

        /*
         * Spiegazione TAG
         * <EOF> End Of File, fine di un messaggio
         * 
         * <JOIN> Serve al server per capire quando un client richiede la modifica di un nickname
         * 
         * <LIST> Serve ai client per capire quando aggiornare la lista partecipanti
         * <QUIT> Serve ai client per capire quando stampare che qualcuno ha abbandonato la chat
         * 
         */

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
                lock (_lockQueue)
                    while (_messagesToBroadcast.Count > 0)
                    {
                        Message msg = _messagesToBroadcast.Dequeue();
                        Broadcast($"[{msg.SendTime}] <{msg.Author.Nickname}> {msg.Content}" + "<EOF>");
                    }
            } while (true);
        }

        static void Broadcast(string strMsg, Client except = null)
        {
            foreach (Client c in _clients)
            {
                if (except != c && c.Handler.Connected)
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

                Console.WriteLine(msgFromClient);


                if (msgFromClient.Contains("<JOIN>"))
                {
                    msgFromClient = msgFromClient.Replace("<JOIN>", "").Replace("<EOF>", "");
                    client.Nickname = msgFromClient;

                    Broadcast($"Nuovo utente nella chat: {client.Nickname}<EOF>");
                    UpdatePartecipantsList();
                }
                else if (msgFromClient == QUIT_STRING)
                {
                    client.Handler.Shutdown(SocketShutdown.Both);
                    client.Handler.Close();
                }
                else
                {
                    lock (_lockQueue)
                    {
                        _messagesToBroadcast.Enqueue(
                            new Message(client, msgFromClient)
                        );
                    }
                }

            } while (client.Handler.Connected);

            client.DataUscita = DateTime.Now;

            _clients.Remove(client);
            _oldClients.Add(client);

            Console.WriteLine("Client con id {0} disconnesso", client.ID);
            Broadcast($"Un utente è uscito dalla chat: {client.Nickname}<EOF>");
            UpdatePartecipantsList();
        }

        private static void UpdatePartecipantsList()
        {
            string listParts = "";
            foreach (Client c in _clients)
            {
                listParts += "[" + c.DataIngresso.ToString("HH/mm/ss") + "] " + c.Nickname + "\n";
            }
            Broadcast($"<LIST>" + listParts + "<EOF>");
        }

    }
}
