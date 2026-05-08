namespace ColeHop.Utils
{
    public sealed class JwtStorage
    {
        private const string TokenKey = "jwt_token";

        public async Task<string?> GetTokenAsync()
        {
            return await SecureStorage.GetAsync(TokenKey);
        }

        public async Task SetTokenAsync(string token)
        {
            await SecureStorage.SetAsync(TokenKey, token);
        }

        public void RemoveToken()
        {
            SecureStorage.Remove(TokenKey);
        }
    }
}
