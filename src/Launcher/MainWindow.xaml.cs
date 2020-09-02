using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
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
using System.Xml;
using TestServer_Launcher.LoginAPI;
using WPFS4Launcher;
using Launcher;
using System.Reflection;

namespace Launcher

{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        int count = 0;
        WebClient client = new WebClient();
        private List<string> DownloadList = new List<string>();
        internal string AuthCode = "";
        public byte ButtonState;

        public MainWindow()
        {
            InitializeComponent();
        }

        public string Stats(string url)
        {
            return new WebClient().DownloadString(url);
        }

        private void SelfUpdate()
        {
           
            if (Assembly.GetExecutingAssembly().GetName().Version.ToString() != Stats("http://84.200.24.69/versionviolet.txt"))
            {
                Process.Start("SelfUpdate.exe");
                Environment.Exit(0);
            }

            FileCheck();
        }

        private void FileCheck()
        {

            foreach (var file in Stats("http://84.200.24.69/updatelistviolet.txt").Split('\n'))
            {
                if (string.IsNullOrEmpty(file))
                    continue;

                dbgtx.Content = "checking" + System.IO.Path.GetFileName(Directory.GetCurrentDirectory() + "\\" + file.Split('|')[0]);
                count++;

                if (File.Exists(Directory.GetCurrentDirectory() + "\\" + file.Split('|')[0]))
                {
                    var md5 = System.Security.Cryptography.MD5.Create();
                    FileStream FileStream = new FileStream(Directory.GetCurrentDirectory() + "\\" + file.Split('|')[0], FileMode.Open, FileAccess.Read, FileShare.Read, 8192);

                    if (BitConverter.ToString(md5.ComputeHash(FileStream)).Replace("-", "").ToLowerInvariant() == file.Split('|')[1])
                        DownloadList.Add(file.Split('|')[0]);

                    FileStream.Close();
                }
                else
                    DownloadList.Add(file.Split('|')[0]);

            }
            if (DownloadList.Count == 0)
            {
                ButtonState = 1;
                dbgtx.Content = "Ready for Login";
                Reset();
                return;
            }

            Progessbar2.Maximum = DownloadList.Count();
            count = 0;

            client.DownloadProgressChanged += client_ProgressChanged;
            client.DownloadFileCompleted += client_DownloadCompleted;

            UpdateCheck();
        }

        private void UpdateCheck()
        {
            try
            {
                client.DownloadFileAsync(new Uri("http://84.200.24.69/filesviolet/" + DownloadList[count]), Environment.CurrentDirectory + @"\" + DownloadList[count]);

                count++;
            }
            catch
            {
            }
        }

        public static string FormatSizeBinary(Int64 size)
        {
            string[] sizes = new[] { "B", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
            double formattedSize = size;
            Int32 sizeIndex = 0;
            while (formattedSize >= 1024 & sizeIndex < sizes.Length)
            {
                formattedSize /= 1024;
                sizeIndex += 1;
            }
            return Math.Round(formattedSize, 2).ToString() + sizes[sizeIndex];
        }

        private void client_ProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            try
            {
                dbgtx.Content = $"{string.Format("Downloading ({0}%)", Math.Truncate(e.BytesReceived / (double)e.TotalBytesToReceive * 100))} {DownloadList[count - 1]}";
                Progessbar1.Value = Math.Truncate(e.BytesReceived / (double)e.TotalBytesToReceive * 100);
            }
            catch
            {

            }
        }

        private void client_DownloadCompleted(object sender, AsyncCompletedEventArgs e)
        {
            if (count == DownloadList.Count())
            {
                ButtonState = 1;
                dbgtx.Content = "Ready for Login";
                Reset();
                return;
            }

            UpdateCheck();

            Progessbar2.Value += 1;
        }

        public void Ready(string code)
        {
            Dispatcher.Invoke(() =>
            {
                AuthCode = code;
                ButtonTx.Text = "";
                ButtonState = 2;
                UpdateButton();
            });
        }

        public void UpdateLabel(string message)
        {
            Dispatcher.Invoke(() => { dbgtx.Content = message; });
        }

        public void UpdateErrorLabel(string message)
        {
            Dispatcher.Invoke(() => { errtx_label.Content = message; });
        }

        public string GetUsername()
        {
            return Dispatcher.Invoke(() => { return login_username.Text; });
        }

        public string GetPassword()
        {
            return Dispatcher.Invoke(() => { return login_passwd.Password; });
        }

        public void LoadNotice()
        {
            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load("http://84.200.24.69/changelogviolet.txt");

                foreach (XmlNode node in xmlDoc.FirstChild)
                {
                    NoticeBox.Items.Add(node.Attributes["Log"].Value);
                }
            }
            catch { }

        }

