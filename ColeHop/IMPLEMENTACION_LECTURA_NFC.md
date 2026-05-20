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

## 6. Documentación Oficial y Estándares de Referencia

### 6.1. Portal del DNI Electrónico — Policía Nacional de España

**Fuente:** https://www.dnielectronico.es

La Dirección General de la Policía (DGP) publica en este portal la especificación funcional del DNIe 3.0. Puntos clave documentados:

- **Chip contactless:** El DNIe 3.0 incorpora un chip dual-interface (contacto ISO 7816 + sin contacto ISO 14443) accesible vía NFC en dispositivos móviles.
- **CAN (Card Access Number):** Número de 6 dígitos impreso en el anverso (esquina inferior derecha) que actúa como contraseña de acceso al canal PACE. Su propósito es evitar lecturas no autorizadas por proximidad casual.
- **Datos accesibles por NFC (sin PIN):** Solo los Data Groups del eMRTD (DG1: MRZ, DG2: fotografía facial) son accesibles tras PACE con el CAN. Los certificados de firma y autenticación requieren PIN del titular.
- **Estructura de ficheros:** Sigue la especificación ICAO 9303 para documentos de viaje electrónicos (eMRTD), con la aplicación eMRTD seleccionable por AID `A0000002471001`.

### 6.2. BSI TR-03110 — Advanced Security Mechanisms for Machine Readable Travel Documents

**Fuente:** Bundesamt für Sicherheit in der Informationstechnik (BSI)  
**URL:** https://www.bsi.bund.de/EN/Themen/Unternehmen-und-Organisationen/Standards-und-Zertifizierung/Technische-Richtlinien/TR-nach-Thema-sortiert/tr03110/tr03110_node.html

Este es el estándar técnico que define PACE y que el DNIe 3.0 implementa. Estructura del protocolo:

#### Fase 1: Derivación de clave a partir del CAN
```
K_π = KDF(CAN, 3)  →  SHA-1(CAN_bytes || 00000003)[0:15]  (para AES-128)
```
El contador `3` indica "clave de descifrado de nonce" según la tabla A.1 del estándar.

#### Fase 2: MSE:Set AT
El terminal envía al chip la selección del algoritmo criptográfico:
- **OID:** `id-PACE-ECDH-GM-AES-CBC-CMAC-128` (0.4.0.127.0.7.2.2.4.2.2)
- **Referencia de contraseña:** `02` (CAN)
- **Parámetros de dominio:** `0D` (brainpoolP256r1, ID estandarizado 13)

#### Fase 3: General Authenticate (4 pasos)
1. **Paso 1:** El chip genera un nonce aleatorio `s`, lo cifra con K_π y lo envía.
2. **Paso 2 (Generic Mapping):** Terminal y chip intercambian puntos ECDH sobre la curva original. El generador mapeado es `G' = s·G + H` donde H es el punto compartido ECDH.
3. **Paso 3:** Intercambio de claves efímeras sobre el dominio mapeado G'.
4. **Paso 4 (Mutual Authentication):** Ambas partes calculan MAC (AES-CMAC truncado a 8 bytes) sobre el punto público de la otra parte. La verificación cruzada demuestra conocimiento del CAN sin revelarlo.

#### Derivación de claves de sesión
```
K_Enc = KDF(SharedSecret, 1)  →  SHA-1(SS || 00000001)[0:15]
K_Mac = KDF(SharedSecret, 2)  →  SHA-1(SS || 00000002)[0:15]
```

### 6.3. ICAO Doc 9303 — Machine Readable Travel Documents

**Fuente:** Organización de Aviación Civil Internacional  
**URL:** https://www.icao.int/publications/pages/publication.aspx?docnum=9303

Define la estructura de datos del eMRTD que el DNIe implementa:

