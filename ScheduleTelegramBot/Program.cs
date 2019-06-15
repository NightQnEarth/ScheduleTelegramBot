using System.IO;
using System.Text;
using System.Threading;

namespace ScheduleTelegramBot // TODO: Change Procfile and others.. 
{
    public static class Program
    {
        private const string TokenStorageName = "ApiAccessToken";

        public static void Main()
        {
            var apiAccessToken = File.ReadAllText(TokenStorageName, Encoding.UTF8);

            new Bot(apiAccessToken).StartReceiving(Timeout.Infinite);
        }
    }
}