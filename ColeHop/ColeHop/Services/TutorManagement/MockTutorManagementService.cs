using ColeHop.Services.TutorManagement;
using ColeHop.Services.TutorManagement;
using ColeHop.Models;

namespace ColeHop.Services.TutorManagement
{
    public sealed class MockTutorManagementService : ITutorManagementService
    {
        private readonly List<Child> _children = new()
        {
            new Child
            {
                Id = "1",
                Name = "Juan",
                LastName = "Garc├¡a L├│pez",
                EducationType = "Primaria",
                Course = "3┬║",
                Group = "A",
                TutorId = "tutor1",
                ApprovalStatus = ApprovalStatus.Approved
            },
            new Child
            {
                Id = "2",
                Name = "Mar├¡a",
                LastName = "Garc├¡a L├│pez",
                EducationType = "Primaria",
                Course = "1┬║",
                Group = "B",
                TutorId = "tutor1",
                ApprovalStatus = ApprovalStatus.Approved
            },
            new Child
            {
                Id = "3",
                Name = "Pedro",
                LastName = "Mart├¡nez Ruiz",
                EducationType = "Infantil",
                Course = "5 a├▒os",
                Group = "A",
                TutorId = "tutor1",
                ApprovalStatus = ApprovalStatus.Approved
            }
        };

        private readonly List<AuthorizedPerson> _authorizedPersons = new()
        {
            new AuthorizedPerson
            {
                Id = "1",
                Name = "Ana",
                LastName = "Garc├¡a S├ínchez",
                Dni = "12345678A",
                Relationship = "Abuela",
                Photo = Array.Empty<byte>(),
                TutorId = "tutor1"
            },
            new AuthorizedPerson
            {
                Id = "2",
                Name = "Carlos",
                LastName = "L├│pez Fern├índez",
                Dni = "87654321B",
                Relationship = "T├¡o",
                Photo = Array.Empty<byte>(),
                TutorId = "tutor1"
            },
            new AuthorizedPerson
            {
                Id = "3",
                Name = "Isabel",
                LastName = "Ruiz Mart├¡nez",
                Dni = "11223344C",
                Relationship = "Madre",
                Photo = Array.Empty<byte>(),
                TutorId = "tutor1"
            }
        };

        private readonly List<Authorization> _authorizations = new()
        {
            new Authorization
            {
                Id = "1",
                FromDate = DateOnly.FromDateTime(DateTime.Today),
                ToDate = DateOnly.FromDateTime(DateTime.Today.AddDays(30)),
                ChildIds = new List<string> { "1", "2" },
                AuthorizedPersonId = "1",
                TutorId = "tutor1",
                IsActive = true
            }
        };

        public Task<Child> AddChildAsync(string tutorId, ChildData childData)
        {
            var child = new Child
            {
                Id = Guid.NewGuid().ToString(),
                Name = childData.Name,
                LastName = childData.LastName,
                EducationType = childData.EducationType,
                Course = childData.Course,
                Group = childData.Group,
                TutorId = tutorId,
                ApprovalStatus = ApprovalStatus.Pending
            };

            _children.Add(child);
            return Task.FromResult(child);
        }

        public Task<IReadOnlyList<Child>> GetChildrenAsync(string tutorId)
        {
            return Task.FromResult<IReadOnlyList<Child>>(_children.ToList());
        }

        public Task UpdateChildAsync(string tutorId, string childId, ChildData updatedData)
        {
            var child = _children.FirstOrDefault(c => c.Id == childId);
            if (child != null)
            {
                _children.Remove(child);
                _children.Add(new Child
                {
                    Id = child.Id,
                    Name = updatedData.Name,
                    LastName = updatedData.LastName,
                    EducationType = updatedData.EducationType,
                    Course = updatedData.Course,
                    Group = updatedData.Group,
                    TutorId = tutorId,
                    ApprovalStatus = ApprovalStatus.Pending,
                    RejectionReason = null
                });
            }
            return Task.CompletedTask;
        }

