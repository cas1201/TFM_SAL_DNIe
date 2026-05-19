# Guía de Implementación - Lectura NFC del DNIe 3.0

## Resumen

Esta aplicación implementa la lectura del DNI electrónico español (DNIe 3.0) mediante NFC, utilizando el protocolo PACE (Password Authenticated Connection Establishment) para establecer un canal seguro con el chip.

## Arquitectura

```
NfcScanViewModel
  └── NfcService
		└── DnieSession
			  ├── PaceSession          (Autenticación PACE)
			  ├── SecureMessagingContext (Canal cifrado AES-128)
			  ├── DnieFileReader        (Lectura de archivos del chip)
			  ├── SodValidator          (Validación de integridad)
			  └── DnieIdentityExtractor (Extracción de datos personales)
```

## Flujo de Ejecución

### 1. Establecimiento del Canal Seguro (PACE)

**Archivo:** `ColeHop/Services/Nfc/Dnie/PaceSession.cs`

El protocolo PACE establece un canal seguro usando el CAN (Card Access Number) de 6 dígitos impreso en el DNIe.

**Parámetros criptográficos del DNIe 3.0:**
- Curva elíptica: brainpoolP256r1
- Cifrado: AES-128-CBC
- MAC: AES-128-CMAC
- KDF Hash: SHA-1 (para todas las derivaciones)
- Mapping: Generic Mapping

**Pasos:**
1. Seleccionar Master File y leer EF.CardAccess
2. Derivar K_pi desde CAN: `SHA-1(CAN_bytes || 00 00 00 03)` truncado a 16 bytes
3. Iniciar PACE (MSE:Set AT)
4. Obtener nonce cifrado del chip y descifrarlo con K_pi (AES-CBC, IV=0)
5. Primera ronda ECDH: mapping genérico para obtener G' (nuevo generador)
6. Segunda ronda ECDH: generar claves efímeras sobre G'
7. Derivar claves de sesión (K_enc, K_mac) usando KDF con SHA-1
8. Autenticación mutua con auth tokens (estructura 7F49 con OID y punto público)

**Detalles críticos:**
- La derivación de K_pi usa SHA-1, NO SHA-256
- El auth token se construye con wrapper `7F49` conteniendo OID `04 00 7F 00 07 02 02 04 02 02` y el punto público de la contraparte (tag `86`)
- El SSC (Send Sequence Counter) inicial es todo ceros (16 bytes) para PACE con AES

### 2. Secure Messaging (SM)

**Archivo:** `ColeHop/Services/Nfc/Dnie/SecureMessagingContext.cs`

Una vez establecido PACE, toda la comunicación se protege con SM:

- **Cifrado:** AES-128-CBC con IV = AES_ECB(K_enc, SSC)
- **MAC:** AES-128-CMAC sobre: SSC || pad(header modificado) || pad(DOs)
- **Padding:** ISO 9797-1 método 2 (0x80 seguido de 0x00 hasta múltiplo de 16)
- **SSC:** Se incrementa antes de proteger y antes de verificar

**Estructura APDU protegido:**
- Header con CLA |= 0x0C
- DO87 (datos cifrados): `87-L-01-{cifrado}`
- DO97 (Le esperado): `97-01-{Le}`
- DO8E (MAC): `8E-08-{mac}`

### 3. Lectura de Archivos

**Archivo:** `ColeHop/Services/Nfc/Dnie/DnieFileReader.cs`

**Archivos leídos:**
| FID    | Contenido |
|--------|-----------|
| 0x0101 | DG1 - Datos MRZ (nombre, fecha nacimiento, etc.) |
| 0x0102 | DG2 - Foto facial (JPEG2000, ~18KB) |
| 0x011D | SOD - Security Object Document (firma e integridad) |

**Proceso:**
1. Seleccionar aplicación eMRTD (AID: A0 00 00 02 47 10 01)
2. Seleccionar archivo por FID
3. Leer cabecera (4 bytes) para determinar tamaño total
4. Leer en chunks de 223 bytes (0xDF) máximo

### 4. Validación de Integridad (SOD)

**Archivo:** `ColeHop/Services/Nfc/Dnie/SodValidator.cs`

El SOD contiene un CMS SignedData con los hashes de los Data Groups. La validación se realiza parseando manualmente la estructura ASN.1 (sin usar CmsSignedData de BouncyCastle, que tiene incompatibilidades con la codificación BER del DNIe).

**Estructura del SOD:**
```
Tag 0x77 (wrapper ICAO)
  └── SEQUENCE (ContentInfo)
		├── OID 1.2.840.113549.1.7.2 (signedData)
		└── [0] SEQUENCE (SignedData)
			  ├── INTEGER (version)
			  ├── SET (digestAlgorithms)
			  ├── SEQUENCE (encapContentInfo)
			  │     ├── OID (contentType)
			  │     └── [0] OCTET STRING (LDSSecurityObject)
			  ├── [0] (certificates)
			  └── SET (signerInfos)
```

La autenticidad del chip está garantizada por PACE. Solo se validan los hashes de integridad.

### 5. Extracción de Identidad

**Archivo:** `ColeHop/Services/Nfc/Dnie/DnieIdentityExtractor.cs`

Parsea DG1 (formato MRZ TD1) para extraer:
- Nombre completo
- Número de documento
- Fecha de nacimiento
- Fecha de expiración
- Nacionalidad

## Dependencias

- **BouncyCastle.Cryptography**: Criptografía (AES, CMAC, ECDH, SHA, ASN.1)
- **.NET MAUI**: Framework UI multiplataforma
- **Android NFC (IsoDep)**: Comunicación NFC ISO 14443-4

## Configuración Necesaria

### AndroidManifest.xml
```xml
<uses-permission android:name="android.permission.NFC" />
<uses-feature android:name="android.hardware.nfc" android:required="true" />
```

### Timeout
El timeout de lectura está configurado a 30 segundos (NfcScanViewModel.cs) para permitir la transferencia completa de DG2 (~18KB en chunks de 223 bytes por NFC).

## Problemas Conocidos y Soluciones

| Problema | Causa | Solución |
|----------|-------|----------|
| PACE error 69-88 | KDF incorrecto o auth token mal formado | Usar SHA-1 para KDF, wrapper 7F49 con OID completo |
| SM error 69-88 | SSC inicial incorrecto | SSC = 16 bytes a cero (no derivar de claves públicas) |
| BouncyCastle DLSequence error | DNIe usa BER, no DER estricto | Parser ASN.1 manual sin CmsSignedData |
| Timeout de lectura | DG2 es ~18KB por NFC lento | Timeout de 30s |
