using ColeHop.Services.Nfc;
using ColeHop.Services.Pickup;
using ColeHop.Models;

namespace ColeHop.Services.Pickup
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