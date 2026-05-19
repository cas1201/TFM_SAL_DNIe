# Acciones Pendientes para ColeHop 100%

## Estado Actual: ~45% completado

La aplicación tiene la arquitectura, navegación, UI y estructura NFC base implementadas. Falta la lógica real del DNIe, backend, persistencia y testing.

---

## PRIORIDAD ALTA (Funcionalidad Core)

### 1. Completar Protocolo PACE (PaceSession.cs)
- [x] Implementar MSE:Set AT con OID correcto y datos TLV completos (actualmente APDU incompleto)
- [x] Implementar `DecryptNonce()` con AES-128-CBC real (actualmente es placeholder, retorna el input sin descifrar)
- [x] Implementar Generic Mapping: calcular G' = nonce * G sobre brainpoolP256r1
- [x] Corregir `GenerateEcdhKeyPair()` para usar G' mapeado como generador (actualmente usa G original)
- [x] Implementar `SendMappingDataAsync()` con formato TLV correcto (7C, 81 tags)
- [x] Implementar `ReceiveChipPublicKeyAsync()` con parseo TLV real (actualmente retorna null)
- [x] Implementar `PerformMutualAuthenticationAsync()` con CMAC AES real (actualmente no hace nada)
- [x] Exponer K_enc y K_mac como resultado del canal seguro

### 2. Implementar Secure Messaging
- [x] Crear clase `SecureMessagingContext` con SSC, cifrado AES-CBC y CMAC
- [x] Implementar `ProtectApdu()` (CLA|0x0C, tags 87/97/8E)
- [x] Implementar `UnprotectResponse()` (verificar MAC, descifrar datos)
- [x] Implementar padding ISO 7816-4
- [x] Integrar con PaceSession (exponer contexto SM tras PACE exitoso)

### 3. Implementar DnieFileReader.cs (lectura real)
- [x] Implementar `SelectMrtdApplicationAsync()` con AID A0000002471001
- [x] Implementar `SelectDataGroupAsync()` por FID (0101=DG1, 0102=DG2, 011D=SOD)
- [x] Implementar `ReadBinaryAsync()` con lectura por chunks (max 224 bytes)
- [x] Implementar parseo de longitud TLV para determinar tamaño total del DG
- [x] Todas las comunicaciones deben usar Secure Messaging
- [x] Actualmente retorna arrays vacios

### 4. Implementar SodValidator.cs (validacion real)
- [x] Parsear SOD como CMS SignedData (actualmente estructura placeholder)
- [x] Extraer LDSSecurityObject con hashes de DGs
- [x] Comparar hashes SHA-256 calculados vs esperados
- [x] Verificar firma digital del SOD con certificado embebido
- [x] Validar vigencia del certificado firmante

### 5. Implementar DnieIdentityExtractor.cs
- [x] Parsear DG1 TLV para extraer MRZ (tag 5F1F)
- [x] Parsear MRZ formato TD1 (3 lineas x 30 chars) del DNIe espanol
- [x] Extraer: numero documento, nombre, apellidos, fecha nacimiento, sexo, fecha expiracion
- [x] Opcionalmente extraer foto de DG2 (JPEG2000)

### 6. Integrar flujo completo NFC en NfcService/DnieSession
- [x] Conectar PaceSession -> SecureMessaging -> DnieFileReader -> SodValidator -> DnieIdentityExtractor
- [x] Propagar VerifiedIdentity al NfcScanViewModel
- [x] Manejar errores en cada fase con mensajes descriptivos al usuario

---

## PRIORIDAD MEDIA (Funcionalidad Completa de App)

### 7. Backend y Autenticacion Real
- [x] Implementar API REST (HttpAuthService con endpoints /api/auth/login, /api/auth/register, /api/auth/validate)
- [x] Reemplazar `AuthService` simulado por autenticacion real con JWT (HttpAuthService + MockAuthService condicional)
- [x] Implementar registro de tutores con validacion (HttpAuthService.RegisterTutorAsync)
- [x] Conectar JwtStorage con el flujo de login real

