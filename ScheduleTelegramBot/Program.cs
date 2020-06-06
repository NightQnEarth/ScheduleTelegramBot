using System.Configuration;
using System.Threading;

namespace ScheduleTelegramBot
{
    public static class Program
    {
        private const string AppConfigTokenKey = "ApiAccessToken";
        private const string AppConfigDatabaseKey = "Database";

        public static void Main()
        {
            var apiAccessToken = ConfigurationManager.AppSettings[AppConfigTokenKey];
            var databaseName = ConfigurationManager.AppSettings[AppConfigDatabaseKey];
            var accessTokensStorage = new AccessTokensStorage(databaseName, apiAccessToken);

            new Bot(accessTokensStorage).StartReceiving(Timeout.InfiniteTimeSpan);
        }
    }
}