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

        public event EventHandler<UserRole?>? AuthenticationStateChanged;

        public async Task RegisterTutorAsync(TutorRegistrationData registrationData)
        {
            // Validación básica
            if (string.IsNullOrWhiteSpace(registrationData.Email))
                throw new InvalidOperationException("Email requerido.");

            if (string.IsNullOrWhiteSpace(registrationData.Password))
                throw new InvalidOperationException("Password requerido.");

            // Llamada a backend / API
            // Registro en backend: estado pendiente de aprobación
            await Task.CompletedTask;
        }

        public async Task LoginAsync(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                throw new InvalidOperationException("Credenciales inválidas.");

            var userId = Guid.NewGuid().ToString();
            var role = UserRole.Tutor;
            var token = Guid.NewGuid().ToString();

            await SecureStorage.SetAsync(UserIdKey, userId);
            await SecureStorage.SetAsync(RoleKey, role.ToString());
            await SecureStorage.SetAsync(TokenKey, token);

            _currentSession = new AuthSession(userId, role, token);
            AuthenticationStateChanged?.Invoke(this, role);
        }

        public async Task SimulateLoginAsync(UserRole role)
        {
            // Simular autenticación sin backend real
            var userId = Guid.NewGuid().ToString();
            var token = Guid.NewGuid().ToString();

            await SecureStorage.SetAsync(UserIdKey, userId);
            await SecureStorage.SetAsync(RoleKey, role.ToString());
            await SecureStorage.SetAsync(TokenKey, token);

            _currentSession = new AuthSession(userId, role, token);

            // Notificar cambio de estado. AppShell escucha este evento
            AuthenticationStateChanged?.Invoke(this, role);
        }

        public async Task<bool> TryRestoreSessionAsync()
        {
            try
            {
                // Intentar recuperar sesión del almacenamiento seguro
                var userId = await SecureStorage.GetAsync(UserIdKey);
                var token = await SecureStorage.GetAsync(TokenKey);
                var roleValue = await SecureStorage.GetAsync(RoleKey);

                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token) || string.IsNullOrEmpty(roleValue))
                    return false;

                if (!Enum.TryParse<UserRole>(roleValue, out var role))
                    return false;

                // Restaurar sesión
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
            // Limpiar almacenamiento seguro
            SecureStorage.Remove(UserIdKey);
            SecureStorage.Remove(RoleKey);
            SecureStorage.Remove(TokenKey);

            _currentSession = null;

            // Notificar logout. AppShell vuelve a LoginPage
            AuthenticationStateChanged?.Invoke(this, null);

            await Task.CompletedTask;
        }
    }
}