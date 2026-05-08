using ColeHop.Core.Services.TutorManagement;
using ColeHop.Core.Services.TutorManagement.Dtos;
using ColeHop.Model.Domain;
using ColeHop.Utils;
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
