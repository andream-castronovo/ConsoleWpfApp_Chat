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


    // Andrea Maria Castronovo
    // 5°I
    // 29/05/2024
    // Client che si connette alla chat di gruppo

    /*  
     *  TODO
     *  
     *   ✔ Aggiunta di (You) nella lista partecipanti
     *   ✔ Tasto per disconnettersi
     *      ✔ Sarà praticamente lo stesso codice del Window_Closing
     *   ✔ Finestra informazioni utente al click nella list box dei partecipanti
     *      ✔ Il server gli passa tutto
     *   ✔ Controllo dell'IP
     *   ✔ Con tasto invio invia il messaggio
     *
     *  Importante:
     *   ✔ Risolvere problemi vari ignorati durante la programmazione (per esempio quando l'ip in connessione non esiste).
     *  
     */


    public partial class MainWindow : Window
    {
        void Errore(string msg)
        {
            MessageBox.Show(
                    msg,
                    "Errore",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
        }

        static string _s = "";
        
        const string QUIT_STRING = "<CLOSE><EOF>";

        public MainWindow()
        {
            InitializeComponent();

            this.Title = "Client chat - Andrea Maria Castronovo - 5°I - 29/05/2024";
        }



        Socket _connection;
        Thread _listenToServer;
        object _lockConnessione = new object();
        List<Tuple<int, string>> _partecipants;
        int _id = -1;

        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {

            if (txtIp.Text.Trim() == "")
            {
                Errore("Non hai scritto nulla nel campo dell'indirizzo");
                return;
            }
            else if (!txtIp.Text.Contains(":"))
            {
                Errore("Non hai inserito una porta (simbolo : tra indirizzo IP e porta)!");
                return;
            }

            if (txtNick.Text.Trim() == "")
            {
                Errore("Non supportiamo utenti senza nome.");
                return;
            }
            else if (txtNick.Text.Contains(">") || txtNick.Text.Contains("<"))
            {
                Errore("Al momento non supportiamo i caratteri \">\" e \"<\" nei nomi.");
                return;
            }


            // Qui va inserito l'IP del SERVER a cui dobbiamo collegarci, che in questo caso è uguale.
            IPAddress ipAddress;
            try
            {
                ipAddress = IPAddress.Parse(txtIp.Text.Split(':')[0]);
            }
            catch (Exception ex)
            {
                Errore(ex);
                return;
            }


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
                MessageBox.Show("Risulti essere già connesso, prova a riavvire l'applicazione.", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Disconnect();
        }

        private void Disconnect()
        {
            if (_connection != null)
            {
                try
                {
                    _connection.Send(Encoding.UTF8.GetBytes(QUIT_STRING));
                }
                catch
                {
                    // Non fare nulla, tanto ti stai disconnettendo
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
            if (txtMessaggio.Text.Trim() == "")
                return;

            if (_connection != null)
            {
                try
                {
                    _connection.Send(Encoding.UTF8.GetBytes(ControllaMessaggio(txtMessaggio.Text) + "<EOF>"));
                    txtMessaggio.Text = "";
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
            Errore(ex);

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
                        int byteRecFromServer = _connection.Receive(bytes);
                        msgFromServer += Encoding.UTF8.GetString(bytes, 0, byteRecFromServer);
                    } while (!msgFromServer.Contains("<EOF>"));
                }
                catch (SocketException ex)
                {
                    Console.WriteLine("Errore Socket: " + ex.Message);
                    return;
                }

                msgFromServer = msgFromServer.Replace("<EOF>", "");
                if (msgFromServer.Contains("<ID>"))
                {
                    msgFromServer = msgFromServer.Replace("<ID>", "");
                    try
                    {
                        _id = int.Parse(msgFromServer);
                    }
                    catch (Exception ex)
                    {
                        Errore(ex);
                    }
                }
                else if (msgFromServer.Contains("<LIST>"))
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
                                lineEdited = lineEdited.Replace("/;/", "/" + code + "/"); // lo sostituisco al ;
                            }

                            if (lineEdited != "")
                            {
                                int id = int.Parse(lineEdited.Split(';')[0]);
                                string name = lineEdited.Split(';')[1].Replace($"/{code}/", ";"); // risostituisco per output

                                if (id == _id)
                                    name += " (You)";

                                _partecipants.Add(new Tuple<int, string>(id, name));

                                lstPartecipants.Items.Add(name);

                            }
                        }
                    });
                }
                else if (msgFromServer.Contains("<INFO>"))
                {
                    //ID:{id};Nome:{c.Nickname};Ingresso:{c.DataIngresso}
                    string outp = "";
                    msgFromServer = msgFromServer.Replace("<INFO>", "");

                    Random rnd = new Random();

                    string codeSemicolon = SafeToSplit(";", rnd, ref msgFromServer);
                    string codeColon = SafeToSplit(":", rnd, ref msgFromServer);

                    string[] msgSplit1 = msgFromServer.Split(';');
                    foreach (string line in msgSplit1)
                    {
                        string lineEdited = line.Trim();
                        //ID:{id}
                        //Nome:{c.Nickname}
                        //Ingresso:{c.DataIngresso}
                        outp += $"{lineEdited.Split(':')[0]}: {lineEdited.Split(':')[1]}\n";
                    }

                    MessageBox.Show(
                        messageBoxText: outp.Replace($"/{codeSemicolon}/", ";").Replace($"/{codeColon}/", ":"),
                        caption: "Informazioni partecipante",
                        button: MessageBoxButton.OK,
                        icon: MessageBoxImage.Information
                    );
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

        private static string SafeToSplit(string whatToChange, Random rSeed, ref string msgFromServer)
        {
            string code = rSeed.Next(137, 691) + ""; // creo un codice temporaneo per ;
            if (msgFromServer.Contains($"/{whatToChange}/"))
            {
                msgFromServer = msgFromServer.Replace($"/{whatToChange}/", "/" + code + "/"); // lo sostituisco al ;
            }
            return code;
        }

        private void lstPartecipants_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if (lstPartecipants.SelectedIndex == -1)
                return;

            Tuple<int, string> partecipante = _partecipants[lstPartecipants.SelectedIndex];
            _connection.Send(Encoding.UTF8.GetBytes($"<INFO>{partecipante.Item1}<EOF>"));
        }

        void Errore(Exception ex)
        {
            MessageBox.Show(
                $"Si è verificato un errore: {ex.Message}",
                "Errore",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }

        private void btnDisconnect_Click(object sender, RoutedEventArgs e)
        {
            Disconnect();
            grdChat.Visibility = Visibility.Collapsed;
            grdConnect.Visibility = Visibility.Visible;
        }

       
        private void txtMessaggio_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                btnSend_Click (sender, e);
            }
        }
    }
}
