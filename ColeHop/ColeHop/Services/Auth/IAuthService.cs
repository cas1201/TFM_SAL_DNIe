using ColeHop.Models;

namespace ColeHop.Services.Auth
{
    public interface IAuthService
    {
        bool IsAuthenticated { get; }
        UserRole? CurrentRole { get; }
        string? CurrentUserId { get; }
        string? CurrentUserName { get; }
        string? CurrentUserDni { get; }

        event EventHandler<UserRole?>? AuthenticationStateChanged;

        Task RegisterTutorAsync(TutorRegistrationData registrationData);
        Task LoginAsync(string email, string password);
        Task SimulateLoginAsync(UserRole role);
        Task<bool> TryRestoreSessionAsync();
        Task LogoutAsync();
    }
}
