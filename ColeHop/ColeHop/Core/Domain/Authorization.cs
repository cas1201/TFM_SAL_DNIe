namespace ColeHop.Model.Domain
{
    public sealed class Authorization
    {
        public DateTime Date { get; init; }
        public IReadOnlyList<string> ChildIds { get; init; } = [];
        public string AuthorizedPersonId { get; init; } = default!;
    }
}
