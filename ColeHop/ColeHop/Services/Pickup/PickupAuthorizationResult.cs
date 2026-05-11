namespace ColeHop.Services.Pickup
{
    public sealed record PickupAuthorizationResult(bool IsAuthorized, string? DenialReason);
}
