namespace ColeHop.Services.Pickup
{
    public sealed record PickupRequest(string TeacherId, string ChildId, string AuthorizationId);
}
