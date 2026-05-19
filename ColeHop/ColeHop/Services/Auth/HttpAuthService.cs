using ColeHop.Helpers;
using ColeHop.Models;
using System.Net.Http.Json;

namespace ColeHop.Services.Auth
{
    public sealed class HttpAuthService : IAuthService
    {
        private const string UserIdKey = "auth_user_id";
        private const string RoleKey = "auth_user_role";

        private readonly HttpClient _httpClient;
        private readonly JwtStorage _jwtStorage;
        private AuthSession? _currentSession;

        public bool IsAuthenticated => _currentSession != null;
        public UserRole? CurrentRole => _currentSession?.Role;
        public string? CurrentUserId => _currentSession?.UserId;

        public event EventHandler<UserRole?>? AuthenticationStateChanged;

        public HttpAuthService(HttpClient httpClient, JwtStorage jwtStorage)
        {
            _httpClient = httpClient;
            _jwtStorage = jwtStorage;
        }

        public async Task RegisterTutorAsync(TutorRegistrationData registrationData)
        {
            if (string.IsNullOrWhiteSpace(registrationData.Email))
                throw new InvalidOperationException("Email requerido.");

            if (string.IsNullOrWhiteSpace(registrationData.Password))
                throw new InvalidOperationException("Password requerido.");

            var response = await _httpClient.PostAsJsonAsync("api/auth/register", new
            {
                registrationData.Name,
                registrationData.LastName,
                registrationData.Dni,
                registrationData.Email,
                registrationData.Password,
                registrationData.School
            });

            response.EnsureSuccessStatusCode();
        }

        public async Task LoginAsync(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                throw new InvalidOperationException("Credenciales inválidas.");

            var response = await _httpClient.PostAsJsonAsync("api/auth/login", new
            {
                Email = email,
                Password = password
            });

            response.EnsureSuccessStatusCode();

            var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
            if (loginResponse == null)
                throw new InvalidOperationException("Respuesta de login inválida.");

            await _jwtStorage.SetTokenAsync(loginResponse.Token);
            await SecureStorage.SetAsync(UserIdKey, loginResponse.UserId);
            await SecureStorage.SetAsync(RoleKey, loginResponse.Role.ToString());

            _currentSession = new AuthSession(loginResponse.UserId, loginResponse.Role, loginResponse.Token);
            AuthenticationStateChanged?.Invoke(this, loginResponse.Role);
        }

        public async Task SimulateLoginAsync(UserRole role)
        {
            await LoginAsync("demo@colehop.es", "demo123");
        }

        public async Task<bool> TryRestoreSessionAsync()
        {
            try
            {
                var token = await _jwtStorage.GetTokenAsync();
                var userId = await SecureStorage.GetAsync(UserIdKey);
                var roleValue = await SecureStorage.GetAsync(RoleKey);

                if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(roleValue))
                    return false;

                if (!Enum.TryParse<UserRole>(roleValue, out var role))
                    return false;

                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.GetAsync("api/auth/validate");
                if (!response.IsSuccessStatusCode)
                {
                    await ClearStorageAsync();
                    return false;
                }

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
            await ClearStorageAsync();
            _currentSession = null;
            _httpClient.DefaultRequestHeaders.Authorization = null;
            AuthenticationStateChanged?.Invoke(this, null);
        }

        private async Task ClearStorageAsync()
        {
            _jwtStorage.RemoveToken();
            SecureStorage.Remove(UserIdKey);
            SecureStorage.Remove(RoleKey);
            await Task.CompletedTask;
        }

        private sealed record LoginResponse(string UserId, UserRole Role, string Token);
    }
}
