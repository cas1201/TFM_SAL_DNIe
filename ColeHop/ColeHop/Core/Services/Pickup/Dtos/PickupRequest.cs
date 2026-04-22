namespace ColeHop.Core.Services.Pickup.Dtos
{
    public sealed record PickupRequest(string TeacherId, string ChildId, string AuthorizationId);
}
