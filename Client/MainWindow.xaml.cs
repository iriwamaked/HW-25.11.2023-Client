using System;
using System.Collections.Generic;
using System.Linq;
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
using System.Net.Sockets;

namespace Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public class ClientSocket
        {
            public string adress { get; set; }
            public short port { get; set; }
            public IPEndPoint point { get; set; }
            public StringBuilder sb { get; set; }
            public Socket clientSocket { get; set; }
            public ClientSocket(string adress, short port)
            {
                this.adress = adress;
                this.port = port;
                point=new IPEndPoint(IPAddress.Parse(adress), port);
                clientSocket = new(point.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                sb = new StringBuilder();
            }

            public async Task Connection()
            {
                await clientSocket.ConnectAsync(point);
            }

            public async Task GetMessage()
            {
                sb.Clear();
                var buffer=new byte[1024];
                var received=await clientSocket.ReceiveAsync(buffer, SocketFlags.None);
                sb.Append(Encoding.Unicode.GetString(buffer, 0, received));
            }

            public async Task SendMessage(string message)
            {
                if (clientSocket == null || !clientSocket.Connected)
                {
                    Console.WriteLine("Socket is not connected.");
                    return;
                }

                byte[] data = Encoding.Unicode.GetBytes(message);
                await clientSocket.SendAsync(data, SocketFlags.None);
            }

            public async Task EndConnection()
            {
                clientSocket.Shutdown(SocketShutdown.Both);
                clientSocket.Close();
            }
            
        }

        ClientSocket cS = null;
        public MainWindow()
        {
            InitializeComponent();
            Messaging.Visibility= Visibility.Hidden;
        }

        private async void ConnectUser_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(IpAdress.Text) && !string.IsNullOrEmpty(Port.Text))
            {
                if(short.TryParse(Port.Text, out short port))
                {
                    cS = new ClientSocket(IpAdress.Text, port);
                    await cS.Connection();
                    MessageBox.Show("З'єднання успішно встановлено");
                    ConnectionGrid.Visibility = Visibility.Hidden;
                }
                else
                {
                    MessageBox.Show("Порт має бути цілим числом");
                }
                
            }
            else
            {
                MessageBox.Show("IP-адреса та порт повині бути заповнені. \n" +
                    "Якщо у вас є складнощі з заповненням, скористайтесь кнопкою для приєднання із параметрами за замовчуванням");
            }
            
        }

        private async void ConnectDefaul_Click(object sender, RoutedEventArgs e)
        {
            cS = new ClientSocket("127.0.0.1", 8080);
            await cS.Connection();
            MessageBox.Show("З'єднання успішно встановлено");
            ConnectionGrid.Visibility = Visibility.Hidden;
            Messaging.Visibility = Visibility.Visible;
        }

        private async void UserSendMessage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await SendMessageAndReceiveResponse();
            }
            catch (Exception ex) 
            { 
                MessageBox.Show(ex.Message);
            }
            
        }

        private async Task SendMessageAndReceiveResponse()
        {
            await cS.SendMessage(UserMessage.Text);
            UserMessage.Text = string.Empty;

            await cS.GetMessage();
            ServerMessage.Text = cS.sb.ToString();
            if (ServerMessage.Text=="bye")
            {
               await EndMessaging();
            }
        }

        private async void UserBye_Click(object sender, RoutedEventArgs e)
        {
           try
            {
                await ByeUserMessage();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private async Task ByeUserMessage()
        {
            await cS.SendMessage("bye");
            MessageBox.Show("З'єднання із сервером перевано. Ви можете підключитись знову або закінчити роботу з програмою");
            Dispatcher.Invoke(() =>
            {
                Messaging.Visibility = Visibility.Hidden;
                ConnectionGrid.Visibility = Visibility.Visible;
            });
        }

        private async Task EndMessaging()
        {
            MessageBox.Show("Зєднання перервано з боку сервера. Спробуйте знову.");
            await cS.EndConnection();
            Dispatcher.Invoke(() =>
            {
                Messaging.Visibility = Visibility.Hidden;
                ConnectionGrid.Visibility = Visibility.Visible;
            });
        }
    }
}
