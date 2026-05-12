namespace ColeHop.Services.Nfc.Dnie
{
    public sealed class DnieSession
    {
        private readonly INfcPlatformService _platformService;
        private readonly string _can;

        public DnieSession(INfcPlatformService platformService, string can)
        {
            _platformService = platformService;
            _can = can;
        }

        public async Task<VerifiedIdentity> ExecuteAsync(CancellationToken cancellationToken)
        {
            System.Diagnostics.Debug.WriteLine("[DNIe] Iniciando sesion de lectura...");

            // Fase 1: Establecer canal seguro con PACE
            var paceSession = new PaceSession(_platformService);
            await paceSession.EstablishSecureChannelAsync(_can, cancellationToken);

            // Fase 2: Crear contexto de Secure Messaging
            var smContext = new SecureMessagingContext(paceSession.KEnc, paceSession.KMac);
            System.Diagnostics.Debug.WriteLine("[DNIe] Secure Messaging activo");

            // Fase 3: Leer Data Groups
            var fileReader = new DnieFileReader(_platformService, smContext);
            var dgs = await fileReader.ReadDataGroupsAsync(cancellationToken);
            var sod = await fileReader.ReadSodAsync(cancellationToken);

            // Fase 4: Validar integridad (SOD)
            var sodValidator = new SodValidator();
            sodValidator.Validate(dgs, sod);

            // Fase 5: Extraer identidad
            var extractor = new DnieIdentityExtractor();
            var identity = extractor.Extract(dgs);

            System.Diagnostics.Debug.WriteLine($"[DNIe] Identidad verificada: {identity.FullName} ({identity.DocumentNumber})");
            return identity;
        }
    }
}
