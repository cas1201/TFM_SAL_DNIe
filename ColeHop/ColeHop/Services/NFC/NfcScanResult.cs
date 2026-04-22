namespace ColeHop.Services.Nfc
{
    public sealed record NfcScanResult(byte[]? RawData, bool IsValid, string? ErrorMessage);
}