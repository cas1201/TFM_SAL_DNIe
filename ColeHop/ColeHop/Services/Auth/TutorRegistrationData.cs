namespace ColeHop.Services.Auth
{
    public sealed record TutorRegistrationData(string Name, string LastName, string Dni, string Email, string Password, string School);
}
