namespace ColeHop.Services.Nfc.Dnie
{
    public sealed class DnieSession
    {
        private readonly INfcPlatformService _platformService;
        private readonly string _can;
        private readonly IProgress<string>? _progress;

        public DnieSession(INfcPlatformService platformService, string can, IProgress<string>? progress = null)
        {
            _platformService = platformService;
            _can = can;
            _progress = progress;
        }

        public async Task<VerifiedIdentity> ExecuteAsync(CancellationToken cancellationToken)
        {
            System.Diagnostics.Debug.WriteLine("[DNIe] Iniciando sesion de lectura...");

            // Fase 1: Establecer canal seguro con PACE
            _progress?.Report("Estableciendo canal seguro (PACE)...");
            using var paceSession = new PaceSession(_platformService);
            await paceSession.EstablishSecureChannelAsync(_can, cancellationToken);

            // Fase 2: Crear contexto de Secure Messaging
            _progress?.Report("Canal seguro activo. Preparando lectura...");
            using var smContext = new SecureMessagingContext(paceSession.KEnc, paceSession.KMac, paceSession.InitialSsc);
            System.Diagnostics.Debug.WriteLine("[DNIe] Secure Messaging activo");

            // Fase 3: Leer Data Groups
            _progress?.Report("Leyendo datos del DNIe...");
            var fileReader = new DnieFileReader(_platformService, smContext);
            var dgs = await fileReader.ReadDataGroupsAsync(cancellationToken);
            var sod = await fileReader.ReadSodAsync(cancellationToken);

            // Fase 4: Validar integridad (SOD)
            _progress?.Report("Validando integridad del documento...");
            var sodValidator = new SodValidator();
            sodValidator.Validate(dgs, sod);

            // Fase 5: Extraer identidad
            _progress?.Report("Extrayendo identidad...");
            var extractor = new DnieIdentityExtractor();
            var identity = extractor.Extract(dgs);

            _progress?.Report("Identidad verificada correctamente");
            System.Diagnostics.Debug.WriteLine($"[DNIe] Identidad verificada: {identity.FullName} ({identity.DocumentNumber})");
            return identity;
        }
    }
}
