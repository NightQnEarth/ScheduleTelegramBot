using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using Dapper;
using Npgsql;

namespace ScheduleTelegramBot
{
    public class AccessTokensStorage : IAccessTokensStorage
    {
        private readonly string connectionString;

        public string ApiAccessToken { get; }
        public bool IsEmpty => GetAccessTokens(1).FirstOrDefault() is null;

        public AccessTokensStorage(string dataBaseName, string apiAccessToken)
        {
            connectionString = ConfigurationManager.ConnectionStrings[dataBaseName].ConnectionString;
            ApiAccessToken = apiAccessToken;
        }

        public bool IsValidToken(string accessToken) =>
            ApiAccessToken == accessToken || IsStoredAccessToken(accessToken);

        public void StoreAccessToken(string accessToken)
        {
            const string insertTokenQuery = @"INSERT INTO ""AccessTokens"" (""AccessToken"")
                                                  VALUES (@AccessToken);";

            ExecuteSimpleTokenQuery(insertTokenQuery, accessToken);
        }

        public void DeleteAccessToken(string accessToken)
        {
            const string deleteTokenQuery = @"DELETE FROM ""AccessTokens""
                                                  WHERE ""AccessToken"" = @AccessToken;";

            ExecuteSimpleTokenQuery(deleteTokenQuery, accessToken);
        }

        public IEnumerable<string> GetAccessTokens(int count = -1)
        {
            using IDbConnection connection = new NpgsqlConnection(connectionString);

            var selectTokensQuery = @"SELECT ""AccessToken"" FROM ""AccessTokens""" +
                                    (count > 0 ? $" LIMIT {count};" : ";");

            return connection.Query<string>(selectTokensQuery);
        }

        private void ExecuteSimpleTokenQuery(string sqlQuery, string accessToken)
        {
            using IDbConnection connection = new NpgsqlConnection(connectionString);
            connection.Execute(sqlQuery, new { AccessToken = accessToken });
        }

        private bool IsStoredAccessToken(string accessToken)
        {
            using IDbConnection connection = new NpgsqlConnection(connectionString);

            const string selectTokenQuery = @"SELECT EXISTS(SELECT ""AccessToken"" FROM ""AccessTokens""
                                                  WHERE ""AccessToken"" = @AccessToken);";

            return connection.QuerySingle<bool>(selectTokenQuery, new { AccessToken = accessToken });
        }
    }
}