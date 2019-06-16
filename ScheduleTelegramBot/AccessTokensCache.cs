using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Telegram.Bot.Types.ReplyMarkups;

namespace ScheduleTelegramBot
{
    public class AccessTokensCache
    {
        public readonly string ApiAccessToken;
        private readonly HashSet<string> accessTokens;
        private readonly string cacheFileName;
        private readonly JsonSerializerSettings jsonSerializerSettings;

        public AccessTokensCache(string apiAccessToken, string cacheFileName)
        {
            ApiAccessToken = apiAccessToken;
            this.cacheFileName = cacheFileName;
            jsonSerializerSettings = new JsonSerializerSettings { Formatting = Formatting.Indented };
            accessTokens = LoadCache();

            throw new NotImplementedException();
        }

        public int Count => accessTokens.Count;

        public bool Contains(string accessToken) => accessTokens.Contains(accessToken);

        public void Add(string accessToken)
        {
            if (accessTokens.Add(accessToken)) SaveCache();
        }

        public void Remove(string accessToken)
        {
            if (accessTokens.Remove(accessToken)) SaveCache();
        }

        public InlineKeyboardMarkup GetInlineAccessTokensKeyboard() => new InlineKeyboardMarkup(
            accessTokens
                .Where(accessToken => !accessToken.Equals(ApiAccessToken))
                .Select(accessToken => new[] { InlineKeyboardButton.WithCallbackData(accessToken) }));

        private HashSet<string> LoadCache()
        {
            try
            {
                Console.WriteLine("Loading existing cache file...");
                return JsonConvert.DeserializeObject<HashSet<string>>(File.ReadAllText(cacheFileName),
                                                                      jsonSerializerSettings);
            }
            catch (Exception exception) when (exception is JsonException || exception is IOException)
            {
                Console.WriteLine("Can't find or load existing cache-file. It will create new cache file.");
                return new HashSet<string>();
            }
        }

        private void SaveCache()
        {
            Console.WriteLine("Storing cache...");

            try
            {
                using (var fileStream = new FileStream(cacheFileName, FileMode.OpenOrCreate, FileAccess.Write))
                    using (var streamWriter = new StreamWriter(fileStream, Encoding.UTF8, 1024, true))
                        streamWriter.WriteLine(JsonConvert.SerializeObject(accessTokens, jsonSerializerSettings));
            }
            catch (Exception exception) when (exception is JsonException || exception is IOException)
            {
                Console.WriteLine(exception);
            }
        }
    }
}