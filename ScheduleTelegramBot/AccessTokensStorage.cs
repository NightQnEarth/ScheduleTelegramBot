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

            CreateTable(AccessTokensHelper.TokenLength);
        }

        public bool IsValidToken(string accessToken) =>
            ApiAccessToken == accessToken || IsStoredAccessToken(accessToken);

        public void StoreAccessToken(string accessToken)
        {
            using IDbConnection connection = new NpgsqlConnection(connectionString);

            const string insertTokenQuery = @"INSERT INTO access_tokens (access_token)
                                                  VALUES (@AccessToken);";

            connection.Execute(insertTokenQuery, new { AccessToken = accessToken });
        }

        public void DeleteAccessToken(string accessToken)
        {
            using IDbConnection connection = new NpgsqlConnection(connectionString);

            const string deleteTokenQuery = @"DELETE FROM access_tokens
                                                  WHERE access_token = @AccessToken;";

            connection.Execute(deleteTokenQuery, new { AccessToken = accessToken });
        }

        public IEnumerable<string> GetAccessTokens(int count = -1)
        {
            using IDbConnection connection = new NpgsqlConnection(connectionString);

            var selectTokensQuery = @"SELECT access_token FROM access_tokens" +
                                    (count > 0 ? $" LIMIT {count};" : ";");

            return connection.Query<string>(selectTokensQuery);
        }

        private void CreateTable(int tokenLength)
        {
            using IDbConnection connection = new NpgsqlConnection(connectionString);

            // ReSharper disable once UseStringInterpolation
            var createTableQuery = string.Format(@"CREATE TABLE IF NOT EXISTS access_tokens
                                                   (
                                                       access_token CHAR({0}) PRIMARY KEY
                                                   );", tokenLength);

            connection.Execute(createTableQuery);
        }

        private bool IsStoredAccessToken(string accessToken)
        {
            using IDbConnection connection = new NpgsqlConnection(connectionString);

            const string selectTokenQuery = @"SELECT EXISTS(
                                                               SELECT access_token FROM access_tokens
                                                                   WHERE access_token = @AccessToken
                                                           ) AS E;";

            return connection.QuerySingle<bool>(selectTokenQuery, new { AccessToken = accessToken });
        }
    }
}