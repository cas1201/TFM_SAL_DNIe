using ColeHop.Services.NFC;

namespace ColeHop.ViewModel
{
    public class NfcScanViewModel
    {
        readonly INfcService _nfc;

        public NfcScanViewModel(INfcService nfc)
        {
            _nfc = nfc;
            _nfc.TagDetected += OnTagDetected;
        }

        private void OnTagDetected(object? sender, NfcScanResult e)
        {
            // RESULTADO LEIDO: NfcScanResult e
        }
    }
}
