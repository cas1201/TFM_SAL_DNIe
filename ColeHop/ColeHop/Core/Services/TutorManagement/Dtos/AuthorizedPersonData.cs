namespace ColeHop.Core.Services.TutorManagement.Dtos
{
    public sealed record AuthorizedPersonData(string Name, string LastName, string Dni, string Relationship, byte[] Photo);
}
