namespace ColeHop.Core.Services.Pickup.Dtos
{
    public sealed record PickupContext(string ChildId, string AuthorizationId, string AuthorizedPersonId, DateOnly Date);
}
