using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Telegram.Bot.Types.ReplyMarkups;

namespace ScheduleTelegramBot
{
    public class AccessTokensCache
    {
        private const string TokenSymbolsPool = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!@#$%&";
        private const int TokenLength = 32;

        public readonly string ApiAccessToken;
        private static readonly Regex accessTokenFormat = new Regex($@"[{TokenSymbolsPool}]{{{TokenLength}}}");
        private readonly HashSet<string> accessTokens;
        private readonly string cacheFileName;

        public AccessTokensCache(string apiAccessToken, string cacheFileName)
        {
            ApiAccessToken = apiAccessToken;
            this.cacheFileName = cacheFileName;
            accessTokens = LoadCache().ToHashSet();
        }

        public int Count => accessTokens.Count;

        public static string GenerateAccessToken(int tokenLength = TokenLength)
        {
            var resultToken = new StringBuilder();
            Random random = new Random();
            while (0 < tokenLength--) resultToken.Append(TokenSymbolsPool[random.Next(TokenSymbolsPool.Length)]);

            return resultToken.ToString();
        }

        public bool IsApiAccessToken(string anyString) => ApiAccessToken.Equals(anyString);

        public bool IsValidToken(string anyString) => IsApiAccessToken(anyString) || accessTokens.Contains(anyString);

        public void Add(string accessToken)
        {
            if (accessTokens.Add(accessToken)) SaveCache(accessToken);
        }

        public void Remove(string accessToken)
        {
            if (accessTokens.Remove(accessToken)) SaveCache(accessToken);
        }

        public InlineKeyboardMarkup GetInlineAccessTokensKeyboard() => new InlineKeyboardMarkup(
            accessTokens
                .Where(accessToken => !accessToken.Equals(ApiAccessToken))
                .Select(accessToken => new[] { InlineKeyboardButton.WithCallbackData(accessToken) }));

        private IEnumerable<string> LoadCache()
        {
            Console.WriteLine("Loading existing cache file...");

            try
            {
                return File.ReadAllLines(cacheFileName, Encoding.UTF8)
                           .Where(line => accessTokenFormat.Match(line, 0, line.Length).Success);
            }
            catch (Exception exception) when (exception is IOException)
            {
                Console.WriteLine(exception.Message);
                while (!(exception is null)) Console.WriteLine((exception = exception.InnerException).Message);
            }

            return new HashSet<string>();
        }

        private void SaveCache(string newAccessToken)
        {
            Console.WriteLine("Storing cache...");

            try
            {
                File.AppendAllText(cacheFileName, newAccessToken + Environment.NewLine, Encoding.UTF8);
            }
            catch (Exception exception) when (exception is IOException)
            {
                Console.WriteLine(exception.Message);
                while (!(exception is null)) Console.WriteLine((exception = exception.InnerException).Message);
            }
        }
    }
}