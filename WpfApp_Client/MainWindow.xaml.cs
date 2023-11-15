using System;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Windows;
using System.Threading;
using System.Collections.Generic;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace WpfApp_Client
{
    /// <summary>
    /// Logica di interazione per MainWindow.xaml
    /// </summary>
    /// 

    /*  
     *  TODO
     *  
     *   - Aggiunta di (You) nella lista partecipanti
     *      - Comporta che il server invii al client (You) in aggiunta ad ognuno per il suo
     *   - Display del proprio nome utente da qualche parte
     *      - Abbastanza facile
     *   - Tasto per disconnettersi
     *      - Sarà praticamente lo stesso codice del Window_Closing
     *   - Finestra informazioni utente al click nella list box dei partecipanti
     *      - Comporta che il client avrà un oggetto Utente che avrà ogni volta la lista di tutti con le informazioni di ognuno
     *   - Controllo dell'IP
     *      - Split di . e split di : e controllo int
     *   - Con tasto invio invia il messaggio
     *   - Utente che non inserisce il nickname in modalità anonima
     *  
     */

    
    public partial class MainWindow : Window
    {
        static string _s = "";
        
        const string QUIT_STRING = "<CLOSE><EOF>";

        public MainWindow()
        {
            InitializeComponent();
        }

        Socket _connection;
        Thread _listenToServer;
        object _lockConnessione = new object();
        List<Tuple<int, string>> _partecipants;

        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            // Qui va inserito l'IP del SERVER a cui dobbiamo collegarci, che in questo caso è uguale.
            IPAddress ipAddress = IPAddress.Parse(txtIp.Text.Split(':')[0]);

            // Un end point è la combinazione di ip e porta
            IPEndPoint remoteEndPoint = new IPEndPoint(ipAddress, int.Parse(txtIp.Text.Split(':')[1]));

            // Creo la Socket che ascolterà
            if (_connection == null)
            {
                // Creo la socket
                _connection = new Socket(
                    ipAddress.AddressFamily,
                    SocketType.Stream,
                    ProtocolType.Tcp
                );
                try
                {
                    // Mi connetto al server
                    _connection.Connect( remoteEndPoint );
                    
                    // Creo e faccio partire il Thread che gestirà la ricezione dei dati dal server
                    _listenToServer = new Thread(ListenToServer);
                    _listenToServer.Start();

                    // Invio al server la comunicazione dell'entrata nella chat, con anche il nickname
                    _connection.Send(Encoding.UTF8.GetBytes($"<JOIN>{txtNick.Text.Trim()}<EOF>"));

                    // Modifico l'interfaccia utente per mostrare la sezione chat
                    grdConnect.Visibility = Visibility.Collapsed;
                    grdChat.Visibility = Visibility.Visible;
                }
                catch (Exception ex)
                {
                    _connection = null;
                    HandleSocketException(ex);
                }
            }
            else
            {
                MessageBox.Show("Already connected.");
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_connection != null)
            {
                try
                {
                    _connection.Send(Encoding.UTF8.GetBytes(QUIT_STRING));
                }
                catch
                {
                    
                }

                _connection.Shutdown(SocketShutdown.Both);
                _connection.Close();

                _listenToServer.Abort();

                _connection = null;

                lstChat.Items.Clear();
                lstPartecipants.Items.Clear();
                txtMessaggio.Text = "";

            }
        }

        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            if (_connection != null)
            {
                try
                {
                    _connection.Send(Encoding.UTF8.GetBytes(ControllaMessaggio(txtMessaggio.Text) + "<EOF>"));
                }
                catch (Exception ex)
                {
                    HandleSocketException(ex);
                }
            }
        }

        private string ControllaMessaggio(string s)
        {
            return s.Replace("<", "/</").Replace(">","/>/");
        }
        static string RiformattaMessaggio(string s)
        {
            return s.Replace("/</", "<").Replace("/>/", ">");
        }

        private void HandleSocketException(Exception ex)
        {
            MessageBox.Show($"{ex.Message}");

            _connection = null;

            grdChat.Visibility = Visibility.Collapsed;
            grdConnect.Visibility = Visibility.Visible;
        }
        
        private void ListenToServer()
        {

            while (_connection != null && _connection.Connected)
            {
                byte[] bytes = new byte[1024];
                string msgFromServer = "";
                
                try
                {
                    do
                    {
                        int byteRecFromServer = _connection.Receive(bytes); // TODO C'è un errore quando chiudo uno dei due
                        msgFromServer += Encoding.UTF8.GetString(bytes, 0, byteRecFromServer);
                    } while (!msgFromServer.Contains("<EOF>"));
                }
                catch (SocketException ex)
                {
                    Console.WriteLine("Errore Socket: " + ex.Message);
                    return;
                }

                msgFromServer = msgFromServer.Replace("<EOF>", "");

                if (msgFromServer.Contains("<LIST>"))
                {
                    msgFromServer = msgFromServer.Replace("<LIST>", "");
                    string[] msgListSplitted = msgFromServer.Split('\n');
                    Dispatcher.Invoke(() =>
                    {
                        lstPartecipants.Items.Clear();
                        _partecipants = new List<Tuple<int, string>>();

                        Random rnd = new Random();
                        foreach (string line in msgListSplitted)
                        {
                            string lineEdited = line.Trim();

                            // Per evitare che se un utente usa ";" nel suo nickname ci siano casini.
                            string code = rnd.Next(137, 691) + ""; // creo un codice temporaneo
                            if (lineEdited.Contains("/;/"))
                            {
                                lineEdited = lineEdited.Replace("/;/","/"+code+"/"); // lo sostituisco al ;
                            }

                            if (lineEdited != "")
                            {
                                int id = int.Parse(lineEdited.Split(';')[0]);
                                string name = lineEdited.Split(';')[1].Replace($"/{code}/",";"); // risostituisco per output

                                // TODO Finire, devo fare che al click dice informazioni sul partecipante
                                // quindi devo cercare un modo di collegare la listbox e l'id del client.


                                //_partecipants.Add(new Tuple<int,string>());

                                lstPartecipants.Items.Add(name);
                            }
                        }
                    });
                }
                else if (msgFromServer != "")
                {
                    Dispatcher.Invoke(new Action(() =>
                    {
                        lstChat.Items.Add(RiformattaMessaggio(msgFromServer));
                    }));
                }

            }
        }

        private void lstPartecipants_Selected(object sender, RoutedEventArgs e)
        {
            Tuple<int, string> partecipante = new Tuple<int, string>();
            MessageBox.Show($"Nome: {}", "Info sul partecipante");
        }
    }
}
