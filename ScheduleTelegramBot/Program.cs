using System.Configuration;
using System.Threading;

namespace ScheduleTelegramBot
{
    public static class Program
    {
        private const string AppConfigTokenKey = "ApiAccessToken";
        private const string AppConfigDatabaseKey = "DatabaseConnectionString";

        public static void Main()
        {
            var apiAccessToken = ConfigurationManager.AppSettings[AppConfigTokenKey];
            var databaseConnectionStringName = ConfigurationManager.AppSettings[AppConfigDatabaseKey];
            var accessTokensStorage = new AccessTokensStorage(databaseConnectionStringName, apiAccessToken);

            new Bot(accessTokensStorage).StartReceiving(Timeout.InfiniteTimeSpan);
        }
    }
}