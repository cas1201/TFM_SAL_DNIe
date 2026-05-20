# Creación del Backend - ColeHop

## Prompt de recreación

> Necesito crear un backend API REST (preferiblemente ASP.NET Core Web API) para la aplicación móvil ColeHop, un sistema de gestión de recogidas escolares con verificación de identidad por DNIe/NFC. La app tiene dos roles: **Tutor** (padre/madre que gestiona hijos, personas autorizadas y autorizaciones) y **Teacher** (profesor que gestiona recogidas diarias y aprobaciones de altas). A continuación se detalla toda la lógica de conexión que la app espera del backend.

---

## Configuración de conexión

```csharp
// ColeHop/Helpers/ApiConfig.cs
namespace ColeHop.Helpers
{
	public sealed class ApiConfig
	{
		public const string BaseUrl = "https://dir_ip:puerto/";
	}
}
```

```csharp
// ColeHop/Helpers/JwtStorage.cs
namespace ColeHop.Helpers
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
```

---

## Servicio de Autenticación (HttpAuthService)

**Endpoints esperados:**
- `POST api/auth/register` — Registro de tutor
- `POST api/auth/login` — Login (devuelve JWT + userId + role)
- `GET api/auth/validate` — Validar token JWT activo

**Respuesta de login:**
```json
{
  "userId": "string",
  "role": "Tutor | Teacher",
  "token": "jwt_string"
}
```

**Implementación completa:**

```csharp
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
```

---

## Servicio de Gestión de Tutor (HttpTutorManagementService)

**Endpoints esperados:**
- `POST api/tutors/{tutorId}/children` — Añadir hijo (queda pendiente de aprobación)
- `GET api/tutors/{tutorId}/children` — Listar hijos
- `PUT api/tutors/{tutorId}/children/{childId}` — Actualizar hijo
- `DELETE api/tutors/{tutorId}/children/{childId}` — Eliminar hijo
- `POST api/tutors/{tutorId}/authorized-persons` — Añadir persona autorizada
- `GET api/tutors/{tutorId}/authorized-persons` — Listar personas autorizadas
- `PUT api/tutors/{tutorId}/authorized-persons/{id}` — Actualizar persona autorizada
- `DELETE api/tutors/{tutorId}/authorized-persons/{id}` — Eliminar persona autorizada
- `POST api/tutors/{tutorId}/authorizations` — Crear autorización
- `GET api/tutors/{tutorId}/authorizations` — Listar autorizaciones
- `PUT api/tutors/{tutorId}/authorizations/{id}` — Actualizar autorización
- `DELETE api/tutors/{tutorId}/authorizations/{id}` — Desactivar autorización

**Implementación completa:**

