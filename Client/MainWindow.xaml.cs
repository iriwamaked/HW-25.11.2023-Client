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
using System.IO;

namespace Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public class Client
        {
            public string login { get; set; }
            public string password { get; set; }
            public IPEndPoint point { get; set; }
            public StringBuilder sb { get; set; }
            public TcpClient client { get; set; }

            public NetworkStream stream { get; set; }
            public Client(string login, string password)
            {
                this.login = login;
                this.password = password;
                point = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8080);
                client = new TcpClient();
                sb = new StringBuilder();
               
            }

            public async Task SendCredentials()
            {
                string credentialMessage = $"{login},{password}";
                await SendMessage(credentialMessage);
            }
            public async Task<bool> Connection()
            {
                await client.ConnectAsync(point);
                stream = client.GetStream();
                await SendCredentials();
                await GetMessage();
                string serverMessage = sb.ToString().Trim();
                if (serverMessage == "Later")
                {
                    MessageBox.Show("Cервер перевантажений. Спробуйте пізніше");
                    return false;
                }
                else if (serverMessage == "InvalidCredentials")
                {
                    MessageBox.Show("Логін чи пароль зазначені не вірно. Спробуйте ще раз.");
                    return false;
                }
                else
                {
                    MessageBox.Show("З'єднання успішно встановлено");
                    return true;
                }
            }

            public async Task GetMessage()
            {
                sb.Clear();
                var buffer = new byte[1024];
                int bytes = 0;
                do
                {
                    bytes = await stream.ReadAsync(buffer, 0, buffer.Length);
                    sb.Append(Encoding.Unicode.GetString(buffer, 0, bytes));
                }
                while (stream.DataAvailable);
            }
            public async Task SendMessage(string message)
            {
                if (client == null || !client.Connected)
                {
                    Console.WriteLine("Client is not connected.");
                    return;
                }

                byte[] data = Encoding.Unicode.GetBytes(message);
                await stream.WriteAsync(data, 0, data.Length);
            }

            public async Task EndConnection()
            {
                client.Close();
            }

        }

        Client cS = null;
        public MainWindow()
        {
            InitializeComponent();
            Messaging.Visibility = Visibility.Hidden;
        }

        //private async void ConnectUser_Click(object sender, RoutedEventArgs e)
        //{
        //    if (!string.IsNullOrEmpty(IpAdress.Text) && !string.IsNullOrEmpty(Port.Text))
        //    {
        //        if(short.TryParse(Port.Text, out short port))
        //        {
        //            cS = new Client(IpAdress.Text, port);
        //            if(await cS.Connection() == true)
        //            {
        //                ConnectionGrid.Visibility = Visibility.Hidden;
        //                Messaging.Visibility = Visibility.Visible;
        //            }

        //        }
        //        else
        //        {
        //            MessageBox.Show("Порт має бути цілим числом");
        //        }           
        //    }
        //    else
        //    {
        //        MessageBox.Show("IP-адреса та порт повині бути заповнені. \n" +
        //            "Якщо у вас є складнощі з заповненням, скористайтесь кнопкою для приєднання із параметрами за замовчуванням");
        //    }

        //}

        private async void ConnectDefaul_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(LoginText.Text) && !string.IsNullOrEmpty(PasswordText.Text))
            {
                cS = new Client(LoginText.Text, PasswordText.Text);
                if (await cS.Connection() == true)
                {
                    ConnectionGrid.Visibility = Visibility.Hidden;
                    Messaging.Visibility = Visibility.Visible;
                }
                else
                {
                    MessageBox.Show("Cервер відхилив з'єднання. Логін або пароль не вірні. Спробуйте ще раз");
                }
            }
            else
            {
                MessageBox.Show("Логін або пароль не зазначені");
            }

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
            await WriteMessageAsync(cS.stream, UserMessage.Text);

            if (UserMessage.Text.ToLower() == "bye")
            {
                await EndMessaging();
                return;
            }

            UserMessage.Text = string.Empty;

            ServerMessage.Text = await ReadMessageAsync(cS.stream);

            if (ServerMessage.Text.ToLower() == "bye")
            {
                await EndMessaging();
            }
        }

        public static async Task<string> ReadMessageAsync(NetworkStream stream)
        {
            byte[] data = new byte[1024];
            StringBuilder sb = new StringBuilder();
            int bytes = 0;

            do
            {
                bytes = await stream.ReadAsync(data, 0, data.Length);
                sb.Append(Encoding.Unicode.GetString(data, 0, bytes));
            }
            while (stream.DataAvailable);

            return sb.ToString();
        }

        public static async Task WriteMessageAsync(NetworkStream stream, string message)
        {
            byte[] data = Encoding.Unicode.GetBytes(message);
            await stream.WriteAsync(data, 0, data.Length);
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
