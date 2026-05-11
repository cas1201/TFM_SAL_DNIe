namespace ColeHop.Services.TutorManagement
{
    public sealed record AuthorizationData(IReadOnlyList<string> ChildIds, string AuthorizedPersonId, DateOnly FromDate, DateOnly ToDate);
}
