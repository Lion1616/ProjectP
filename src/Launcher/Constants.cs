using Launcher;
using System.Net;

namespace WPFS4Launcher
{
    internal class Constants
    {
        public static bool KRClient = false;

        public static MainWindow LoginWindow;
        public static IPEndPoint ConnectEndPoint = new IPEndPoint(IPAddress.Parse("84.200.24.69"), 28001);
    }
}