```csharp
using ColeHop.Models;
using ColeHop.Helpers;
using System.Net.Http.Json;

namespace ColeHop.Services.TutorManagement
{
	public sealed class HttpTutorManagementService : ITutorManagementService
	{
		private readonly HttpClient _httpClient;
		private readonly JwtStorage _jwtStorage;

		public HttpTutorManagementService(HttpClient httpClient, JwtStorage jwtStorage)
		{
			_httpClient = httpClient;
			_jwtStorage = jwtStorage;
		}

		private async Task<HttpClient> GetAuthenticatedClientAsync()
		{
			var token = await _jwtStorage.GetTokenAsync();
			if (!string.IsNullOrEmpty(token))
			{
				_httpClient.DefaultRequestHeaders.Authorization =
					new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
			}
			return _httpClient;
		}

		public async Task<Child> AddChildAsync(string tutorId, ChildData childData)
		{
			var client = await GetAuthenticatedClientAsync();
			var response = await client.PostAsJsonAsync($"api/tutors/{tutorId}/children", childData);
			response.EnsureSuccessStatusCode();
			var child = await response.Content.ReadFromJsonAsync<Child>();
			return child ?? throw new InvalidOperationException("Error al crear hijo");
		}

		public async Task<IReadOnlyList<Child>> GetChildrenAsync(string tutorId)
		{
			var client = await GetAuthenticatedClientAsync();
			var response = await client.GetAsync($"api/tutors/{tutorId}/children");
			response.EnsureSuccessStatusCode();
			var children = await response.Content.ReadFromJsonAsync<List<Child>>();
			return children ?? new List<Child>();
		}

		public async Task UpdateChildAsync(string tutorId, string childId, ChildData updatedData)
		{
			var client = await GetAuthenticatedClientAsync();
			var response = await client.PutAsJsonAsync($"api/tutors/{tutorId}/children/{childId}", updatedData);
			response.EnsureSuccessStatusCode();
		}

		public async Task RemoveChildAsync(string tutorId, string childId)
		{
			var client = await GetAuthenticatedClientAsync();
			var response = await client.DeleteAsync($"api/tutors/{tutorId}/children/{childId}");
			response.EnsureSuccessStatusCode();
		}

		public async Task<AuthorizedPerson> AddAuthorizedPersonAsync(string tutorId, AuthorizedPersonData personData)
		{
			var client = await GetAuthenticatedClientAsync();
			var response = await client.PostAsJsonAsync($"api/tutors/{tutorId}/authorized-persons", personData);
			response.EnsureSuccessStatusCode();
			var person = await response.Content.ReadFromJsonAsync<AuthorizedPerson>();
			return person ?? throw new InvalidOperationException("Error al crear persona autorizada");
		}

		public async Task<IReadOnlyList<AuthorizedPerson>> GetAuthorizedPersonsAsync(string tutorId)
		{
			var client = await GetAuthenticatedClientAsync();
			var response = await client.GetAsync($"api/tutors/{tutorId}/authorized-persons");
			response.EnsureSuccessStatusCode();
			var persons = await response.Content.ReadFromJsonAsync<List<AuthorizedPerson>>();
			return persons ?? new List<AuthorizedPerson>();
		}

		public async Task UpdateAuthorizedPersonAsync(string tutorId, string authorizedPersonId, AuthorizedPersonData updatedData)
		{
			var client = await GetAuthenticatedClientAsync();
			var response = await client.PutAsJsonAsync($"api/tutors/{tutorId}/authorized-persons/{authorizedPersonId}", updatedData);
			response.EnsureSuccessStatusCode();
		}

		public async Task RemoveAuthorizedPersonAsync(string tutorId, string authorizedPersonId)
		{
			var client = await GetAuthenticatedClientAsync();
			var response = await client.DeleteAsync($"api/tutors/{tutorId}/authorized-persons/{authorizedPersonId}");
			response.EnsureSuccessStatusCode();
		}

		public async Task<Authorization> CreateAuthorizationAsync(string tutorId, AuthorizationData authorizationData)
		{
			var client = await GetAuthenticatedClientAsync();
			var response = await client.PostAsJsonAsync($"api/tutors/{tutorId}/authorizations", authorizationData);
			response.EnsureSuccessStatusCode();
			var authorization = await response.Content.ReadFromJsonAsync<Authorization>();
			return authorization ?? throw new InvalidOperationException("Error al crear autorización");
		}

		public async Task<IReadOnlyList<Authorization>> GetAuthorizationsAsync(string tutorId)
		{
			var client = await GetAuthenticatedClientAsync();
			var response = await client.GetAsync($"api/tutors/{tutorId}/authorizations");
			response.EnsureSuccessStatusCode();
			var authorizations = await response.Content.ReadFromJsonAsync<List<Authorization>>();
			return authorizations ?? new List<Authorization>();
		}

		public async Task UpdateAuthorizationAsync(string tutorId, string authorizationId, AuthorizationData updatedData)
		{
			var client = await GetAuthenticatedClientAsync();
			var response = await client.PutAsJsonAsync($"api/tutors/{tutorId}/authorizations/{authorizationId}", updatedData);
			response.EnsureSuccessStatusCode();
		}

		public async Task DisableAuthorizationAsync(string tutorId, string authorizationId)
		{
			var client = await GetAuthenticatedClientAsync();
			var response = await client.DeleteAsync($"api/tutors/{tutorId}/authorizations/{authorizationId}");
			response.EnsureSuccessStatusCode();
		}
	}
}
```

