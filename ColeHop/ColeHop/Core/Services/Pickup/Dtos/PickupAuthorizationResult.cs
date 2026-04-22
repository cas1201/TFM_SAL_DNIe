namespace ColeHop.Core.Services.Pickup.Dtos
{
    public sealed record PickupAuthorizationResult(bool IsAuthorized, string? DenialReason);
}
