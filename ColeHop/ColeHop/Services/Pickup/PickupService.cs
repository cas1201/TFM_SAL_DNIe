using ColeHop.Core.Services.Nfc.Dtos;
using ColeHop.Core.Services.Pickup;
using ColeHop.Core.Services.Pickup.Dtos;
using ColeHop.Model.Domain;

namespace ColeHop.Services.Pickup
{
    public sealed class PickupService : IPickupService
    {
        private readonly List<PickupLog> _pickupLogs = [];

        public Task<IReadOnlyList<DailyPickupItem>> GetDailyPickupsAsync(string teacherId, DateOnly date)
        {
            // En una implementación real, aquí consultarías repositorios
            // De momento devolvemos una lista simulada
            IReadOnlyList<DailyPickupItem> result = new List<DailyPickupItem>
            {
                new DailyPickupItem("child-1", "Juan Pérez", "María Pérez", false),
                new DailyPickupItem("child-2", "Lucía García", "Carlos García", true)
            };

            return Task.FromResult(result);
        }

        public Task<PickupContext> StartPickupAsync(string teacherId, string childId, DateOnly date)
        {
            // Aquí se prepararía el contexto real consultando autorizaciones activas
            // De momento simulamos un contexto válido
            var context = new PickupContext(childId, "authorization-123", "authorized-person-456", date);
            return Task.FromResult(context);
        }

        public Task<PickupAuthorizationResult> CheckAuthorizationAsync(PickupContext context, VerifiedIdentity verifiedIdentity)
        {
            // Aquí iría la lógica real:
            // - DNI coincide con persona autorizada
            // - Fecha válida
            // - Menor correcto
            // - No recogido previamente

            // Simulación positiva
            var result = new PickupAuthorizationResult(true, null);
            return Task.FromResult(result);
        }

        public Task<PickupLog> ConfirmPickupAsync(string teacherId, PickupContext context, VerifiedIdentity verifiedIdentity)
        {
            // Registro definitivo de la recogida
            var pickupLog = new PickupLog(Guid.NewGuid().ToString(), context.ChildId, verifiedIdentity.Dni, teacherId, DateTime.UtcNow);
            _pickupLogs.Add(pickupLog);

            return Task.FromResult(pickupLog);
        }

        public Task<IReadOnlyList<PickupLog>> GetPickupHistoryAsync(string requesterId, PickupHistoryQuery query)
        {
            // Aquí aplicarías filtros reales por fecha, menor, rol, etc.
            IReadOnlyList<PickupLog> result = _pickupLogs.AsReadOnly();
            return Task.FromResult(result);
        }
    }
}