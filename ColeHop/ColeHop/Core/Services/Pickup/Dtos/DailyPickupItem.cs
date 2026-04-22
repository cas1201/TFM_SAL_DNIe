namespace ColeHop.Core.Services.Pickup.Dtos
{
    public sealed record DailyPickupItem(string ChildId, string ChildName, string AuthorizedPersonName, bool AlreadyPickedUp);
}
