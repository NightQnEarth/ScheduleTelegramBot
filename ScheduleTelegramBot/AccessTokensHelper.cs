using System;
using System.Linq;
using System.Text;
using Telegram.Bot.Types.ReplyMarkups;

namespace ScheduleTelegramBot
{
    public static class AccessTokensHelper
    {
        private const string TokenSymbolsPool = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!@#$%&";
        private const int TokenLength = 32;

        public static string GenerateAccessToken(int tokenLength = TokenLength)
        {
            var resultToken = new StringBuilder();
            var random = new Random();

            while (0 < tokenLength--)
                resultToken.Append(TokenSymbolsPool[random.Next(TokenSymbolsPool.Length)]);

            return resultToken.ToString();
        }

        public static InlineKeyboardMarkup GetInlineAccessTokensKeyboard(IAccessTokensStorage tokensStorage)
        {
            var accessTokens = tokensStorage.GetAccessTokens();

            return new InlineKeyboardMarkup(
                accessTokens
                    .Select(accessToken => new[] { InlineKeyboardButton.WithCallbackData(accessToken) }));
        }
    }
}