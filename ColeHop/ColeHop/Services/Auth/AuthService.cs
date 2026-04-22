using ColeHop.Core.Services.Auth;
using ColeHop.Core.Services.Auth.Dtos;
using ColeHop.Model.Identity;

namespace ColeHop.Services.Auth
{
    public sealed class AuthService : IAuthService
    {
        private const string UserIdKey = "auth_user_id";
        private const string RoleKey = "auth_user_role";
        private const string TokenKey = "auth_token";

        private AuthSession? _currentSession;

        public bool IsAuthenticated => _currentSession != null;
        public UserRole? CurrentRole => _currentSession?.Role;
        public string? CurrentUserId => _currentSession?.UserId;

        public async Task RegisterTutorAsync(TutorRegistrationData registrationData)
        {
            // Llamada a backend / API.

            if (string.IsNullOrWhiteSpace(registrationData.Email))
                throw new InvalidOperationException("Email requerido.");

            if (string.IsNullOrWhiteSpace(registrationData.Password))
                throw new InvalidOperationException("Password requerido.");

            // Registro en backend estado pendiente de aprobación
            await Task.CompletedTask;
        }

        public async Task LoginAsync(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                throw new InvalidOperationException("Credenciales inválidas.");

            // Validación contra backend / API.
            // Simulamos resultado válido
            var userId = Guid.NewGuid().ToString();
            var role = UserRole.Tutor; // o Teacher, según backend
            var token = Guid.NewGuid().ToString();

            await SecureStorage.SetAsync(UserIdKey, userId);
            await SecureStorage.SetAsync(RoleKey, role.ToString());
            await SecureStorage.SetAsync(TokenKey, token);

            _currentSession = new AuthSession(userId, role, token);
        }

        public async Task<bool> TryRestoreSessionAsync()
        {
            try
            {
                var userId = await SecureStorage.GetAsync(UserIdKey);
                var token = await SecureStorage.GetAsync(TokenKey);
                var roleValue = await SecureStorage.GetAsync(RoleKey);

                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token) || string.IsNullOrEmpty(roleValue))
                    return false;

                if (!Enum.TryParse<UserRole>(roleValue, out var role))
                    return false;

                _currentSession = new AuthSession(userId, role, token);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task LogoutAsync()
        {
            SecureStorage.Remove(UserIdKey);
            SecureStorage.Remove(RoleKey);
            SecureStorage.Remove(TokenKey);

            _currentSession = null;
            await Task.CompletedTask;
        }
    }
}