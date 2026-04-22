using ColeHop.Core.Services.Nfc;
using ColeHop.Services.Nfc;

namespace ColeHop.ViewModel
{
    public sealed class NfcScanViewModel
    {
        readonly INfcService _nfc;

        public NfcScanViewModel(INfcService nfc)
        {
            _nfc = nfc;
            _nfc.TagDetected += OnTagDetected;
        }

        private void OnTagDetected(object? sender, NfcScanResult nfcReadResult)
        {
            // RESULTADO LEIDO: nfcReadResult
        }
    }
}
