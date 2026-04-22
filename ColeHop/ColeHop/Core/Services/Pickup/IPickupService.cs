using ColeHop.Core.Services.Nfc.Dtos;
using ColeHop.Core.Services.Pickup.Dtos;
using ColeHop.Model.Domain;

namespace ColeHop.Core.Services.Pickup
{
    public interface IPickupService
    {
        Task<IReadOnlyList<DailyPickupItem>> GetDailyPickupsAsync(string teacherId, DateOnly date);

        Task<PickupContext> StartPickupAsync(string teacherId, string childId, DateOnly date);

        Task<PickupAuthorizationResult> CheckAuthorizationAsync(PickupContext context, VerifiedIdentity verifiedIdentity);

        Task<PickupLog> ConfirmPickupAsync(string teacherId, PickupContext context, VerifiedIdentity verifiedIdentity);

        Task<IReadOnlyList<PickupLog>> GetPickupHistoryAsync(string requesterId, PickupHistoryQuery query);
    }
}