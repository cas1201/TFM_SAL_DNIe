using ColeHop.Services.Nfc;
using ColeHop.Services.Pickup;
using ColeHop.Services.Pickup;
using ColeHop.Models;

namespace ColeHop.Services.Pickup
{
    public sealed class PickupService : IPickupService
    {
        private readonly List<PickupLog> _pickupLogs = [];

        public Task<IReadOnlyList<DailyPickupItem>> GetDailyPickupsAsync(string teacherId, DateOnly date)
        {
            // Datos simulados para desarrollo/testing
            IReadOnlyList<DailyPickupItem> result = new List<DailyPickupItem>
            {
                new DailyPickupItem("child-1", "Juan Pérez", "María Pérez", false),
                new DailyPickupItem("child-2", "Lucía García", "Carlos García", true)
            };

            return Task.FromResult(result);
        }

        public Task<PickupContext> StartPickupAsync(string teacherId, string childId, DateOnly date)
        {
            // Crear contexto de recogida
            var context = new PickupContext(childId, "authorization-123", "authorized-person-456", date);
            return Task.FromResult(context);
        }

        public Task<PickupAuthorizationResult> CheckAuthorizationAsync(PickupContext context, VerifiedIdentity verifiedIdentity)
        {
            // Verificar que la persona está autorizada para recoger al niño
            // En producción: comparar DNI con autorizaciones en BD
            var result = new PickupAuthorizationResult(true, null);
            return Task.FromResult(result);
        }

        public Task<PickupLog> ConfirmPickupAsync(string teacherId, PickupContext context, VerifiedIdentity verifiedIdentity)
        {
            // Registrar la recogida en el log
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