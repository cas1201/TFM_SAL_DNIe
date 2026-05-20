using ColeHop.Models;

namespace ColeHop.Services.Auth
{
    public sealed class AuthSession
    {
        public string UserId { get; }
        public UserRole Role { get; }
        public string Token { get; }
        public string FullName { get; }
        public string Dni { get; }

        public AuthSession(string userId, UserRole role, string token, string fullName = "", string dni = "")
        {
            UserId = userId;
            Role = role;
            Token = token;
            FullName = fullName;
            Dni = dni;
        }
    }
}
