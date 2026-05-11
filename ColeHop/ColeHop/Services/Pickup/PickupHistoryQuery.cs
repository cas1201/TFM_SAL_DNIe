namespace ColeHop.Services.Pickup
{
    public sealed record PickupHistoryQuery(DateOnly? FromDate, DateOnly? ToDate, string? ChildId);
}
