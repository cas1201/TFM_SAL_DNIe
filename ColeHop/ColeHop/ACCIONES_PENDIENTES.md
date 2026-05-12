# Acciones Pendientes para ColeHop 100%

## Estado Actual: ~45% completado

La aplicación tiene la arquitectura, navegación, UI y estructura NFC base implementadas. Falta la lógica real del DNIe, backend, persistencia y testing.

---

## PRIORIDAD ALTA (Funcionalidad Core)

### 1. Completar Protocolo PACE (PaceSession.cs)
- [ ] Implementar MSE:Set AT con OID correcto y datos TLV completos (actualmente APDU incompleto)
- [ ] Implementar `DecryptNonce()` con AES-128-CBC real (actualmente es placeholder, retorna el input sin descifrar)
- [ ] Implementar Generic Mapping: calcular G' = nonce * G sobre brainpoolP256r1
- [ ] Corregir `GenerateEcdhKeyPair()` para usar G' mapeado como generador (actualmente usa G original)
- [ ] Implementar `SendMappingDataAsync()` con formato TLV correcto (7C, 81 tags)
- [ ] Implementar `ReceiveChipPublicKeyAsync()` con parseo TLV real (actualmente retorna null)
- [ ] Implementar `PerformMutualAuthenticationAsync()` con CMAC AES real (actualmente no hace nada)
- [ ] Exponer K_enc y K_mac como resultado del canal seguro

### 2. Implementar Secure Messaging
- [ ] Crear clase `SecureMessagingContext` con SSC, cifrado AES-CBC y CMAC
- [ ] Implementar `ProtectApdu()` (CLA|0x0C, tags 87/97/8E)
- [ ] Implementar `UnprotectResponse()` (verificar MAC, descifrar datos)
- [ ] Implementar padding ISO 7816-4
- [ ] Integrar con PaceSession (exponer contexto SM tras PACE exitoso)

### 3. Implementar DnieFileReader.cs (lectura real)
- [ ] Implementar `SelectMrtdApplicationAsync()` con AID A0000002471001
- [ ] Implementar `SelectDataGroupAsync()` por FID (0101=DG1, 0102=DG2, 011D=SOD)
- [ ] Implementar `ReadBinaryAsync()` con lectura por chunks (max 224 bytes)
- [ ] Implementar parseo de longitud TLV para determinar tamaño total del DG
- [ ] Todas las comunicaciones deben usar Secure Messaging
- [ ] Actualmente retorna arrays vacios

### 4. Implementar SodValidator.cs (validacion real)
- [ ] Parsear SOD como CMS SignedData (actualmente estructura placeholder)
- [ ] Extraer LDSSecurityObject con hashes de DGs
- [ ] Comparar hashes SHA-256 calculados vs esperados
- [ ] Verificar firma digital del SOD con certificado embebido
- [ ] Validar vigencia del certificado firmante

### 5. Implementar DnieIdentityExtractor.cs
- [ ] Parsear DG1 TLV para extraer MRZ (tag 5F1F)
- [ ] Parsear MRZ formato TD1 (3 lineas x 30 chars) del DNIe espanol
- [ ] Extraer: numero documento, nombre, apellidos, fecha nacimiento, sexo, fecha expiracion
- [ ] Opcionalmente extraer foto de DG2 (JPEG2000)

### 6. Integrar flujo completo NFC en NfcService/DnieSession
- [ ] Conectar PaceSession -> SecureMessaging -> DnieFileReader -> SodValidator -> DnieIdentityExtractor
- [ ] Propagar VerifiedIdentity al NfcScanViewModel
- [ ] Manejar errores en cada fase con mensajes descriptivos al usuario

---

## PRIORIDAD MEDIA (Funcionalidad Completa de App)

### 7. Backend y Autenticacion Real
- [ ] Implementar API REST (o definir endpoint si ya existe)
- [ ] Reemplazar `AuthService` simulado por autenticacion real con JWT
- [ ] Implementar registro de tutores con validacion
- [ ] Conectar JwtStorage con el flujo de login real

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
- [ ] Tras leer DNIe, comparar DocumentNumber con personas autorizadas del nino
- [ ] Verificar que la autorizacion esta vigente (fecha)
- [ ] Registrar la recogida en PickupLog con timestamp y datos verificados

### 10. Localizacion Completa
- [ ] Revisar que TODOS los strings visibles usan AppResources
- [ ] Completar AppResources.en.resx con traducciones al ingles
- [ ] Verificar que alertas/mensajes custom usan strings localizados

### 11. Alertas y Mensajes Personalizados
- [ ] Reemplazar DisplayAlert nativo por alertas con estilo propio (segun instrucciones: no usar estilo por defecto de Android)
- [ ] Crear componente de alerta/popup acorde al estilo "facil, divertido, agil, moderno, alegre"

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
- [ ] Limpiar CAN de memoria despues de uso (SecureString o zeroing)
- [ ] Limpiar claves de sesion tras uso
- [ ] Auditar cumplimiento RGPD/LOPD para datos biometricos
- [ ] Rate limiting en intentos de lectura NFC

### 15. UX y Polish
- [ ] Indicadores de progreso por fase durante lectura NFC
- [ ] Instrucciones visuales para posicionar DNI sobre el telefono
- [ ] Notificaciones push para tutores (recogida completada)
- [ ] Exportacion de informes de recogidas

### 16. Documentacion y Mantenimiento
- [ ] Actualizar RESUMEN_CODIFICACION_COLEHOP.md con progreso
- [ ] Eliminar archivos innecesarios (dotnet_bot.png, AboutAssets.txt)
- [ ] Revisar que no hay TODOs sin contexto en el codigo

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
