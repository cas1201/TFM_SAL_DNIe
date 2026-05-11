namespace ColeHop.Services.Pickup
{
    public sealed record PickupContext(string ChildId, string AuthorizationId, string AuthorizedPersonId, DateOnly Date);
}
