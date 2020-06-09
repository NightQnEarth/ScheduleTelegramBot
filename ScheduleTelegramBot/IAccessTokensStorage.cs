using System.Collections.Generic;

namespace ScheduleTelegramBot
{
    public interface IAccessTokensStorage
    {
        string ApiAccessToken { get; }
        bool IsEmpty { get; }
        bool IsValidToken(string accessToken);
        void StoreAccessToken(string accessToken);
        void DeleteAccessToken(string accessToken);
        IEnumerable<string> GetAccessTokens(int count = -1);
    }
}