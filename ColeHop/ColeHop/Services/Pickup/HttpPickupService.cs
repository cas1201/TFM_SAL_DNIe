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