        public void Reset()
        {
            Constants.LoginWindow = this;
            Dispatcher.Invoke(() =>
            {
                Progessbar1.Value = Progessbar1.Maximum;
                Progessbar2.Value = Progessbar2.Maximum;
                ButtonTx.Text = "";
                errtx_label.Content = "";
                dbgtx.Content = "Ready for Authentication.";
                login_passwd.IsEnabled = true;
                login_username.IsEnabled = true;
            });
        }

        public void UpdateButton()
        {
            if (ButtonState == 0)
                Button.Source = new BitmapImage(new Uri("Res/btn_login.png", UriKind.Relative));
            else if (ButtonState == 1)
                Button.Source = new BitmapImage(new Uri("Res/btn_login.png", UriKind.Relative));
            else if (ButtonState == 2)
                Button.Source = new BitmapImage(new Uri("Res/btn_login.png", UriKind.Relative));
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }

        private void Image_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (ButtonState == 1)
            {
                errtx_label.Content = "";

                if (login_passwd.Password.Length < 1 || login_username.Text.Length < 1)
                {
                    errtx_label.Content = "";
                }
                else
                {
                    ButtonTx.Text = "";
                    login_passwd.IsEnabled = false;
                    login_username.IsEnabled = false;
                    Task.Run(() => LoginClient.Connect(Constants.ConnectEndPoint));
                }
            }

            UpdateButton();
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (Save.IsChecked == true)
            {
                Properties.Settings.Default.username = login_username.Text;
                Properties.Settings.Default.password = login_passwd.Password;
                Properties.Settings.Default.Save();
            }
            else
            {
                Properties.Settings.Default.username = "";
                Properties.Settings.Default.password = "";
                Properties.Settings.Default.Save();
            }
        }

        private void Discord_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://discord.gg/nFFtgS9");
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

            if (!Directory.Exists("_resources"))
            {

                MessageBox.Show("Patcher is not in Game Folder!");
                Environment.Exit(0);
            }

          //   Reset();
            if (Stats("http://84.200.24.69/updatelistviolet.txt").Length == 0)
            {
                ButtonState = 1;
                dbgtx.Content = "Ready for Login";
                Reset();

            }
            else
            {
                dbgtx.Content = "Patching";
            }

            SelfUpdate();

            loginstats.Content = "Login status : " + Stats("http://84.200.24.69/Loginserver.txt");
            serverstats.Content = "Server status:  " + Stats("http://84.200.24.69/Gameserver.txt");
            Online.Content = "Player count : " + Stats("http://84.200.24.69/playercountviolet.txt");

            client.DownloadProgressChanged += client_ProgressChanged;
            client.DownloadFileCompleted += client_DownloadCompleted;

            LoadNotice();

            Properties.Settings.Default.Reload();

            if (Properties.Settings.Default.username != "")
            {
                login_username.Text = Properties.Settings.Default.username;
                login_passwd.Password = Properties.Settings.Default.password;
            }
        }

        private void Close_Btn(object sender, MouseButtonEventArgs e)
        {
            Environment.Exit(0);
        }


    }
}

