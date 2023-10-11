using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading;

namespace WpfApp_Client
{
    /// <summary>
    /// Logica di interazione per MainWindow.xaml
    /// </summary>
    /// 

    public partial class MainWindow : Window
    {
        const string QUIT_STRING = "c - l - o - s - i - n - g - 2 - 2 - 3 - 4 - 3<EOF>";

        public MainWindow()
        {
            InitializeComponent();
        }

        Socket _connection;
        Thread _listenToServer;

        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            // Qui va inserito l'IP del SERVER a cui dobbiamo collegarci, che in questo caso è uguale.
            IPAddress ipAddress = IPAddress.Parse(txtIp.Text.Split(':')[0]);

            // Un end point è la combinazione di ip e porta
            IPEndPoint remoteEndPoint = new IPEndPoint(ipAddress, int.Parse(txtIp.Text.Split(':')[1]));

            // Creo la Socket che ascolterà
            if (_connection  == null)
            {
                _connection = new Socket(
                    ipAddress.AddressFamily,
                    SocketType.Stream,
                    ProtocolType.Tcp
                );
                try
                {
                    _connection.Connect( remoteEndPoint );
                    
                    _listenToServer = new Thread(ListenToServer);
                    _listenToServer.Start();

                    _connection.Send(Encoding.UTF8.GetBytes($"<NICKNAME>{txtNick.Text}<EOF>"));

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
                    _connection.Shutdown(SocketShutdown.Both);
                    _connection.Close(); 
                }
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
                catch
                {
                    
                }

                msgFromServer = msgFromServer.Replace("<EOF>", "");

                if (msgFromServer.Contains("<LIST>"))
                {
                    msgFromServer = msgFromServer.Replace("<LIST>", "");
                    string[] msgListSplitted = msgFromServer.Split('\n');
                    Dispatcher.Invoke(() =>
                    {
                        lstPartecipants.Items.Clear();
                        foreach (string line in msgListSplitted)
                        {
                            if (line.Trim() != "")
                            {
                                lstPartecipants.Items.Add(line.Replace("\n",""));
                            }
                        }
                    });
                }
                else if (msgFromServer.Contains("<QUIT>"))
                {
                    msgFromServer = msgFromServer.Replace("<QUIT>", "");
                    for (int i = 0; i < lstPartecipants.Items.Count; i++)
                    {
                        if (lstPartecipants.Items[i].ToString() == msgFromServer)
                            Dispatcher.Invoke(() =>
                            {
                                lstPartecipants.Items.RemoveAt(i);
                            });
                    }
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
    }
}
