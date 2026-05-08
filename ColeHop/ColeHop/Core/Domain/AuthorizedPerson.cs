namespace ColeHop.Model.Domain
{
    public sealed class AuthorizedPerson
    {
        public string Id { get; init; } = default!;
        public string Name { get; init; } = default!;
        public string LastName { get; init; } = default!;
        public string FullName => $"{Name} {LastName}";
        public string Dni { get; init; } = default!;
        public string Relationship { get; init; } = default!;
        public byte[] Photo { get; init; } = [];
        public string TutorId { get; init; } = default!;
    }
}
