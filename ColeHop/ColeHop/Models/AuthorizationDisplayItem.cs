namespace ColeHop.Models
{
    public sealed class AuthorizationDisplayItem
    {
        public string Id { get; init; } = default!;
        public string PersonName { get; init; } = default!;
        public string Relationship { get; init; } = default!;
        public string ChildrenNames { get; init; } = default!;
        public List<string> ChildrenList { get; init; } = [];
        public DateOnly FromDate { get; init; }
        public DateOnly ToDate { get; init; }
        public string DateRange { get; init; } = default!;
        public bool IsToday { get; init; }
        public bool IsCompleted { get; init; }
        public int PickedUpCount { get; init; }
        public int TotalChildren { get; init; }
        public bool HasPartialProgress => IsToday && PickedUpCount > 0 && !IsCompleted;
        public string ProgressText => $"{PickedUpCount}/{TotalChildren}";
    }
}