---

## Servicio de Recogidas (HttpPickupService)

**Endpoints esperados:**
- `GET api/pickups/daily?teacherId={id}&date={yyyy-MM-dd}` — Listado de recogidas del día
- `POST api/pickups/start` — Iniciar proceso de recogida
- `POST api/pickups/check-authorization` — Verificar autorización tras lectura NFC
- `POST api/pickups/confirm` — Confirmar recogida
- `GET api/pickups/history?requesterId={id}&fromDate=&toDate=&childId=` — Historial

**Implementación completa:**

```csharp
using ColeHop.Helpers;
using ColeHop.Models;
using ColeHop.Services.Nfc;
using System.Net.Http.Json;

namespace ColeHop.Services.Pickup
{
	public sealed class HttpPickupService : IPickupService
	{
		private readonly HttpClient _httpClient;
		private readonly JwtStorage _jwtStorage;

		public HttpPickupService(HttpClient httpClient, JwtStorage jwtStorage)
		{
			_httpClient = httpClient;
			_jwtStorage = jwtStorage;
		}

		private async Task EnsureAuthenticatedAsync()
		{
			var token = await _jwtStorage.GetTokenAsync();
			if (!string.IsNullOrEmpty(token))
			{
				_httpClient.DefaultRequestHeaders.Authorization =
					new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
			}
		}

		public async Task<IReadOnlyList<DailyPickupItem>> GetDailyPickupsAsync(string teacherId, DateOnly date)
		{
			await EnsureAuthenticatedAsync();
			var response = await _httpClient.GetAsync($"api/pickups/daily?teacherId={teacherId}&date={date:yyyy-MM-dd}");
			response.EnsureSuccessStatusCode();
			var items = await response.Content.ReadFromJsonAsync<List<DailyPickupItem>>();
			return items ?? [];
		}

		public async Task<PickupContext> StartPickupAsync(string teacherId, string childId, DateOnly date)
		{
			await EnsureAuthenticatedAsync();
			var response = await _httpClient.PostAsJsonAsync("api/pickups/start", new
			{
				TeacherId = teacherId,
				ChildId = childId,
				Date = date.ToString("yyyy-MM-dd")
			});
			response.EnsureSuccessStatusCode();
			var context = await response.Content.ReadFromJsonAsync<PickupContext>();
			return context ?? throw new InvalidOperationException("Error al iniciar recogida");
		}

		public async Task<PickupAuthorizationResult> CheckAuthorizationAsync(PickupContext context, VerifiedIdentity verifiedIdentity)
		{
			await EnsureAuthenticatedAsync();
			var response = await _httpClient.PostAsJsonAsync("api/pickups/check-authorization", new
			{
				context.ChildId,
				context.AuthorizationId,
				context.AuthorizedPersonId,
				DocumentNumber = verifiedIdentity.DocumentNumber,
				FullName = verifiedIdentity.FullName,
				Date = context.Date.ToString("yyyy-MM-dd")
			});
			response.EnsureSuccessStatusCode();
			var result = await response.Content.ReadFromJsonAsync<PickupAuthorizationResult>();
			return result ?? new PickupAuthorizationResult(false, "Error al verificar autorización");
		}

		public async Task<PickupLog> ConfirmPickupAsync(string teacherId, PickupContext context, VerifiedIdentity verifiedIdentity)
		{
			await EnsureAuthenticatedAsync();
			var response = await _httpClient.PostAsJsonAsync("api/pickups/confirm", new
			{
				TeacherId = teacherId,
				context.ChildId,
				AuthorizedDni = verifiedIdentity.Dni,
				AuthorizedName = verifiedIdentity.FullName,
				Date = context.Date.ToString("yyyy-MM-dd")
			});
			response.EnsureSuccessStatusCode();
			var log = await response.Content.ReadFromJsonAsync<PickupLog>();
			return log ?? throw new InvalidOperationException("Error al confirmar recogida");
		}

		public async Task<IReadOnlyList<PickupLog>> GetPickupHistoryAsync(string requesterId, PickupHistoryQuery query)
		{
			await EnsureAuthenticatedAsync();
			var url = $"api/pickups/history?requesterId={requesterId}";
			if (query.FromDate.HasValue) url += $"&fromDate={query.FromDate.Value:yyyy-MM-dd}";
			if (query.ToDate.HasValue) url += $"&toDate={query.ToDate.Value:yyyy-MM-dd}";
			if (!string.IsNullOrEmpty(query.ChildId)) url += $"&childId={query.ChildId}";

			var response = await _httpClient.GetAsync(url);
			response.EnsureSuccessStatusCode();
			var logs = await response.Content.ReadFromJsonAsync<List<PickupLog>>();
			return logs ?? [];
		}
	}
}
```

