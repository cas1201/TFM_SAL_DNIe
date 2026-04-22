using ColeHop.Core.Services.Auth.Dtos;
using ColeHop.Model.Identity;

namespace ColeHop.Core.Services.Auth
{
    public interface IAuthService
    {
        bool IsAuthenticated { get; }
        UserRole? CurrentRole { get; }
        string? CurrentUserId { get; }

        Task RegisterTutorAsync(TutorRegistrationData registrationData);
        Task LoginAsync(string email, string password);
        Task<bool> TryRestoreSessionAsync();
        Task LogoutAsync();
    }
}
