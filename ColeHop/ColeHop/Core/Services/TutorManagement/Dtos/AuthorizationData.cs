namespace ColeHop.Core.Services.TutorManagement.Dtos
{
    public sealed record AuthorizationData(IReadOnlyList<string> ChildIds, string AuthorizedPersonId, DateOnly FromDate, DateOnly ToDate);
}