---

## Registro de servicios en DI (para cuando se implemente el backend)

```csharp
// En MauiProgram.cs, reemplazar los Mock por los Http:
builder.Services.AddSingleton<HttpClient>(sp =>
{
	var httpClient = new HttpClient
	{
		BaseAddress = new Uri(ApiConfig.BaseUrl)
	};
	httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
	return httpClient;
});
builder.Services.AddSingleton<JwtStorage>();
builder.Services.AddSingleton<IAuthService, HttpAuthService>();
builder.Services.AddSingleton<IPickupService, HttpPickupService>();
builder.Services.AddSingleton<ITutorManagementService, HttpTutorManagementService>();
// Nota: ITeacherService necesitará una implementación Http (HttpTeacherService) con endpoints:
// GET api/teachers/{teacherId}/pending-approvals
// POST api/teachers/{teacherId}/approve/{childId}
// POST api/teachers/{teacherId}/reject/{childId} (body: { reason: string })
```

---

## Modelos compartidos (ya existen en la app)

- `UserRole` (enum): Tutor, Teacher
- `Child`: Id, Name, LastName, EducationType, Course, Group, TutorId, ApprovalStatus, RejectionReason, TutorName, TutorLastName, TutorDni
- `AuthorizedPerson`: Id, Name, LastName, Dni, Relationship, TutorId
- `Authorization`: Id, FromDate, ToDate, ChildIds, AuthorizedPersonId, TutorId, IsActive
- `PickupLog`: Id, ChildId, AuthorizedDni, TeacherId, Timestamp
- `DailyPickupItem`: ChildId, ChildFullName, AuthorizedPersonName, IsPickedUp
- `PickupContext`: ChildId, AuthorizationId, AuthorizedPersonId, Date
- `PickupAuthorizationResult`: IsAuthorized, Message
- `TutorRegistrationData`: Name, LastName, Dni, Email, Password, School
- `AuthSession`: UserId, Role, Token

---

## Notas de diseño del backend

1. **Autenticación**: JWT Bearer tokens. El registro de tutor deja la cuenta pendiente de aprobación por un administrador/profesor.
2. **Flujo de alta de niño**: El tutor solicita el alta → queda en estado `Pending` → el profesor aprueba o rechaza (con motivo).
3. **Flujo de recogida**: El profesor inicia la recogida de un niño → se escanea el DNIe por NFC → se verifica que el DNI corresponde a una persona autorizada con autorización activa para ese niño en la fecha actual → se confirma la recogida.
4. **Seguridad**: Todos los endpoints (excepto login/register) requieren JWT válido. Las operaciones de tutor solo acceden a sus propios datos. Las operaciones de profesor acceden a los niños de su clase/centro.
