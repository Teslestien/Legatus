using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Newtonsoft.Json;

namespace Legatus
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public class Message
    {
        public string Sender { get; set; }
        public string Time { get; set; }
        public string Content { get; set; }
    }

    public partial class MainWindow : Window
    {
        readonly Thread t;
        string user;
        string URL;
        public MainWindow()
        {
            InitializeComponent();

            ServerURL.Text = URL = Properties.Settings.Default.ServerURLSetting;
            Username.Text = user = Properties.Settings.Default.UsernameSetting;
            BackgroundImage.Text = Properties.Settings.Default.BackgroundImageSetting;
            OptionsMenu.Visibility = Visibility.Collapsed;

            try
            {
                MainContainer.Background = new ImageBrush(new BitmapImage(new Uri(Properties.Settings.Default.BackgroundImageSetting)));
            }
            catch
            {
                MainContainer.Background = Brushes.SkyBlue;
                Properties.Settings.Default.BackgroundImageSetting = "";
            }

            Today.Text = DateTime.Now.ToLongDateString();

            t = new Thread(new ThreadStart(MessageManager));
            t.Start();
        }

        public void MessageManager()
        {
            var web = new WebClient();
            var url = $"{URL}receive?user={user}";
            string downloadString = "[]\n";
            try
            {
                downloadString = web.DownloadString(url);
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to receive because: " + e.Message);
                MessageManager();
                return;
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
            var web = new WebClient();
            var url = $"{URL}send?user={user}&message={message}";
            try
            {
                web.DownloadString(url);
            }
            catch (Exception exception)
            {
                System.Windows.MessageBox.Show("Reason: " + exception.Message, "Failed to connect to server", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.Yes);
                return;
            }

            GenerateMessage(user, message, DateTime.Now.TimeOfDay.ToString());
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

        private void OptionsClick(object sender, RoutedEventArgs e)
        {
            OptionsMenu.Visibility = Visibility.Visible;
        }

        private void CancelOptions(object sender, RoutedEventArgs e)
        {
            ServerURL.Text = Properties.Settings.Default.ServerURLSetting;
            Username.Text = Properties.Settings.Default.UsernameSetting;
            BackgroundImage.Text = Properties.Settings.Default.BackgroundImageSetting;
            OptionsMenu.Visibility = Visibility.Collapsed;
        }

        private void SaveOptions(object sender, RoutedEventArgs e)
        {

            if (ServerURL.Text.Last() != '/')
            {
                ServerURL.Text += "/";
            }
            URL = Properties.Settings.Default.ServerURLSetting = ServerURL.Text;
            user = Properties.Settings.Default.UsernameSetting = Username.Text;
            Properties.Settings.Default.BackgroundImageSetting = BackgroundImage.Text;
            Properties.Settings.Default.Save();
            try
            {
                MainContainer.Background = new ImageBrush(new BitmapImage(new Uri(Properties.Settings.Default.BackgroundImageSetting)));
            }
            catch
            {
                MainContainer.Background = Brushes.SkyBlue;
                Properties.Settings.Default.BackgroundImageSetting = "";
            }
        }

        private void OpenImage(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                FileName = "Image", // Default file name
                DefaultExt = ".png", // Default file extension
                Filter = "All supported graphics|*.jpg;*.jpeg;*.png|" +
              "JPEG (*.jpg;*.jpeg)|*.jpg;*.jpeg|" +
              "Portable Network Graphic (*.png)|*.png" // Filter files by extension
            };

            // Show open file dialog box
            bool? result = dialog.ShowDialog();

            // Process open file dialog box results
            if (result == true)
            {
                BackgroundImage.Text = dialog.FileName;
                Console.WriteLine(dialog.FileName);
            }
        }
    }
}
