using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SelfUpdate
{
    class VioletUpdater
    {
        static WebClient client = new WebClient();
        static bool isfinished = false;

        static void Main(string[] args)
        {
            

            client.DownloadFileCompleted += client_DownloadCompleted;
            Console.WriteLine("Launcher Updater");
            Console.WriteLine("Getting newest Launcher");
            client.DownloadFileAsync(new Uri("http://127.0.0.1/files/Violet.exe"), Environment.CurrentDirectory + @"\Violet.exe");
            while (!isfinished) ;
        }

        private static void DownloadProgressCallback(object sender, DownloadProgressChangedEventArgs e)
        {

            Console.WriteLine("{0}    downloaded {1} of {2} bytes. {3} % complete...",
                (string)e.UserState,
                e.BytesReceived,
                e.TotalBytesToReceive,
                e.ProgressPercentage);
        }

        private static void client_DownloadCompleted(object sender, AsyncCompletedEventArgs e)
        {
            Console.WriteLine("Download complete");
            Process.Start("Violet.exe");
            Environment.Exit(0);
        }
    }
}
