using ColeHop.Model.Identity;

namespace ColeHop.Core.Services.Auth
{
    public sealed class AuthSession
    {
        public string UserId { get; }
        public UserRole Role { get; }
        public string Token { get; }

        public AuthSession(string userId, UserRole role, string token)
        {
            UserId = userId;
            Role = role;
            Token = token;
        }
    }
}
