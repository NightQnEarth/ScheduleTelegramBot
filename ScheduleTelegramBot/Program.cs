using System.IO;
using System.Text;
using System.Threading;

namespace ScheduleTelegramBot
{
    public static class Program
    {
        private const string TokenStorageName = "ApiAccessToken";
        private const string TokensCacheFilename = "AccessTokensCache";

        public static void Main()
        {
            var apiAccessToken = File.ReadAllText(TokenStorageName, Encoding.UTF8);
            var accessTokensCache = new AccessTokensCache(apiAccessToken, TokensCacheFilename);

            new Bot(accessTokensCache).StartReceiving(Timeout.Infinite);
        }
    }
}