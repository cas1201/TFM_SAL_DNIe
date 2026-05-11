using ColeHop.Services.Nfc;

namespace ColeHop.Services.Nfc.Dnie
{
    public sealed class DnieIdentityExtractor
    {
        public VerifiedIdentity Extract(Dictionary<string, byte[]> dataGroups)
        {
            // Parse real de DG1
            var dni = "00000000X";
            var fullName = "Nombre Apellido";

            return new VerifiedIdentity(dni, fullName);
        }
    }
}
