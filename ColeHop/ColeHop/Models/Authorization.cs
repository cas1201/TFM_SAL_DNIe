namespace ColeHop.Models
{
    public sealed class Authorization
    {
        public string Id { get; init; } = default!;
        public DateOnly FromDate { get; init; }
        public DateOnly ToDate { get; init; }
        public IReadOnlyList<string> ChildIds { get; init; } = [];
        public string AuthorizedPersonId { get; init; } = default!;
        public string TutorId { get; init; } = default!;
        public bool IsActive { get; init; }
    }
}
