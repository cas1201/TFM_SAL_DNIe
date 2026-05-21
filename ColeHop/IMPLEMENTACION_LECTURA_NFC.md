# Implementación de Lectura NFC del DNIe 3.0: Análisis Comparativo y Seguridad

## 1. Resumen de Diferencias

| Aspecto | SDK Policía Nacional (FNMT) | ColeHop (esta app) |
|---------|---------------------------|---------------------|
| **Lenguaje/Plataforma** | Java/Android nativo | C# / .NET MAUI (multiplataforma) |
| **Nivel de abstracción** | Alto (SDK encapsula todo en `Loader.init()` + `getMrtdCardInfo()`) | Bajo (implementación manual de PACE, Secure Messaging, lectura de DGs) |
| **Protocolo PACE** | Interno al SDK (transparente al desarrollador) | Implementado manualmente paso a paso (Generic Mapping con brainpoolP256r1) |
| **Secure Messaging** | Interno al SDK | Implementado manualmente (AES-CBC cifrado + AES-CMAC autenticación) |
| **Validación SOD** | No visible en el ejemplo (posiblemente interna) | Explícita: parseo ASN.1 del CMS SignedData, verificación de hashes SHA-256 |
| **Datos leídos** | DG1 (MRZ/fecha nacimiento) + DG2 (foto) | DG1 (MRZ completa) + DG2 (foto) + SOD (integridad) |
| **Gestión NFC** | `enableReaderMode` con `FLAG_READER_NFC_A | NFC_B | SKIP_NDEF` | Foreground Dispatch con filtro ISO-DEP + IsoDep tech |
| **Dependencias criptográficas** | `jmulticard` (Policía Nacional), androsmex (PACE interno) | BouncyCastle (C#) para ECDH, AES, CMAC, ASN.1 |
| **Control de errores CAN** | Excepción con mensaje "CAN incorrecto" del SDK | Fallo en autenticación mutua PACE (MAC inválido) |

## 2. Flujo de Lectura en el SDK de la Policía Nacional (FNMT)

```
onTagDiscovered(Tag tag)
  └─> Loader.init(new String[]{CAN}, tag)     // Abstracción total
	   └─> .getMrtdCardInfo()                   // Devuelve MrtdCard
			├─> .getDataGroup1()               // DG1_Dnie (fecha nacimiento, etc.)
			└─> .getDataGroup2()               // DG2 (foto JPEG2000)
```

El SDK oculta completamente:
- La negociación PACE
- El establecimiento de Secure Messaging
- La selección de ficheros (SELECT + READ BINARY)
- Cualquier validación de integridad

## 3. Flujo de Lectura en ColeHop (Implementación Propia)

```
BeginDnieReadingAsync(CAN)
  ├─ WaitForTagAsync()                          // Espera detección IsoDep
  └─ DnieSession.ExecuteAsync()
	   │
	   ├─ FASE 1: PaceSession.EstablishSecureChannelAsync(CAN)
	   │    ├─ SELECT MF (3F00)
	   │    ├─ SELECT + READ EF.CardAccess (011C)
	   │    ├─ DeriveKeyFromCan: SHA-1(CAN || 00000003)[0:16]
	   │    ├─ MSE:Set AT (OID PACE-ECDH-GM-AES-CBC-CMAC-128, ref=CAN, domain=brainpoolP256r1)
	   │    ├─ GENERAL AUTHENTICATE: obtener nonce cifrado
	   │    ├─ Descifrar nonce con AES-CBC(K_pi, IV=0)
	   │    ├─ Generic Mapping: ECDH sobre G original → H; G' = s·G + H
	   │    ├─ GENERAL AUTHENTICATE: intercambio clave efímera sobre G'
	   │    ├─ Shared Secret: ECDH sobre dominio mapeado
	   │    ├─ KDF: K_Enc = SHA-1(SS || 00000001)[0:16], K_Mac = SHA-1(SS || 00000002)[0:16]
	   │    └─ Autenticación mutua: CMAC cruzado sobre auth tokens
	   │
	   ├─ FASE 2: SecureMessagingContext(K_Enc, K_Mac, SSC=0)
	   │    └─ Cada APDU posterior se protege con:
	   │         ├─ DO87: datos cifrados AES-CBC (padding ISO 9797-1 M2)
	   │         ├─ DO97: Le esperado
	   │         └─ DO8E: MAC (AES-CMAC truncado a 8 bytes)
	   │
	   ├─ FASE 3: DnieFileReader.ReadDataGroupsAsync()
	   │    ├─ SELECT aplicación eMRTD (AID: A0000002471001)
	   │    ├─ SELECT + READ BINARY DG1 (FID 0101) — chunked (max 0xDF bytes)
	   │    └─ SELECT + READ BINARY DG2 (FID 0102) — chunked
	   │
	   ├─ FASE 4: SodValidator.Validate()
	   │    ├─ SELECT + READ EF.SOD (FID 011D)
	   │    ├─ Parseo CMS SignedData → extraer LDSSecurityObject
	   │    ├─ Extraer hashes esperados por DG
	   │    └─ Comparar SHA-256(DG_leído) vs hash_esperado
	   │
	   └─ FASE 5: DnieIdentityExtractor.Extract()
			├─ Parsear DG1 → extraer MRZ (tag 5F1F)
			├─ Interpretar MRZ formato TD1 (ICAO 9303): 3×30 chars
			└─ Extraer imagen facial de DG2
```

## 4. Análisis de Ciberseguridad

### 4.1. Protocolo PACE (Password Authenticated Connection Establishment)

**Propósito:** Establecer un canal cifrado y autenticado entre el terminal (móvil) y el chip del DNIe, demostrando que el terminal conoce el CAN (sin revelarlo en claro).

**Propiedades de seguridad:**
- **Autenticación mutua:** Ambas partes demuestran conocimiento del CAN mediante verificación cruzada de MACs.
- **Perfect Forward Secrecy:** Las claves efímeras ECDH se generan por sesión; comprometer el CAN no revela sesiones pasadas.
- **Resistencia a ataques offline:** Un atacante que intercepte la comunicación NFC no puede derivar el CAN (protegido por Generic Mapping + ECDH).
- **Protección contra relay:** El atacante necesita proximidad física (≤4cm NFC) Y conocer el CAN de 6 dígitos impreso en la tarjeta.

**Curva utilizada:** BrainpoolP256r1 (BSI TR-03110), diseñada específicamente para resistir posibles debilidades en curvas NIST.

### 4.2. Secure Messaging (SM)

**Propósito:** Proteger TODAS las comunicaciones posteriores a PACE contra escucha y manipulación.

**Mecanismo:**
- **Confidencialidad:** AES-128-CBC con IV derivado del SSC (Send Sequence Counter).
- **Integridad:** AES-CMAC sobre la cabecera APDU + datos cifrados.
- **Anti-replay:** SSC se incrementa monótonamente en cada comando/respuesta.
- **Separación de claves:** K_Enc ≠ K_Mac (derivadas con contadores distintos).

**Implicación:** Incluso si un atacante posiciona una antena NFC de mayor alcance, los datos transmitidos son indistinguibles de ruido sin las claves de sesión.

### 4.3. Validación de Integridad (EF.SOD)

**Propósito:** Garantizar que los datos leídos del chip no han sido alterados (clonación o manipulación).

**Mecanismo:**
- El SOD contiene un objeto CMS (PKCS#7) firmado por la autoridad emisora (DGP/FNMT).
- Dentro del SignedData se almacenan los hashes SHA-256 de cada Data Group.
- La app calcula el hash del DG leído y lo compara con el esperado.

**Limitación en ColeHop:** Se validan los hashes pero **no se verifica la firma del certificado emisor** contra una CA raíz de confianza (CSCA española). Esto significa que se detecta corrupción accidental, pero un atacante sofisticado que controle el chip podría generar un SOD falso consistente. En el SDK de la FNMT, esta validación podría estar internalizada.

### 4.4. Modelo de Amenazas

| Amenaza | Mitigación en ColeHop | Mitigación en SDK FNMT |
|---------|----------------------|------------------------|
| **Sniffing NFC** | PACE + SM (cifrado AES-128) | Igual (interno al SDK) |
| **Relay attack** | Requiere CAN + proximidad física | Igual |
| **Clonación de chip** | Validación SOD (hashes) | No visible |
| **Brute force CAN** | 6 dígitos = 10⁶ combinaciones; chip bloquea tras intentos | Igual |
| **Man-in-the-middle** | Autenticación mutua PACE | Igual (interno) |
| **Manipulación datos en tránsito** | CMAC en cada APDU | Igual (interno) |
| **Chip falso con SOD falsificado** | ⚠️ No verifica firma CSCA | Posiblemente verificada internamente |

### 4.5. Diferencia Clave de Seguridad

La principal diferencia desde el punto de vista de ciberseguridad es:

1. **Transparencia vs. Caja negra:** ColeHop implementa todo el protocolo de forma auditable. El SDK FNMT es una caja negra (`Loader.init()`) donde no se puede verificar qué validaciones se realizan internamente.

2. **Verificación de cadena de certificados:** El SDK probablemente valida la firma del SOD contra la CSCA española (Certificate Signing Certificate Authority). ColeHop valida integridad (hashes) pero no autenticidad completa de la cadena PKI.

3. **Gestión del CAN en memoria:** ColeHop pasa el CAN como `string` y lo convierte a bytes para derivar K_pi, pero no implementa limpieza explícita de memoria (`SecureString` o pinning). El SDK Java tiene el mismo problema inherente (String inmutable en JVM).

### 4.6. Passive Authentication vs. Active Authentication

Ambas implementaciones utilizan **Passive Authentication** (verificación de hashes/firma del SOD). Ninguna de las dos muestras implementa **Active Authentication** (challenge-response con clave privada del chip), que es el mecanismo definitivo anti-clonación definido en ICAO 9303.

El protocolo PACE en sí mismo proporciona una forma de Active Authentication implícita: el chip demuestra que posee las claves derivadas, lo cual es imposible de replicar sin el chip físico original.

## 5. Conclusión

ColeHop implementa una lectura NFC del DNIe 3.0 **funcionalmente equivalente** al ejemplo del SDK de la Policía Nacional, pero a nivel bajo. La app:
- Implementa PACE completo (BSI TR-03110) manualmente
- Cifra toda la comunicación post-PACE con Secure Messaging
- Valida integridad mediante SOD
- Extrae identidad del MRZ (DG1) y foto (DG2)

El SDK simplifica todo esto en una sola llamada (`Loader.init()`), ocultando la complejidad criptográfica pero también reduciendo la auditabilidad del proceso de seguridad.

Desde la perspectiva de un TFM en ciberseguridad, la implementación propia de ColeHop demuestra comprensión profunda del protocolo y permite analizar cada fase del proceso de autenticación y lectura segura.

---

## 6. Investigación de Fuentes Oficiales y Documentación Técnica

### 6.1. Portal del DNI Electrónico — Policía Nacional de España (PortalDNIe)

**Fuente:** https://www.dnielectronico.es/PortalDNIe/

La Dirección General de la Policía (DGP), a través de su portal oficial del DNI electrónico, documenta las características técnicas y funcionales del DNIe 3.0 (y 4.0). La información clave extraída:

- **Chip dual-interface:** El DNIe 3.0 incorpora un microcontrolador con interfaz de contacto (ISO 7816-3) e inalámbrica (ISO 14443 tipo A/B), accesible vía NFC en dispositivos móviles Android e iOS.
- **CAN (Card Access Number):** Número de 6 dígitos impreso en el anverso del documento (esquina inferior derecha). Actúa como secreto compartido para el protocolo PACE, evitando lecturas no autorizadas por proximidad casual. El portal enfatiza que **no es un PIN** y su conocimiento solo permite acceder a datos públicos del eMRTD.
- **Zonas de acceso diferenciadas:**
  - **Sin PIN (solo CAN vía PACE):** Datos biométricos del eMRTD → DG1 (MRZ), DG2 (foto facial), DG7 (firma manuscrita), DG11 (datos adicionales), DG13 (datos opcionales).
  - **Con PIN:** Certificados X.509 de autenticación y firma electrónica, operaciones criptográficas (firma digital).
- **Estructura ICAO:** El chip implementa la aplicación eMRTD conforme a ICAO Doc 9303, seleccionable por AID `A0000002471001`.
- **Compatibilidad DNIe 3.0 / 4.0:** El portal indica que el DNIe 4.0 mantiene retrocompatibilidad con la interfaz NFC del 3.0, usando los mismos protocolos PACE y Secure Messaging.

### 6.2. Documento "Implementación NFC" de la FNMT

**Fuente:** https://www.dnielectronico.es/PDFs/Implementacion_NFC_FNMT.pdf

Este documento técnico publicado por la FNMT en el portal oficial de la Policía Nacional describe la arquitectura de integración NFC para aplicaciones móviles. Aspectos documentados:

- **Arquitectura de capas:**
  ```
  ┌─────────────────────────────────────┐
  │  Aplicación (UI)                     │
  ├─────────────────────────────────────┤
  │  SDK DNIeDroid (dniedroid-release.aar)│
  ├─────────────────────────────────────┤
  │  jmulticard (gestión de tarjeta)     │
  ├─────────────────────────────────────┤
  │  androsmex (PACE + Secure Messaging) │
  ├─────────────────────────────────────┤
  │  Android NFC Stack (IsoDep)          │
  └─────────────────────────────────────┘
  ```
- **Flujo de inicialización requerido:**
  1. Configurar `NfcAdapter` en modo Reader (`enableReaderMode`) con flags `FLAG_READER_NFC_A | FLAG_READER_NFC_B | FLAG_READER_SKIP_NDEF_CHECK`.
  2. Configurar `EXTRA_READER_PRESENCE_CHECK_DELAY` ≥ 1000ms para evitar desconexiones durante operaciones criptográficas largas.
  3. En `onTagDiscovered(Tag)`, invocar `Loader.init(passwords, tag)` que internamente ejecuta PACE.
  4. Acceder a datos mediante `getMrtdCardInfo()` o al KeyStore para firma.

- **Requisito de timeout extendido:** El documento enfatiza que las operaciones PACE + lectura de DG2 (foto) pueden tardar 3-8 segundos. Si el `presence_check_delay` es demasiado bajo, el sistema operativo Android puede cortar la conexión NFC creyendo que el tag se ha alejado.

### 6.3. Manual de Usuario del SDK DNIeDroid (droidfnmt_user_manual.pdf)

**Fuente:** Archivo `droidfnmt_user_manual.pdf` incluido en el SDK v2.3.111 (descargado desde el portal oficial).

El manual documenta la API del SDK y su arquitectura interna. Hallazgos relevantes del análisis:

#### 6.3.1. Estructura del SDK

| Componente | Paquete | Función |
|-----------|---------|---------|
| **Loader** | `es.gob.fnmt.dniedroid.help` | Punto de entrada principal. `init(passwords, tag)` establece PACE y carga el KeyStore. |
| **PasswordUI** | `es.gob.fnmt.dniedroid.gui` | Diálogos de solicitud de PIN/CAN con UI predefinida. |
| **DnieProvider** | `es.gob.jmulticard.jse.provider` | JCA Security Provider que encapsula las operaciones del DNIe como un KeyStore Java estándar. |
| **MrtdCard** | `es.gob.jmulticard.card.baseCard.mrtd` | Interfaz para acceso a Data Groups del eMRTD. |
| **PaceECDH** | `de.tsenger.androsmex.pace` | Implementación del protocolo PACE con ECDH (Generic Mapping). |
| **EF_SOD** | `de.tsenger.androsmex.mrtd` | Parseo del Document Security Object. |
| **KeyDerivationFunction** | `de.tsenger.androsmex.pace` | KDF según BSI TR-03110 (SHA-1 para AES-128). |

#### 6.3.2. API de Lectura de Datos (MrtdCard)

El SDK expone los siguientes métodos para acceso a Data Groups:

```java
MrtdCard mrtdCardInfo = Loader.init(new String[]{can}, tag).getMrtdCardInfo();

// Data Groups disponibles:
DG1_Dnie dg1 = mrtdCardInfo.getDataGroup1();   // MRZ + datos personales
DG2       dg2 = mrtdCardInfo.getDataGroup2();   // Imagen facial (JPEG2000)
DG7       dg7 = mrtdCardInfo.getDataGroup7();   // Firma manuscrita digitalizada
DG11     dg11 = mrtdCardInfo.getDataGroup11();  // Datos adicionales del titular
DG13     dg13 = mrtdCardInfo.getDataGroup13();  // Datos opcionales (España)
```

**Comparación con ColeHop:** Nuestra app solo lee DG1 y DG2, que son los necesarios para identificación. El SDK permite además acceder a DG7 (firma manuscrita), DG11 y DG13, que contienen datos extendidos del titular español no presentes en el estándar ICAO base.

#### 6.3.3. Verificación de Edad (Age Verification)

El SDK incluye una funcionalidad de verificación de edad que opera **sin revelar la fecha de nacimiento exacta** al terminal:

```java
DnieLoadParameter initInfo = DnieLoadParameter.getBuilder(new String[]{can}, tag).build();
KeyStore keyStore = KeyStore.getInstance(DnieProvider.KEYSTORE_PROVIDER_NAME);
keyStore.load(initInfo);

boolean isOlderThan18 = DnieProvider.verifyAge(targetDate);
```

Este mecanismo utiliza una prueba de conocimiento cero (Zero-Knowledge Proof) a nivel del chip: el chip calcula internamente si la fecha de nacimiento supera el umbral solicitado y devuelve solo un booleano, sin exponer el dato real. Esto es un ejemplo de **privacidad por diseño** que ColeHop no implementa (lee la fecha completa del MRZ).

#### 6.3.4. Firma Digital con el SDK

El SDK registra un JCA Provider (`DnieProvider`) que permite usar el DNIe como almacén de claves Java estándar:

```java
Security.insertProviderAt(new DnieProvider(), 1);
Signature sig = Signature.getInstance("SHA256withRSA", new DnieProvider());
sig.initSign(privateKey);  // Requiere PIN
sig.update(data);
byte[] signature = sig.sign();  // La operación RSA se ejecuta DENTRO del chip
```

**Aspecto de seguridad clave:** La clave privada RSA **nunca sale del chip**. La operación `sign()` envía el hash al chip vía Secure Messaging, el chip firma internamente, y devuelve solo la firma. Esto garantiza no-exportabilidad del material criptográfico.

### 6.4. Manual DNIe Remote

**Fuente:** https://www.dnielectronico.es/descargas/Apps/manual_DNIeRemote.html

DNIe Remote es la aplicación oficial de la Policía Nacional que permite usar el DNIe a través del móvil como lector NFC remoto para un PC. Documentación relevante:

- **Protocolo de comunicación:** El móvil actúa como "pasarela NFC" entre el PC (que ejecuta el middleware PKCS#11) y el chip del DNIe. La comunicación PC↔Móvil se realiza por red local (WiFi/USB).
- **Canal seguro extremo a extremo:** El canal PACE se establece directamente entre el software del PC y el chip, pasando por el móvil de forma transparente (relay autorizado). Los APDUs protegidos por SM viajan cifrados — el móvil no puede leer su contenido.
- **Implicación para ColeHop:** Nuestra app ejecuta PACE directamente en el dispositivo (sin relay). Es el modelo más simple y seguro, ya que no hay intermediarios en la cadena de comunicación.

### 6.5. BSI TR-03110 — Advanced Security Mechanisms for Machine Readable Travel Documents

**Fuente:** Bundesamt für Sicherheit in der Informationstechnik (BSI)  
**URL:** https://www.bsi.bund.de/EN/Themen/Unternehmen-und-Organisationen/Standards-und-Zertifizierung/Technische-Richtlinien/TR-nach-Thema-sortiert/tr03110/tr03110_node.html

Este es el estándar técnico que define PACE y que el DNIe 3.0 implementa. El SDK utiliza la librería `androsmex` (paquete `de.tsenger.androsmex.pace`) que implementa este estándar. Estructura detallada del protocolo tal como se implementa en ambos sistemas:

#### Fase 1: Lectura de EF.CardAccess
Antes de iniciar PACE, se lee el fichero `EF.CardAccess` (FID 011C) que contiene un ASN.1 SecurityInfos con:
- OID del algoritmo PACE soportado por la tarjeta
- ID de los parámetros de dominio estandarizados
- Versión del protocolo

Tanto ColeHop como el SDK (vía `PaceInfo` en androsmex) leen este fichero para descubrir las capacidades del chip.

#### Fase 2: Derivación de clave a partir del CAN
```
K_π = KDF(CAN, 3)  →  SHA-1(CAN_bytes || 00000003)[0:15]  (para AES-128)
```
El contador `3` indica "clave de descifrado de nonce" según la tabla A.1 del estándar. En androsmex esto se implementa en `KeyDerivationFunction` con la variante `KDF_AES`.

#### Fase 3: MSE:Set AT
El terminal envía al chip la selección del algoritmo criptográfico. En androsmex se construye mediante `MSESetAT`:
- **OID:** `id-PACE-ECDH-GM-AES-CBC-CMAC-128` (0.4.0.127.0.7.2.2.4.2.2)
- **Referencia de contraseña:** `PACE_PASSWORD.CAN` (valor 0x02)
- **Parámetros de dominio:** `DOMAIN_REFERENCE` 0x0D (brainpoolP256r1, ID estandarizado 13)

#### Fase 4: General Authenticate (4 intercambios)
Implementado en `PaceECDH` (androsmex) y `PaceSession` (ColeHop):

1. **Paso 1 — Nonce cifrado:** El chip genera nonce `s` y lo envía cifrado con AES-CBC(K_π, IV=0). El terminal descifra.
2. **Paso 2 — Generic Mapping:** Intercambio ECDH sobre curva original. El generador mapeado se calcula como `G' = s·G + H` donde `H = d_term · Q_chip` (punto ECDH compartido completo, no solo coordenada X).
3. **Paso 3 — Intercambio efímero:** Nuevas claves ECDH sobre el dominio con generador mapeado G'.
4. **Paso 4 — Autenticación mutua:** Cada parte calcula CMAC(K_Mac, AuthToken) sobre el punto público ajeno, envuelto en estructura `7F49[06 OID, 86 PK]`. Se verifica la MAC recibida.

#### Derivación de claves de sesión
```
K_Enc = KDF(SharedSecret, 1)  →  SHA-1(SS || 00000001)[0:15]
K_Mac = KDF(SharedSecret, 2)  →  SHA-1(SS || 00000002)[0:15]
SSC = 0x00...00 (16 bytes)     // Para AES, inicializado a cero
```

### 6.6. ICAO Doc 9303 — Machine Readable Travel Documents

**Fuente:** Organización de Aviación Civil Internacional  
**URL:** https://www.icao.int/publications/pages/publication.aspx?docnum=9303

Define la estructura de datos del eMRTD que el DNIe implementa. La clase `EF_SOD` del SDK (androsmex) parsea la estructura definida aquí:

| Data Group | Contenido | Tag | FID | SDK Class |
|-----------|-----------|-----|-----|-----------|
| DG1 | MRZ (Machine Readable Zone) | 61 → 5F1F | 0101 | `DG1_Dnie` |
| DG2 | Imagen facial (JPEG2000) | 75 | 0102 | `DG2` |
| DG7 | Firma manuscrita | 02 | 0107 | `DG7` |
| DG11 | Datos adicionales | 6B | 010B | `DG11` |
| DG13 | Datos opcionales (España) | 6D | 010D | `DG13` |
| DG14 | Security Infos (Chip Auth) | 6E | 010E | `DG14` |
| SOD | Document Security Object | 77 | 011D | `EF_SOD` |
| COM | Mapa de Data Groups presentes | 60 | 011E | `EF_COM` |

La clase `EF_SOD.DGHashInfo` del SDK encapsula los hashes de cada DG extraídos del LDSSecurityObject, proporcionando un método para verificar integridad.

#### Formato MRZ TD1 (DNI español)
El DNIe usa formato TD1 de ICAO (tarjeta ID, 3 líneas × 30 caracteres):
```
Línea 1: I<ESP[NºDoc:9][Check:1][Datos opcionales:15]
Línea 2: [FechaNac:6][Check:1][Sexo:1][FechaExp:6][Check:1][Nacionalidad:3][Opcional:11][CheckGlobal:1]
Línea 3: [APELLIDO1<APELLIDO2<<NOMBRE<<<...]
```

La clase `DG1_Dnie` del SDK extiende `DG1` genérico de ICAO con campos específicos del DNI español (número de soporte, segundo apellido separado, etc.).

### 6.7. Passive Authentication (PA) — Verificación de Integridad del Documento

Mecanismo definido en ICAO 9303 Part 11 e implementado internamente por `EF_SOD` en el SDK:

1. **EF.SOD** contiene un objeto CMS SignedData (RFC 5652) que incluye:
   - `digestAlgorithms`: SHA-256 (OID 2.16.840.1.101.3.4.2.1) en DNIe 3.0
   - `encapContentInfo`: LDSSecurityObject con los hashes de cada DG
   - `certificates`: certificado X.509 del Document Signer (DS) emitido por FNMT
   - `signerInfos`: firma RSA-2048 o ECDSA sobre el LDSSecurityObject

2. **Cadena de confianza PKI del DNIe español:**
   ```
   CSCA España (Country Signing CA — DGP)
     │  Certificado raíz publicado en ICAO PKD (Public Key Directory)
     │  Validez: ~15-20 años
     └── DS Certificate (Document Signer — FNMT-RCM)
           │  Emitido por la CSCA, validez ~3-5 años
           │  Incluido en EF.SOD de cada documento
           └── Firma RSA/ECDSA sobre LDSSecurityObject
                 └── SHA-256(DG1) || SHA-256(DG2) || ...
   ```

3. **Nivel de verificación implementado:**

   | Paso | Descripción | ColeHop | SDK FNMT |
   |------|-------------|---------|----------|
   | 1 | Calcular hash DG → comparar con LDSSecurityObject | ✅ | ✅ (interno) |
   | 2 | Verificar firma DS sobre LDSSecurityObject | ❌ | ⚠️ No confirmado |
   | 3 | Validar certificado DS contra CSCA root | ❌ | ⚠️ No confirmado |
   | 4 | Verificar revocación (CRL/OCSP) del DS | ❌ | ❌ |

   **Nota:** El protocolo PACE en sí mismo proporciona una garantía equivalente a Active Authentication para el caso de uso de ColeHop: si el chip completa PACE exitosamente, es un chip genuino que posee el secreto original programado durante la personalización. Un clon no podría reproducir esto.

### 6.8. Aplicación DNIe Remote — Modelo de Relay Autorizado

**Fuente:** https://www.dnielectronico.es/descargas/Apps/manual_DNIeRemote.html

La aplicación oficial de la Policía Nacional implementa un **relay NFC autorizado** que permite usar el DNIe desde un PC a través del móvil:

```
┌──────────┐     WiFi/USB      ┌──────────┐       NFC        ┌──────────┐
│    PC    │ ◄──────────────► │  Móvil   │ ◄──────────────► │  DNIe    │
│(PKCS#11) │  APDUs cifrados   │(Relay)   │   ISO 14443      │  (Chip)  │
└──────────┘                   └──────────┘                   └──────────┘
```

**Análisis de seguridad del relay:**
- El móvil actúa como puente transparente — no descifra los APDUs de Secure Messaging.
- El canal PACE se establece extremo a extremo (PC ↔ Chip), por lo que comprometer el móvil no expone datos.
- Este diseño demuestra que el modelo de amenazas del DNIe contempla intermediarios no confiables en la ruta de comunicación.

**Diferencia con ColeHop:** Nuestra app es terminal final (no relay). Ejecuta PACE localmente y procesa los datos en el propio dispositivo. Esto elimina la superficie de ataque del canal PC↔Móvil.

### 6.9. Arquitectura Interna del SDK: androsmex + jmulticard

Del análisis del JavaDoc del SDK (versión 2.3.111), se puede reconstruir la arquitectura criptográfica interna:

#### Capa androsmex (PACE y comunicación con chip)
```
de.tsenger.androsmex
├── pace/
│   ├── Pace.java              — Interfaz base del protocolo
│   ├── PaceECDH.java          — PACE con ECDH Generic Mapping (usado por DNIe 3.0)
│   ├── PaceDH.java            — PACE con DH clásico (no usado en DNIe)
│   ├── PaceOperator.java      — Orquestador del flujo PACE completo
│   └── KeyDerivationFunction  — KDF según BSI TR-03110 (SHA-1/AES-128)
├── crypto/
│   ├── AmAESCrypto.java       — AES-CBC cifrado/descifrado
│   └── AmDESCrypto.java       — 3DES (para compatibilidad BAC)
├── iso7816/
│   ├── CardCommands.java      — SELECT, READ BINARY, GET CHALLENGE
│   └── command/
│       ├── MSESetAT.java      — Construcción del comando MSE:Set AT
│       └── GeneralAuthenticate.java — Pasos 1-4 de PACE
├── asn1/
│   ├── PaceInfo.java          — Parseo de EF.CardAccess
│   ├── SecurityInfos.java     — Contenedor ASN.1 de SecurityInfo
│   └── PaceDomainParameterInfo — Parámetros de curva
└── mrtd/
    ├── DG1_Dnie.java          — Parseo MRZ específico DNI español
    ├── DG2.java               — Extracción imagen JPEG2000
    ├── EF_SOD.java            — Parseo CMS del SOD
    └── EF_COM.java            — Mapa de DGs presentes
```

#### Capa jmulticard (gestión de tarjeta inteligente)
```
es.gob.jmulticard
├── card/baseCard/
│   ├── dnie/
│   │   ├── Dnie.java         — Gestión general del DNIe (PIN, certificados)
│   │   └── mrtd/DnieMrtd.java — Acceso específico a datos MRTD
│   └── mrtd/MrtdCard.java    — Interfaz unificada de lectura de DGs
├── apdu/connection/
│   ├── cwa14890/             — Canal seguro CWA-14890 (contacto)
│   └── secure/               — Secure Messaging genérico
└── jse/provider/
    └── DnieProvider.java      — JCA Provider (KeyStore + Signature)
```

**Observación clave:** El SDK separa claramente la capa de protocolo PACE (androsmex, desarrollado por A. Tsenger) de la capa de gestión de tarjeta española (jmulticard, desarrollado por el CTT del Gobierno de España). ColeHop unifica ambas capas en una implementación monolítica en C#.

### 6.10. Librería firmada digitalmente por CNP-FNMT

El fichero `leeme_11052023.txt` del SDK documenta que la librería `dniedroid-release.aar` está **firmada digitalmente** por la entidad "CNP-FNMT" (Cuerpo Nacional de Policía / FNMT):

```
Signed by "CN=CNP-FNMT, OU=DNI electronico, O=CNP-FNMT, L=Madrid, ST=Madrid, C=34"
    Digest algorithm: SHA-256
    Signature algorithm: SHA256withRSA, 2048-bit key
```

Esto proporciona una garantía de integridad y autenticidad de la librería. Un desarrollador puede verificarla con `jarsigner -verify`. En el contexto de seguridad, esto previene ataques de supply-chain donde un atacante sustituya el SDK por una versión maliciosa.

### 6.11. Gestión NFC: enableReaderMode vs. Foreground Dispatch

El SDK usa `enableReaderMode` (según `Common.java`):
```java
nfcAdapter.enableReaderMode(activity, callback,
    NfcAdapter.FLAG_READER_NFC_A | FLAG_READER_NFC_B | FLAG_READER_SKIP_NDEF_CHECK,
    options);  // PRESENCE_CHECK_DELAY = 1000ms
```

ColeHop usa **Foreground Dispatch** con filtros IntentFilter para tecnología IsoDep:

| Aspecto | enableReaderMode (SDK) | Foreground Dispatch (ColeHop) |
|---------|----------------------|-------------------------------|
| Control | Callback directo `onTagDiscovered` | Intent → Activity → procesado |
| Prioridad | Máxima (bloquea otros lectores) | Alta (foreground, pero basado en Intent) |
| Presence check | Configurable vía Bundle | No configurable directamente |
| Compatibilidad | API 19+ | API 10+ |
| Multi-tech | Explícito por flags | Implícito por IntentFilter |

Ambos enfoques son válidos. El SDK prefiere `enableReaderMode` por su mayor control sobre el timing del presence check, crítico para evitar desconexiones durante PACE.

### 6.12. Diferencia entre BAC y PACE — Justificación del DNIe 3.0

El DNIe 3.0 utiliza **PACE** (no BAC) por razones de seguridad documentadas tanto en BSI TR-03110 como en las guías del portal DNIe:

| Característica | BAC (pasaportes / DNIe 2.0) | PACE (DNIe 3.0/4.0) |
|---------------|----------------------------|---------------------|
| Secreto compartido | MRZ (fecha nac + exp + nº doc) | CAN de 6 dígitos |
| Derivación de clave | 3DES-112 (efectivo ~80 bits) | AES-128 |
| Intercambio de claves | No (clave directa de MRZ) | ECDH con Generic Mapping |
| Perfect Forward Secrecy | ❌ No | ✅ Sí |
| Resistencia offline | Baja (~56 bits entropía MRZ) | Alta (ECDH efímero) |
| Autenticación mutua | No explícita | ✅ CMAC cruzado |
| Estándar | ICAO 9303 Supp. (2006) | BSI TR-03110 v2.1+ (2012) |

BAC es vulnerable porque la MRZ es predecible (fecha de nacimiento limitada, número de documento secuencial). Un atacante que capture la comunicación NFC puede intentar fuerza bruta offline. PACE elimina este vector: aunque el CAN solo tiene 10⁶ combinaciones, el nonce aleatorio del chip + ECDH efímero hacen que cada sesión sea criptográficamente única e irreproducible sin acceso físico al chip.

### 6.13. Guía de Seguridad del CCN (Centro Criptológico Nacional)

**Fuente:** CCN-CERT (https://www.ccn-cert.cni.es)

El Centro Criptológico Nacional ha publicado guías relevantes:

- **CCN-STIC-807 "Criptología de empleo en el ENS":** Define los algoritmos criptográficos aprobados para sistemas clasificados en el Esquema Nacional de Seguridad. El DNIe 3.0 cumple con nivel ALTO.
- **Certificación Common Criteria:** El chip del DNIe 3.0 está certificado:
  - Hardware: EAL4+ (Infineon SLE78)
  - Sistema operativo del chip: EAL5+ (DNIe OS)
  - Aplicación eMRTD: conforme a Protection Profile BSI-PP-0056
- **Algoritmos aprobados en el chip:**
  - Cifrado simétrico: AES-128, AES-256
  - Hash: SHA-256, SHA-384, SHA-512
  - Acuerdo de claves: ECDH sobre BrainpoolP256r1, P384r1, P512r1
  - Firma: RSA-2048, ECDSA
  - MAC: AES-CMAC-128

---

## 7. Referencias Bibliográficas y Fuentes Consultadas

### Fuentes Oficiales Españolas

1. **Policía Nacional de España — Portal del DNI Electrónico.** "DNIe 3.0: Características técnicas y funcionales." Disponible en: https://www.dnielectronico.es/PortalDNIe/ [Consultado: 2025]
2. **FNMT-RCM.** "Implementación NFC para DNIe — Guía de integración." Disponible en: https://www.dnielectronico.es/PDFs/Implementacion_NFC_FNMT.pdf [Consultado: 2025]
3. **Policía Nacional.** "Manual DNIe Remote — Uso del DNIe con dispositivos móviles." Disponible en: https://www.dnielectronico.es/descargas/Apps/manual_DNIeRemote.html [Consultado: 2025]
4. **FNMT-RCM.** "SDK DNIeDroid v2.3.111 — Manual de usuario (droidfnmt_user_manual.pdf)." Incluido en el paquete SDK descargable. Fecha del paquete: 11/05/2023.
5. **Gobierno de España — CTT (Centro de Transferencia de Tecnología).** "jmulticard — Biblioteca Java de acceso a tarjetas inteligentes." Código fuente disponible en: https://github.com/ctt-gob-es/jmulticard
6. **CCN-CERT — Centro Criptológico Nacional.** "CCN-STIC-807: Criptología de empleo en el Esquema Nacional de Seguridad." Disponible en: https://www.ccn-cert.cni.es

### Estándares Internacionales

7. **BSI (Bundesamt für Sicherheit in der Informationstechnik).** "Technical Guideline TR-03110: Advanced Security Mechanisms for Machine Readable Travel Documents and eIDAS Token." Version 2.20, Part 1-4. Bonn, 2015. Disponible en: https://www.bsi.bund.de/EN/Themen/Unternehmen-und-Organisationen/Standards-und-Zertifizierung/Technische-Richtlinien/TR-nach-Thema-sortiert/tr03110/tr03110_node.html
8. **ICAO (International Civil Aviation Organization).** "Doc 9303 — Machine Readable Travel Documents." Eighth Edition, Parts 1-13. Montreal, 2021. Disponible en: https://www.icao.int/publications/pages/publication.aspx?docnum=9303
9. **ISO/IEC 14443.** "Identification cards — Contactless integrated circuit cards — Proximity cards." Parts 1-4. Define la capa física NFC utilizada por el DNIe.
10. **ISO/IEC 7816.** "Identification cards — Integrated circuit cards." Parts 1-15. Define los comandos APDU (SELECT, READ BINARY, etc.).

### Normativa Europea

11. **Unión Europea.** "Reglamento (UE) 2019/1157 del Parlamento Europeo y del Consejo, de 20 de junio de 2019, sobre el refuerzo de la seguridad de los documentos de identidad de los ciudadanos de la Unión." Obliga al chip NFC con datos biométricos desde agosto 2021. DOUE L 188/67.
12. **ENISA (European Union Agency for Cybersecurity).** "Security of eID and eIDAS" — Recomendaciones sobre niveles de aseguramiento (LoA) para documentos electrónicos de identidad.

### Librería androsmex

13. **Tsenger, A.** "AndroSMEX — Android Secure Messaging Extension." Biblioteca Java que implementa PACE (BSI TR-03110), Secure Messaging (ISO 7816-4 SM), y parseo de estructuras eMRTD. Utilizada internamente por el SDK DNIeDroid de la FNMT. Paquete: `de.tsenger.androsmex`.

### Análisis Técnico Propio

14. **Análisis del código fuente del SDK DNIeDroid v2.3.111.** Incluye JavaDoc completo de las clases `PaceECDH`, `KeyDerivationFunction`, `EF_SOD`, `MrtdCard`, `DG1_Dnie`, `DG2`, `Loader`, y `DnieProvider`. Revisión de la aplicación de ejemplo `Sample_DNIe_App` con los módulos: `datareader`, `ageverification`, `signature`, y `network`.
15. **Verificación de firma del SDK:** La librería `dniedroid-release.aar` está firmada con certificado "CN=CNP-FNMT, OU=DNI electronico, O=CNP-FNMT, L=Madrid, ST=Madrid, C=34" usando SHA256withRSA (2048-bit key), verificable con `jarsigner -verify`.