| Data Group | Contenido | Tag | FID |
|-----------|-----------|-----|-----|
| DG1 | MRZ (Machine Readable Zone) | 61 → 5F1F | 0101 |
| DG2 | Imagen facial (JPEG/JP2) | 75 | 0102 |
| DG3 | Huellas dactilares (solo BAC+AA) | 63 | 0103 |
| DG14 | Security Infos (Chip Authentication) | 6E | 010E |
| DG15 | Active Authentication Public Key | 6F | 010F |
| SOD | Document Security Object (CMS/PKCS#7) | 77 | 011D |

#### Formato MRZ TD1 (DNI español)
El DNIe usa formato TD1 de ICAO (tarjeta ID, 3 líneas × 30 caracteres):
```
Línea 1: [Tipo:2][País:3][Nº Documento:9][Check:1][Opcional:15]
Línea 2: [FechaNac:6][Check:1][Sexo:1][FechaExp:6][Check:1][Nacionalidad:3][Opcional:11][CheckGlobal:1]
Línea 3: [Apellidos<<Nombre<<<<<<<<<<<<...]
```

### 6.4. Passive Authentication (PA) — ICAO 9303 Part 11

Mecanismo de validación de integridad y autenticidad:

1. **EF.SOD** contiene un objeto CMS SignedData que incluye:
   - `digestAlgorithms`: algoritmos de hash usados (SHA-256 en DNIe 3.0)
   - `encapContentInfo`: LDSSecurityObject con los hashes de cada DG
   - `certificates`: certificado del Document Signer (DS)
   - `signerInfos`: firma RSA/ECDSA sobre el LDSSecurityObject

2. **Cadena de confianza:**
   ```
   CSCA (Country Signing CA, DGP España)
     └── DS Certificate (Document Signer, FNMT)
           └── Firma sobre LDSSecurityObject
                 └── Hashes de DG1, DG2, etc.
   ```

3. **Verificación completa:**
   - Calcular hash de cada DG leído → comparar con hash en LDSSecurityObject ✓ (ColeHop lo hace)
   - Verificar firma del DS sobre LDSSecurityObject ⚠️ (ColeHop no lo implementa)
   - Validar certificado DS contra CSCA root ⚠️ (ColeHop no lo implementa)
   - Verificar CRL/OCSP del certificado DS ⚠️ (no implementado)

### 6.5. FNMT — Fábrica Nacional de Moneda y Timbre

**Fuente:** https://www.sede.fnmt.gob.es/certificados/certificado-electronico

La FNMT actúa como prestador de servicios de certificación del DNIe:
- Emite los certificados de autenticación y firma digital contenidos en el chip.
- Publica el SDK `DNIeDroid` para desarrolladores Android (Java), que encapsula toda la lógica criptográfica mediante la librería `jmulticard`.
- La librería `jmulticard` es de código abierto: https://github.com/ctt-gob-es/jmulticard

### 6.6. Guía de Seguridad del CCN (Centro Criptológico Nacional)

**Fuente:** CCN-CERT (https://www.ccn-cert.cni.es)

El CCN ha publicado guías sobre el uso seguro del DNIe en las que se detalla:
- **ENS (Esquema Nacional de Seguridad):** El DNIe 3.0 cumple con nivel ALTO para autenticación de personas físicas.
- **Certificación Common Criteria:** El chip del DNIe 3.0 está certificado a nivel EAL4+ (hardware) y EAL5+ (sistema operativo del chip).
- **Algoritmos aprobados:** AES-128/256, SHA-256/384/512, ECDSA/ECDH sobre BrainpoolP256r1/P384r1/P512r1, RSA-2048+.

### 6.7. Diferencia entre BAC y PACE

El DNIe 3.0 utiliza **PACE** (no BAC) por las siguientes razones de seguridad documentadas:

| Característica | BAC (DNIe 2.0 / pasaportes antiguos) | PACE (DNIe 3.0) |
|---------------|---------------------------------------|-----------------|
| Secreto compartido | MRZ completa (fecha nac + expiración + nº doc) | CAN de 6 dígitos |
| Derivación de clave | 3DES (débil) | AES-128 |
| Diffie-Hellman | No | ECDH con Generic Mapping |
| Forward Secrecy | No | Sí |
| Resistencia offline | Baja (MRZ es predecible) | Alta (nonce aleatorio + ECDH efímero) |
| Estándar | ICAO 9303 (2006) | BSI TR-03110 v2.1+ |

BAC es vulnerable a ataques de fuerza bruta offline porque la MRZ tiene baja entropía (~40-50 bits efectivos). PACE con CAN tiene solo ~20 bits de entropía (10⁶), pero el protocolo ECDH con nonce aleatorio hace que un ataque offline sea computacionalmente infeasible sin acceso al chip.

---

## 7. Referencias Bibliográficas

1. **Policía Nacional de España.** "DNI Electrónico 3.0 — Guía de uso." https://www.dnielectronico.es
2. **BSI.** "TR-03110 Advanced Security Mechanisms for Machine Readable Travel Documents and eIDAS Token." Version 2.20, 2015.
3. **ICAO.** "Doc 9303 — Machine Readable Travel Documents." Eighth Edition, 2021.
4. **FNMT-RCM.** "SDK DNIeDroid v2.3.111 — Manual de usuario." Incluido en el SDK descargable.
5. **Gobierno de España (CTT).** "jmulticard — Biblioteca Java para tarjetas inteligentes." https://github.com/ctt-gob-es/jmulticard
6. **CCN-CERT.** "Guía de Seguridad CCN-STIC 807 — Criptología de empleo en el ENS." https://www.ccn-cert.cni.es
7. **Tsenger, A.** "AndroSMEX — Android Secure Messaging Extension." Biblioteca utilizada internamente por el SDK FNMT para PACE/SM.
8. **European Union.** "Reglamento (UE) 2019/1157 sobre el refuerzo de la seguridad de los documentos de identidad." Define la obligatoriedad de chip NFC con datos biométricos en DNIs europeos desde agosto 2021.
