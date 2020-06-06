using System.Collections.Generic;

namespace ScheduleTelegramBot
{
    public interface IAccessTokensStorage
    {
        public string ApiAccessToken { get; }
        public bool IsEmpty { get; }
        public bool IsValidToken(string accessToken);
        public void StoreAccessToken(string accessToken);
        public void DeleteAccessToken(string accessToken);
        public IEnumerable<string> GetAccessTokens(int count = -1);
    }
}