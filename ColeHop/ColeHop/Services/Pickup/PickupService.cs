using ColeHop.Services.Nfc;
using ColeHop.Services.TutorManagement;
using ColeHop.Models;

namespace ColeHop.Services.Pickup
{
    public sealed class MockPickupService : IPickupService
    {
        private readonly ITutorManagementService _tutorManagement;
        private readonly List<PickupLog> _pickupLogs = [];
        private readonly HashSet<string> _pickedUpChildIds = [];

        public MockPickupService(ITutorManagementService tutorManagement)
        {
            _tutorManagement = tutorManagement;
        }

        public async Task<IReadOnlyList<DailyPickupItem>> GetDailyPickupsAsync(string teacherId, DateOnly date)
        {
            // Obtener todos los niños y autorizaciones activas para hoy
            const string tutorId = "tutor1";
            var children = await _tutorManagement.GetChildrenAsync(tutorId);
            var authorizedPersons = await _tutorManagement.GetAuthorizedPersonsAsync(tutorId);
            var authorizations = await _tutorManagement.GetAuthorizationsAsync(tutorId);

            var items = new List<DailyPickupItem>();

            foreach (var auth in authorizations.Where(a => a.IsActive && a.FromDate <= date && a.ToDate >= date))
            {
                var person = authorizedPersons.FirstOrDefault(p => p.Id == auth.AuthorizedPersonId);
                if (person == null) continue;

                foreach (var childId in auth.ChildIds)
                {
                    var child = children.FirstOrDefault(c => c.Id == childId);
                    if (child == null) continue;

                    // Evitar duplicados si ya hay una entrada para ese niño
                    if (items.Any(i => i.ChildId == childId)) continue;

                    items.Add(new DailyPickupItem(
                        childId,
                        $"{child.Name} {child.LastName}",
                        person.FullName,
                        _pickedUpChildIds.Contains(childId)));
                }
            }

            return items;
        }

        public async Task<PickupContext> StartPickupAsync(string teacherId, string childId, DateOnly date)
        {
            const string tutorId = "tutor1";
            var authorizations = await _tutorManagement.GetAuthorizationsAsync(tutorId);

            var auth = authorizations.FirstOrDefault(a =>
                a.IsActive && a.FromDate <= date && a.ToDate >= date && a.ChildIds.Contains(childId));

            var context = new PickupContext(
                childId,
                auth?.Id ?? "none",
                auth?.AuthorizedPersonId ?? "none",
                date);

            return context;
        }

        public async Task<PickupAuthorizationResult> CheckAuthorizationAsync(PickupContext context, VerifiedIdentity verifiedIdentity)
        {
            const string tutorId = "tutor1";
            var authorizedPersons = await _tutorManagement.GetAuthorizedPersonsAsync(tutorId);
            var authorizations = await _tutorManagement.GetAuthorizationsAsync(tutorId);

            // Buscar persona autorizada por DNI
            var person = authorizedPersons.FirstOrDefault(p =>
                p.Dni.Equals(verifiedIdentity.Dni, StringComparison.OrdinalIgnoreCase));

            if (person == null)
            {
                return new PickupAuthorizationResult(false, $"El DNI {verifiedIdentity.Dni} no corresponde a ninguna persona autorizada.");
            }

            // Verificar que tiene autorización activa para este niño hoy
            var today = context.Date;
            var hasAuth = authorizations.Any(a =>
                a.IsActive &&
                a.AuthorizedPersonId == person.Id &&
                a.ChildIds.Contains(context.ChildId) &&
                a.FromDate <= today &&
                a.ToDate >= today);

            if (!hasAuth)
            {
                return new PickupAuthorizationResult(false, $"{person.FullName} no tiene autorización vigente para recoger a este menor.");
            }

            return new PickupAuthorizationResult(true, null);
        }

        public Task<PickupLog> ConfirmPickupAsync(string teacherId, PickupContext context, VerifiedIdentity verifiedIdentity)
        {
            _pickedUpChildIds.Add(context.ChildId);

            var pickupLog = new PickupLog(Guid.NewGuid().ToString(), context.ChildId, verifiedIdentity.Dni, teacherId, DateTime.UtcNow);
            _pickupLogs.Add(pickupLog);

            return Task.FromResult(pickupLog);
        }

        public Task<IReadOnlyList<PickupLog>> GetPickupHistoryAsync(string requesterId, PickupHistoryQuery query)
        {
            IReadOnlyList<PickupLog> result = _pickupLogs.AsReadOnly();
            return Task.FromResult(result);
        }
    }
}