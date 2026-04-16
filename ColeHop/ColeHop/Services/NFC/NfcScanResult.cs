namespace ColeHop.Services.NFC
{
    public class NfcScanResult
    {
        public string Uid { get; init; } = string.Empty;
        public byte[]? RawData { get; init; }
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    }
}
