using ColeHop.Core.Services.TutorManagement.Dtos;
using ColeHop.Model.Domain;

namespace ColeHop.Core.Services.TutorManagement
{
    public interface ITutorManagementService
    {
        Task<Child> AddChildAsync(string tutorId, ChildData childData);

        Task<IReadOnlyList<Child>> GetChildrenAsync(string tutorId);

        Task UpdateChildAsync(string tutorId, string childId, ChildData updatedData);

        Task RemoveChildAsync(string tutorId, string childId);

        Task<AuthorizedPerson> AddAuthorizedPersonAsync(string tutorId, AuthorizedPersonData personData);

        Task<IReadOnlyList<AuthorizedPerson>> GetAuthorizedPersonsAsync(string tutorId);

        Task UpdateAuthorizedPersonAsync(string tutorId, string authorizedPersonId, AuthorizedPersonData updatedData);

        Task RemoveAuthorizedPersonAsync(string tutorId, string authorizedPersonId);

        Task<Authorization> CreateAuthorizationAsync(string tutorId, AuthorizationData authorizationData);

        Task<IReadOnlyList<Authorization>> GetAuthorizationsAsync(string tutorId);

        Task UpdateAuthorizationAsync(string tutorId, string authorizationId, AuthorizationData updatedData);

        Task DisableAuthorizationAsync(string tutorId, string authorizationId);
    }
}