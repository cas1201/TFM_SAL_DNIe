namespace ColeHop.Core.Services.Auth.Dtos
{
    public sealed record TutorRegistrationData(string Name, string LastName, string Dni, string Email, string Password, string School, string Course);
}
