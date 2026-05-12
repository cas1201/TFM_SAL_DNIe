# Acciones Realizadas - Prioridad Alta

## Fecha: 12/05/2026

---

## Accion 1: Completar Protocolo PACE (PaceSession.cs)

**Archivo:** `ColeHop/Services/Nfc/Dnie/PaceSession.cs`

**Que se hizo:**
Se reescribio completamente PaceSession para implementar el protocolo PACE segun TR-03110.

**Detalles tecnicos:**
- **MSE:Set AT**: Envia el OID correcto `id-PACE-ECDH-GM-AES-CBC-CMAC-128` (0.4.0.127.0.7.2.2.4.2.2) con referencia al CAN (tag 83, valor 02).
- **Nonce cifrado**: Se solicita con GENERAL AUTHENTICATE (INS=86, CLA=10 para chaining). Se parsea la respuesta TLV buscando tag 80.
- **Descifrado del nonce**: AES-128-CBC con IV=0 usando K_pi (primeros 16 bytes del SHA-256 del CAN).
- **Generic Mapping**: G' = nonce * G sobre brainpoolP256r1. El nonce se interpreta como BigInteger escalar.
- **Generacion ECDH**: Se genera par de claves efimeras sobre el dominio mapeado (usando G' como generador, no G).
- **Intercambio de claves**: Se envia PK_terminal en formato TLV (7C > 81 > PK). Se recibe PK_chip (tag 82).
- **Secreto compartido**: ECDH basico, resultado normalizado a 32 bytes.
- **Derivacion de claves**: KDF con SHA-256 + contador big-endian (1 para K_enc, 2 para K_mac). Se toman 16 bytes de cada uno.
- **Autenticacion mutua**: CMAC AES-128 sobre PK_chip||PK_terminal (terminal) y PK_terminal||PK_chip (chip). Se envian/verifican los primeros 8 bytes.
- **Expone**: `KEnc` y `KMac` como propiedades publicas para uso en SecureMessaging.

---

## Accion 2: Implementar Secure Messaging (SecureMessagingContext.cs)

**Archivo:** `ColeHop/Services/Nfc/Dnie/SecureMessagingContext.cs` (NUEVO)

**Que se hizo:**
Se creo la clase que cifra y autentica todas las comunicaciones APDU tras establecer PACE.

**Detalles tecnicos:**
- **ProtectApdu()**: Transforma un APDU plano en uno protegido:
  - CLA se modifica con OR 0x0C (bit SM activado)
  - Datos se cifran con AES-128-CBC (DO tag 87, con indicador padding 0x01)
  - Le se incluye como DO tag 97
  - MAC se calcula con CMAC AES-128 sobre SSC + header con padding + DOs con padding (DO tag 8E, 8 bytes)
  - IV = AES-ECB(K_enc, SSC)
- **UnprotectResponse()**: Proceso inverso:
  - Parsea DOs de la respuesta (87=datos cifrados, 99=SW, 8E=MAC)
  - Verifica MAC antes de descifrar
  - Descifra datos y remueve padding ISO 7816-4
- **SSC (Send Sequence Counter)**: 16 bytes, se incrementa como big-endian antes de cada operacion (envio y recepcion).
- **Padding ISO 7816-4**: 0x80 seguido de 0x00 hasta completar bloque de 16 bytes.

---

## Accion 3: Implementar DnieFileReader.cs (Lectura Real)

**Archivo:** `ColeHop/Services/Nfc/Dnie/DnieFileReader.cs`

**Que se hizo:**
Se reescribio para leer los Data Groups reales del DNIe usando Secure Messaging.

**Detalles tecnicos:**
- **SelectMrtdApplication**: Selecciona la aplicacion eMRTD con AID A0000002471001 (SELECT por nombre, P1=04, P2=0C).
- **SelectFile**: Selecciona DGs por FID (P1=02, P2=0C): DG1=0101, DG2=0102, SOD=011D.
- **ReadBinary**: Lee datos con READ BINARY (INS=B0), offset en P1P2, chunks de 223 bytes maximo.
- **ParseFileLength**: Lee los primeros 4 bytes para determinar el tamano total del archivo via TLV (soporta tags de 1 y 2 bytes, longitudes cortas y largas).
- **Todas las APDUs** pasan por `_smContext.ProtectApdu()` y las respuestas por `_smContext.UnprotectResponse()`.

---

## Accion 4: Ampliar VerifiedIdentity

**Archivo:** `ColeHop/Services/Nfc/VerifiedIdentity.cs`

**Que se hizo:**
Se transformo de un record simple `(Dni, FullName)` a una clase completa con todos los datos de identidad.

**Campos:**
- `DocumentNumber` - Numero del DNI
- `GivenNames` - Nombre(s)
- `Surnames` - Apellido(s)
- `FullName` - Propiedad calculada: `$"{GivenNames} {Surnames}"`
- `Dni` - Alias de DocumentNumber (compatibilidad retroactiva)
- `DateOfBirth` - Fecha de nacimiento
- `Sex` - Sexo (Masculino/Femenino/No especificado)
- `ExpirationDate` - Fecha de caducidad del documento
- `Nationality` - Nacionalidad (codigo 3 letras)
- `FaceImage` - Foto facial en bytes (nullable, extraida de DG2)

---

## Accion 5: Implementar DnieIdentityExtractor.cs (Parseo MRZ)

**Archivo:** `ColeHop/Services/Nfc/Dnie/DnieIdentityExtractor.cs`

**Que se hizo:**
Se implemento la extraccion real de datos de identidad desde el DG1 del DNIe.

**Detalles tecnicos:**
- **Extraccion MRZ de DG1**: Busca tag 5F1F dentro de la estructura TLV del DG1 (contenedor 61).
- **Parseo MRZ TD1**: Formato del DNIe espanol = 3 lineas de 30 caracteres:
  - Linea 1: `IDESP` + Apellidos `<<` Nombres (relleno con `<`)
  - Linea 2: Documento(9) + check(1) + Nacionalidad(3) + Nacimiento YYMMDD(6) + check(1) + Sexo(1) + Expiracion YYMMDD(6) + check(1) + opcional
  - Linea 3: Continuacion de nombres si son largos
- **Parseo de fechas**: YYMMDD con ajuste de siglo (< 50 = 2000+, >= 50 = 1900+).
- **Extraccion de foto (DG2)**: Busca marcadores JPEG (FF D8) o JPEG2000 (00 00 00 0C 6A 50) dentro del DG2.

---

## Accion 6: Implementar SodValidator.cs (Validacion Criptografica)

**Archivo:** `ColeHop/Services/Nfc/Dnie/SodValidator.cs`

**Que se hizo:**
Se implemento la validacion real del SOD usando BouncyCastle CMS.

**Detalles tecnicos:**
- **Parseo CMS**: El SOD es un CMS SignedData (PKCS#7). Se parsea con `CmsSignedData` de BouncyCastle.
- **Extraccion de hashes**: Se navega la estructura ASN.1 del LDSSecurityObject para encontrar pares (DGNumber, Hash) como secuencias de (INTEGER, OCTET STRING).
- **Validacion de hashes**: Para cada DG leido, se calcula SHA-256 y se compara con el hash esperado del SOD. Si no coincide, se lanza excepcion.
- **Verificacion de firma**: Se obtiene el SignerInfo, se localiza el certificado del firmante por SignerID, y se verifica la firma con `signer.Verify(cert)`.
- **Vigencia**: Se verifica que el certificado del firmante no este expirado con `CheckValidity(DateTime.UtcNow)`.

---

## Accion Integracion: DnieSession.cs

**Archivo:** `ColeHop/Services/Nfc/Dnie/DnieSession.cs`

**Que se hizo:**
Se actualizo para conectar todas las fases en secuencia:

```
PACE (PaceSession) -> SecureMessaging (SecureMessagingContext) -> Lectura (DnieFileReader) -> Validacion (SodValidator) -> Extraccion (DnieIdentityExtractor)
```

El flujo completo desde que el usuario acerca el DNI al telefono hasta obtener la identidad verificada esta ahora implementado end-to-end.

---

## Archivos Auxiliares Actualizados

- **`Helpers/Asn1Utils.cs`**: Utilidades para parseo TLV (ParseTlvLength, FindTag, EncodeTlvLength).
- **`Helpers/ByteExtensions.cs`**: Extension methods para padding ISO 7816-4, conversion hex, incremento de contador.
- **`Services/Nfc/NfcService.cs`**: Limpieza de usings duplicados.

---

## Estado de Compilacion

Compilacion exitosa sin errores ni warnings.

---

## Notas Importantes

1. **Testing con DNIe real**: La implementacion sigue fielmente TR-03110 y ICAO 9303, pero requiere validacion con un DNIe 3.0 fisico.
2. **Logging extensivo**: Cada fase tiene logs de Debug para facilitar depuracion durante testing.
3. **Compatibilidad retroactiva**: `VerifiedIdentity.Dni` y `.FullName` siguen existiendo, por lo que el resto del codigo (ViewModels, PickupService) no requiere cambios.
4. **BouncyCastle**: Se usa para todo el crypto (AES, CMAC, ECDH, CMS). No se anadieron dependencias nuevas.
