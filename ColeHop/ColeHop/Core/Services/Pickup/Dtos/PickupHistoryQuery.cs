namespace ColeHop.Core.Services.Pickup.Dtos
{
    public sealed record PickupHistoryQuery(DateOnly? FromDate, DateOnly? ToDate, string? ChildId);
}