### 8. Persistencia de Datos
- [ ] Implementar base de datos local (SQLite o similar) para:
  - Hijos registrados por tutor
  - Personas autorizadas
  - Autorizaciones con calendario
  - Historial de recogidas (PickupLog)
- [ ] Reemplazar datos simulados en PickupService
- [ ] Reemplazar datos simulados en MockTutorManagementService
- [ ] Sincronizacion con backend cuando haya conexion

### 9. Verificacion de Autorizacion Real
- [x] Tras leer DNIe, comparar DocumentNumber con personas autorizadas del nino (HttpPickupService.CheckAuthorizationAsync)
- [x] Verificar que la autorizacion esta vigente (fecha) - delegado a API backend
- [x] Registrar la recogida en PickupLog con timestamp y datos verificados (HttpPickupService.ConfirmPickupAsync)

### 10. Localizacion Completa
- [x] Revisar que TODOS los strings visibles usan AppResources
- [x] Completar AppResources.en.resx con traducciones al ingles
- [x] Verificar que alertas/mensajes custom usan strings localizados

### 11. Alertas y Mensajes Personalizados
- [x] Reemplazar DisplayAlert nativo por alertas con estilo propio (CustomAlertPopup con iconos contextuales)
- [x] Crear componente de alerta/popup acorde al estilo "facil, divertido, agil, moderno, alegre" (IAlertService + AlertService)

---

## PRIORIDAD BAJA (Calidad y Produccion)

### 12. Testing
- [ ] Crear proyecto ColeHop.Tests (xUnit)
- [ ] Tests unitarios para PaceSession (con mock de INfcPlatformService)
- [ ] Tests unitarios para DnieIdentityExtractor (parseo MRZ)
- [ ] Tests unitarios para SodValidator
- [ ] Tests unitarios para ViewModels principales
- [ ] Tests de integracion con DNIe fisico real

### 13. iOS
- [ ] Completar IosNfcPlatformService con CoreNFC
- [ ] Testing en dispositivo iOS real

### 14. Seguridad y Produccion
- [x] Limpiar CAN de memoria despues de uso (SecureString o zeroing)
- [x] Limpiar claves de sesion tras uso
- [ ] Auditar cumplimiento RGPD/LOPD para datos biometricos
- [x] Rate limiting en intentos de lectura NFC

### 15. UX y Polish
- [x] Indicadores de progreso por fase durante lectura NFC
- [x] Instrucciones visuales para posicionar DNI sobre el telefono
- [ ] Notificaciones push para tutores (recogida completada)
- [ ] Exportacion de informes de recogidas

### 16. Documentacion y Mantenimiento
- [x] Actualizar RESUMEN_CODIFICACION_COLEHOP.md con progreso
- [x] Eliminar archivos innecesarios (dotnet_bot.png, AboutAssets.txt)
- [x] Revisar que no hay TODOs sin contexto en el codigo

---

## Dependencias Externas Necesarias

| Paquete | Uso | Estado |
|---------|-----|--------|
| BouncyCastle.Cryptography | PACE, SM, SOD | Ya incluido |
| (Backend API) | Auth, datos | Por definir |
| (SQLite/EF Core) | Persistencia local | Por agregar |

---

## Orden Recomendado de Ejecucion

1. **Fase DNIe completa** (acciones 1-6): Es el core diferencial de la app
2. **Alertas personalizadas** (accion 11): Mejora UX inmediata
3. **Localizacion** (accion 10): Requerido por instrucciones
4. **Backend + Persistencia** (acciones 7-9): Cuando haya API disponible
5. **Testing** (accion 12): Tras estabilizar logica
6. **iOS + Produccion** (acciones 13-15): Fase final

---

*Generado el 12/05/2026 basado en el analisis del codigo actual vs CONTEXTO_PROYECTO_COLEHOP.md y DNIE_IMPLEMENTATION_GUIDE.md*

