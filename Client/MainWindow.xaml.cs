using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Newtonsoft.Json;

namespace Legatus
{
    // <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 

    public class Message
    {
        public string Sender { get; set; }
        public string Time { get; set; }
        public string Content { get; set; }
    }

    public partial class MainWindow : Window
    {
        readonly Thread t;
        readonly string user = "Questo";
        public MainWindow()
        {
            InitializeComponent();

            Today.Text = DateTime.Now.ToLongDateString();

            t = new Thread(new ThreadStart(MessageManager));
            t.Start();
        }

        public void MessageManager()
        {
            var web = new WebClient();
            var url = $"https://legatus.teslestien.repl.co/receive?user={user}";
            string downloadString = "[]\n";
            try
            {
                downloadString = web.DownloadString(url);
            }
            catch
            {
                MessageManager();
            }
            Console.WriteLine("Download string: " + downloadString);
            if (downloadString != "[]\n")
            {
                List<Message> ReceivedMessages = JsonConvert.DeserializeObject<List<Message>>(downloadString);

                foreach (var item in ReceivedMessages)
                {
                    Dispatcher.Invoke(() =>
                    {
                        GenerateMessage(item.Sender, item.Content, item.Time);
                    });
                }
                MessageManager();
            }
            else
            {
                Thread.Sleep(1000);
                MessageManager();
            }
        }

        private void SendMessage(object sender, RoutedEventArgs e)
        {
            string message = Encrypt(new TextRange(MessageBox.Document.ContentStart, MessageBox.Document.ContentEnd).Text, "*");
            GenerateMessage(user, message, DateTime.Now.TimeOfDay.ToString());

            var web = new WebClient();
            var url = $"https://legatus.teslestien.repl.co/send?user={user}&message={message}";
            web.DownloadString(url);

            MessageBox.Document.Blocks.Clear();
        }

        private void GenerateMessage(string sender, string content, string time)
        {

            Border msgBorder = new Border();
            StackPanel msgStackPanel = new StackPanel();
            StackPanel senderNameAndTimeInfo = new StackPanel();
            TextBox msgSender = new TextBox();
            TextBox msgContent = new TextBox();
            TextBox msgSendTime = new TextBox();

            msgBorder.Margin = new Thickness(10);
            msgBorder.MinWidth = 400;
            msgBorder.BorderThickness = new Thickness(10);
            msgBorder.Background = Brushes.White;
            msgBorder.BorderBrush = Brushes.Black;
            msgBorder.CornerRadius = new CornerRadius(15);

            msgBorder.Child = msgStackPanel;

            msgStackPanel.MinHeight = 25;
            senderNameAndTimeInfo.Orientation = Orientation.Horizontal;

            msgSender.Margin = new Thickness(5);
            msgSender.Text = sender;
            msgSender.FontSize = 18;
            msgSender.BorderThickness = new Thickness(0);
            msgSender.IsReadOnly = true;
            msgSender.HorizontalAlignment = HorizontalAlignment.Left;

            msgSendTime.Margin = new Thickness(5);
            msgSendTime.Text = time;
            msgSendTime.FontSize = 12;
            msgSendTime.BorderThickness = new Thickness(0);
            msgSendTime.IsReadOnly = true;
            msgSendTime.VerticalAlignment = VerticalAlignment.Bottom;
            msgSendTime.HorizontalAlignment = HorizontalAlignment.Left;

            msgContent.Margin = new Thickness(5);
            msgContent.FontSize = 14;
            msgContent.TextWrapping = TextWrapping.Wrap;
            msgContent.BorderThickness = new Thickness(0);
            msgContent.IsReadOnly = true;

            msgBorder.VerticalAlignment = VerticalAlignment.Bottom;

            msgStackPanel.HorizontalAlignment = HorizontalAlignment.Left;
            msgStackPanel.VerticalAlignment = VerticalAlignment.Bottom;

            if (sender == user)
            {
                msgSender.Foreground = Brushes.Blue;
                msgSendTime.Foreground = Brushes.Blue;
                msgBorder.HorizontalAlignment = HorizontalAlignment.Right;
                msgContent.Text = Decrypt(content, "*");
            }
            else
            {
                msgContent.Text = Decrypt(content, "*");
                msgSender.Foreground = Brushes.Green;
                msgSendTime.Foreground = Brushes.Green;
                msgBorder.HorizontalAlignment = HorizontalAlignment.Left;
            }

            senderNameAndTimeInfo.Children.Add(msgSender);
            senderNameAndTimeInfo.Children.Add(msgSendTime);
            msgStackPanel.Children.Add(senderNameAndTimeInfo);
            msgStackPanel.Children.Add(msgContent);


            MessageContainer.Children.Add(msgBorder);



            ScrollBar.ScrollToEnd();
        }

        private void EnterSend(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return && Keyboard.IsKeyDown(Key.LeftShift))
            {
                MessageBox.AppendText(Environment.NewLine);
                MessageBox.CaretPosition = MessageBox.CaretPosition.DocumentEnd;
                e.Handled = true;
                return;
            }
            if (e.Key == Key.Return)
            {
                e.Handled = true;
                SendMessage(null, null);
                return;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Put your close code here
            t.Abort();
            Application.Current.Shutdown();
            Process.GetCurrentProcess().Kill();
        }

        //Encryption and Decryption functions
        public static string Encrypt(string clearText, string EncryptionKey)
        {
            using (Aes encryptor = Aes.Create())
            {
                byte[] clearBytes = Encoding.Unicode.GetBytes(clearText);
                Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(EncryptionKey, new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(clearBytes, 0, clearBytes.Length);
                        cs.Close();
                    }
                    clearText = Convert.ToBase64String(ms.ToArray());
                }
            }
            return clearText;
        }
        public static string Decrypt(string cipherText, string EncryptionKey)
        {
            try
            {
                byte[] cipherBytes = Convert.FromBase64String(cipherText);
                using (Aes encryptor = Aes.Create())
                {
                    Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(EncryptionKey, new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
                    encryptor.Key = pdb.GetBytes(32);
                    encryptor.IV = pdb.GetBytes(16);
                    using (MemoryStream ms = new MemoryStream())
                    {
                        using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write))
                        {
                            cs.Write(cipherBytes, 0, cipherBytes.Length);
                            cs.Close();
                        }
                        cipherText = Encoding.Unicode.GetString(ms.ToArray());
                    }
                }
            }
            catch
            {
                ;
            }
            return cipherText;
        }
    }
}
