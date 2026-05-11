namespace ColeHop.Services.Pickup
{
    public sealed record DailyPickupItem(string ChildId, string ChildName, string AuthorizedPersonName, bool AlreadyPickedUp);
}