        public Task RemoveChildAsync(string tutorId, string childId)
        {
            var child = _children.FirstOrDefault(c => c.Id == childId);
            if (child != null)
            {
                _children.Remove(child);
            }
            return Task.CompletedTask;
        }

        public Task<AuthorizedPerson> AddAuthorizedPersonAsync(string tutorId, AuthorizedPersonData personData)
        {
            var person = new AuthorizedPerson
            {
                Id = Guid.NewGuid().ToString(),
                Name = personData.Name,
                LastName = personData.LastName,
                Dni = personData.Dni,
                Relationship = personData.Relationship,
                Photo = personData.Photo,
                TutorId = tutorId
            };

            _authorizedPersons.Add(person);
            return Task.FromResult(person);
        }

        public Task<IReadOnlyList<AuthorizedPerson>> GetAuthorizedPersonsAsync(string tutorId)
        {
            return Task.FromResult<IReadOnlyList<AuthorizedPerson>>(_authorizedPersons.ToList());
        }

        public Task UpdateAuthorizedPersonAsync(string tutorId, string authorizedPersonId, AuthorizedPersonData updatedData)
        {
            var person = _authorizedPersons.FirstOrDefault(p => p.Id == authorizedPersonId);
            if (person != null)
            {
                _authorizedPersons.Remove(person);
                _authorizedPersons.Add(new AuthorizedPerson
                {
                    Id = person.Id,
                    Name = updatedData.Name,
                    LastName = updatedData.LastName,
                    Dni = updatedData.Dni,
                    Relationship = updatedData.Relationship,
                    Photo = updatedData.Photo,
                    TutorId = tutorId
                });
            }
            return Task.CompletedTask;
        }

        public Task RemoveAuthorizedPersonAsync(string tutorId, string authorizedPersonId)
        {
            var person = _authorizedPersons.FirstOrDefault(p => p.Id == authorizedPersonId);
            if (person != null)
            {
                _authorizedPersons.Remove(person);
            }
            return Task.CompletedTask;
        }

        public Task<Authorization> CreateAuthorizationAsync(string tutorId, AuthorizationData authorizationData)
        {
            var authorization = new Authorization
            {
                Id = Guid.NewGuid().ToString(),
                FromDate = authorizationData.FromDate,
                ToDate = authorizationData.ToDate,
                ChildIds = authorizationData.ChildIds,
                AuthorizedPersonId = authorizationData.AuthorizedPersonId,
                TutorId = tutorId,
                IsActive = true
            };

            _authorizations.Add(authorization);
            return Task.FromResult(authorization);
        }

        public Task<IReadOnlyList<Authorization>> GetAuthorizationsAsync(string tutorId)
        {
            return Task.FromResult<IReadOnlyList<Authorization>>(_authorizations.ToList());
        }

        public Task UpdateAuthorizationAsync(string tutorId, string authorizationId, AuthorizationData updatedData)
        {
            var authorization = _authorizations.FirstOrDefault(a => a.Id == authorizationId);
            if (authorization != null)
            {
                _authorizations.Remove(authorization);
                _authorizations.Add(new Authorization
                {
                    Id = authorization.Id,
                    FromDate = updatedData.FromDate,
                    ToDate = updatedData.ToDate,
                    ChildIds = updatedData.ChildIds,
                    AuthorizedPersonId = updatedData.AuthorizedPersonId,
                    TutorId = tutorId,
                    IsActive = true
                });
            }
            return Task.CompletedTask;
        }

        public Task DisableAuthorizationAsync(string tutorId, string authorizationId)
        {
            var authorization = _authorizations.FirstOrDefault(a => a.Id == authorizationId);
            if (authorization != null)
            {
                _authorizations.Remove(authorization);
                _authorizations.Add(new Authorization
                {
                    Id = authorization.Id,
                    FromDate = authorization.FromDate,
                    ToDate = authorization.ToDate,
                    ChildIds = authorization.ChildIds,
                    AuthorizedPersonId = authorization.AuthorizedPersonId,
                    TutorId = authorization.TutorId,
                    IsActive = false
                });
            }
            return Task.CompletedTask;
        }
    }
}