namespace ColeHop.Services.TutorManagement
{
    public sealed record AuthorizedPersonData(string Name, string LastName, string Dni, string Relationship, byte[] Photo);
}
