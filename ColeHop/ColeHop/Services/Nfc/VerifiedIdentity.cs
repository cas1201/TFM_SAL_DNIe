namespace ColeHop.Services.Nfc
{
    public sealed class VerifiedIdentity
    {
        public string DocumentNumber { get; init; } = string.Empty;
        public string GivenNames { get; init; } = string.Empty;
        public string Surnames { get; init; } = string.Empty;
        public string FullName => $"{GivenNames} {Surnames}".Trim();
        public string Dni => DocumentNumber;
        public DateTime DateOfBirth { get; init; }
        public string Sex { get; init; } = string.Empty;
        public DateTime ExpirationDate { get; init; }
        public string Nationality { get; init; } = string.Empty;
        public byte[]? FaceImage { get; init; }
    }
}
