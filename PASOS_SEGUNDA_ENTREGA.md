# 📋 PASOS PARA LA SEGUNDA ENTREGA DEL TFM - COLEHOP

## 🎯 Objetivo de la Segunda Entrega

La segunda entrega del TFM debe demostrar que la aplicación ColeHop ha completado su **implementación funcional core** con:
- ✅ Backend desplegado y funcional
- ✅ Base de datos implementada con persistencia real
- ✅ Lectura NFC del DNIe 3.0 completamente operativa
- ✅ Flujo completo de autorizaciones y recogidas
- ✅ Testing básico implementado
- ✅ Documentación técnica completa

---

## 📊 Estado Actual del Proyecto

Según el análisis del código existente en `ColeHop/`, el proyecto está aproximadamente al **~70% de completitud**:

### ✅ COMPLETADO (Primera Entrega)
1. **Arquitectura y estructura** - Clean Architecture implementada
2. **Interfaz de usuario** - Todas las vistas XAML con estilos modernos
3. **Navegación** - AppShell con rutas dinámicas según rol
4. **Protocolo PACE** - Implementación completa para DNIe 3.0
5. **Secure Messaging** - Cifrado AES-CBC + CMAC implementado
6. **Lectura NFC** - DnieFileReader, SodValidator, DnieIdentityExtractor funcionando
7. **Backend HTTP** - HttpAuthService, HttpPickupService, HttpTutorManagementService
8. **Alertas personalizadas** - CustomAlertPopup con iconos Material
9. **Localización** - Recursos en español e inglés
10. **Seguridad básica** - Limpieza de claves de sesión, rate limiting NFC

### 🔧 PENDIENTE (Segunda Entrega)
1. **Backend API REST** - Despliegue y configuración
2. **Base de datos** - Persistencia local con SQLite
3. **Sincronización** - Offline-first con sync al backend
4. **Testing** - Proyecto de tests unitarios e integración
5. **Documentación final** - Manual técnico, casos de uso, diagramas
6. **Validación iOS** - Implementación y pruebas en iOS

---

## 📝 PASOS DETALLADOS PARA LA SEGUNDA ENTREGA

### PASO 1: Implementar Backend API REST (PRIORIDAD MÁXIMA)

#### 1.1. Crear proyecto ASP.NET Core Web API

```bash
cd C:\Users\U362407\Samu\Desa
mkdir ColeHop.Backend
cd ColeHop.Backend
dotnet new webapi -n ColeHop.Api
cd ColeHop.Api
```

#### 1.2. Configurar dependencias del backend

Añadir paquetes NuGet necesarios:
```bash
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add package Microsoft.AspNetCore.Identity.EntityFrameworkCore
dotnet add package Swashbuckle.AspNetCore
```

#### 1.3. Implementar endpoints según especificación

**Referencia:** `ColeHop/CREACION_BACKEND.md`

Endpoints requeridos:

**Autenticación:**
- `POST /api/auth/register` - Registro de tutor
- `POST /api/auth/login` - Login (devuelve JWT + userId + role)
- `GET /api/auth/validate` - Validar token JWT

**Gestión de Tutores:**
- `GET /api/tutor/children` - Listar hijos del tutor
- `POST /api/tutor/children` - Añadir hijo
- `PUT /api/tutor/children/{childId}` - Actualizar hijo
- `DELETE /api/tutor/children/{childId}` - Eliminar hijo
- `GET /api/tutor/children/{childId}/authorized-persons` - Listar personas autorizadas
- `POST /api/tutor/children/{childId}/authorized-persons` - Añadir persona autorizada
- `PUT /api/tutor/authorized-persons/{personId}` - Actualizar persona
- `DELETE /api/tutor/authorized-persons/{personId}` - Eliminar persona
- `GET /api/tutor/authorizations` - Listar autorizaciones
- `POST /api/tutor/authorizations` - Crear autorización
- `DELETE /api/tutor/authorizations/{authorizationId}` - Cancelar autorización
- `GET /api/tutor/pickup-history` - Historial de recogidas

**Gestión de Profesores:**
- `GET /api/teacher/pending-approvals` - Solicitudes pendientes de aprobación
- `PUT /api/teacher/pending-approvals/{requestId}/approve` - Aprobar alta de tutor
- `PUT /api/teacher/pending-approvals/{requestId}/reject` - Rechazar alta
- `GET /api/teacher/daily-pickups` - Lista de recogidas del día
- `POST /api/teacher/pickup/check-authorization` - Verificar autorización
- `POST /api/teacher/pickup/confirm` - Confirmar recogida

#### 1.4. Configurar base de datos

**Archivo:** `appsettings.json`

```json
{
  "ConnectionStrings": {
	"DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=ColeHopDb;Trusted_Connection=true;MultipleActiveResultSets=true"
  },
  "Jwt": {
	"Key": "your-secret-key-min-32-characters-long",
	"Issuer": "ColeHopApi",
	"Audience": "ColeHopApp",
	"ExpirationMinutes": 1440
  }
}
```

#### 1.5. Crear DbContext y modelos

**Referencia de modelos existentes en ColeHop/Models:**
- `Authorization.cs`
- `AuthorizedPerson.cs`
- `Child.cs`
- `PickupLog.cs`
- `ApprovalStatus.cs`

#### 1.6. Configurar autenticación JWT

**Archivo:** `Program.cs`

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
	.AddJwtBearer(options =>
	{
		options.TokenValidationParameters = new TokenValidationParameters
		{
			ValidateIssuer = true,
			ValidateAudience = true,
			ValidateLifetime = true,
			ValidateIssuerSigningKey = true,
			ValidIssuer = builder.Configuration["Jwt:Issuer"],
			ValidAudience = builder.Configuration["Jwt:Audience"],
			IssuerSigningKey = new SymmetricSecurityKey(
				Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
		};
	});
```

#### 1.7. Configurar CORS para desarrollo

```csharp
builder.Services.AddCors(options =>
{
	options.AddPolicy("ColeHopMobile",
		policy => policy.AllowAnyOrigin()
						.AllowAnyMethod()
						.AllowAnyHeader());
});
```

#### 1.8. Desplegar backend

**Opciones:**
1. **Local (desarrollo):** IIS Express / Kestrel con HTTPS
2. **Azure App Service** (recomendado para TFM)
3. **Docker + Azure Container Instances**

**Configurar URL en la app móvil:**
```csharp
// ColeHop/ColeHop/Helpers/ApiConfig.cs
public const string BaseUrl = "https://tu-backend.azurewebsites.net/";
```

#### 1.9. Validar endpoints con Swagger

Acceder a `https://localhost:5001/swagger` y verificar todos los endpoints.

---

### PASO 2: Implementar Base de Datos Local (SQLite)

#### 2.1. Añadir dependencias SQLite al proyecto móvil

```bash
cd C:\Users\U362407\Samu\Desa\ColeHop\ColeHop
dotnet add package sqlite-net-pcl
dotnet add package SQLitePCLRaw.bundle_green
```

#### 2.2. Crear modelos de base de datos local

**Crear:** `ColeHop/ColeHop/Data/LocalDatabase.cs`

```csharp
using SQLite;
using ColeHop.Models;

namespace ColeHop.Data
{
	public class LocalDatabase
	{
		private readonly SQLiteAsyncConnection _database;

		public LocalDatabase(string dbPath)
		{
			_database = new SQLiteAsyncConnection(dbPath);
			_database.CreateTableAsync<Child>().Wait();
			_database.CreateTableAsync<AuthorizedPerson>().Wait();
			_database.CreateTableAsync<Authorization>().Wait();
			_database.CreateTableAsync<PickupLog>().Wait();
		}

		// Operaciones CRUD para cada entidad
		public Task<List<Child>> GetChildrenAsync() => _database.Table<Child>().ToListAsync();
		public Task<int> SaveChildAsync(Child child) => _database.InsertOrReplaceAsync(child);
		// ... más métodos
	}
}
```

#### 2.3. Registrar servicio de base de datos

**Modificar:** `ColeHop/ColeHop/MauiProgram.cs`

```csharp
string dbPath = Path.Combine(FileSystem.AppDataDirectory, "colehop.db3");
builder.Services.AddSingleton<LocalDatabase>(s => new LocalDatabase(dbPath));
```

#### 2.4. Implementar sincronización offline-first

**Crear:** `ColeHop/ColeHop/Services/Sync/ISyncService.cs`

```csharp
public interface ISyncService
{
	Task<bool> SyncAllAsync();
	Task<bool> PushLocalChangesAsync();
	Task<bool> PullRemoteDataAsync();
	Task<DateTime?> GetLastSyncTimeAsync();
}
```

**Estrategia:**
1. La app trabaja siempre con datos locales (SQLite)
2. Al tener conexión, sincroniza cambios con el backend
3. Manejo de conflictos: timestamp del servidor prevalece

#### 2.5. Actualizar servicios mock existentes

Reemplazar:
- `MockTutorManagementService` → `LocalTutorManagementService` (usa SQLite)
- `MockTeacherService` → `LocalTeacherService` (usa SQLite)
- `PickupService` → Integrar con SQLite + HttpPickupService

---

### PASO 3: Testing Unitario e Integración

#### 3.1. Crear proyecto de tests

```bash
cd C:\Users\U362407\Samu\Desa\ColeHop
dotnet new xunit -n ColeHop.Tests
cd ColeHop.Tests
dotnet add reference ../ColeHop/ColeHop.csproj
dotnet add package Moq
dotnet add package FluentAssertions
```

#### 3.2. Tests críticos para implementar

**Tests de seguridad NFC (PRIORIDAD ALTA):**

```csharp
// ColeHop.Tests/Services/Nfc/PaceSessionTests.cs
public class PaceSessionTests
{
	[Fact]
	public void DeriveKeyFromCan_WithValidCan_ReturnsCorrectKey()
	{
		// Verificar derivación de K_pi según BSI TR-03110
	}

	[Fact]
	public async Task EstablishSecureChannelAsync_WithInvalidCan_ThrowsException()
	{
		// Simular CAN incorrecto
	}
}

// ColeHop.Tests/Services/Nfc/DnieIdentityExtractorTests.cs
public class DnieIdentityExtractorTests
{
	[Fact]
	public void ExtractFromMrz_WithValidTD1_ExtractsCorrectData()
	{
		// Parseo de MRZ de DNIe español
	}
}

// ColeHop.Tests/Services/Nfc/SodValidatorTests.cs
public class SodValidatorTests
{
	[Fact]
	public void Validate_WithTamperedDg_ReturnsFalse()
	{
		// Verificar detección de manipulación
	}
}
```

**Tests de ViewModels:**

```csharp
// ColeHop.Tests/ViewModels/NfcScanViewModelTests.cs
public class NfcScanViewModelTests
{
	[Fact]
	public async Task StartScanCommand_WithValidCan_ReadsIdentitySuccessfully()
	{
		// Mock INfcService
	}
}
```

**Tests de integración con backend:**

```csharp
// ColeHop.Tests/Integration/AuthServiceIntegrationTests.cs
public class AuthServiceIntegrationTests
{
	[Fact]
	public async Task Login_WithValidCredentials_ReturnsToken()
	{
		// Prueba contra backend real
	}
}
```

#### 3.3. Tests con DNIe físico real

**Crear:** `ColeHop.Tests/Manual/DniePhysicalTests.md`

Documentar casos de prueba manual:
1. ✅ Lectura correcta con CAN válido
2. ✅ Fallo con CAN incorrecto
3. ✅ Validación de integridad SOD
4. ✅ Extracción de foto facial
5. ✅ Comportamiento al alejar tarjeta durante lectura

#### 3.4. Cobertura de código objetivo

**Mínimo recomendado para TFM:**
- Servicios NFC: ≥ 80%
- ViewModels: ≥ 70%
- Servicios de autenticación: ≥ 85%

---

### PASO 4: Documentación Técnica Final

#### 4.1. Actualizar documentos existentes

**Revisar y completar:**
1. `CONTEXTO_PROYECTO_COLEHOP.md` - Añadir sección de backend implementado
2. `IMPLEMENTACION_LECTURA_NFC.md` - Añadir resultados de pruebas reales
3. `ACCIONES_PENDIENTES_DNI.md` - Marcar todas las tareas como completadas
4. `RESUMEN_CODIFICACION_COLEHOP.md` - Añadir sesiones 6+ con backend y testing

#### 4.2. Crear manual de despliegue

**Crear:** `C:\Users\U362407\Samu\Desa\Documentos\MANUAL_DESPLIEGUE.md`

Contenido:
```markdown
# Manual de Despliegue - ColeHop

## 1. Requisitos Previos
- Visual Studio 2022/2026
- .NET 10 SDK
- Android SDK (API 31+)
- Xcode (para iOS)
- SQL Server / Azure SQL Database

## 2. Configuración del Backend
[Pasos detallados...]

## 3. Configuración de la App Móvil
[Pasos detallados...]

## 4. Configuración de Base de Datos
[Scripts SQL...]

## 5. Variables de Entorno
[Configuración...]
```

#### 4.3. Crear diagramas de arquitectura actualizados

**Actualizar:** `Documentos/diagramas.md`

Añadir:
1. Diagrama de arquitectura completa (app + backend + base de datos)
2. Diagrama de secuencia de lectura NFC completo
3. Diagrama de flujo de sincronización offline
4. Diagrama de modelo de datos (ER)

#### 4.4. Crear guía de usuario

**Crear:** `C:\Users\U362407\Samu\Desa\Documentos\GUIA_USUARIO.md`

Secciones:
1. **Introducción** - ¿Qué es ColeHop?
2. **Instalación** - Requisitos y pasos
3. **Registro** - Para tutores
4. **Gestión de hijos** - Añadir, editar, eliminar
5. **Gestión de autorizaciones** - Crear, consultar
6. **Recogida escolar** - Flujo para profesores
7. **Lectura DNIe** - Instrucciones paso a paso
8. **Preguntas frecuentes**

#### 4.5. Documentar casos de uso completos

**Actualizar:** `C:\Users\U362407\Samu\Desa\Documentos\casos_de_uso`

Casos de uso principales:
1. **CU-01: Registro de tutor con aprobación del centro**
2. **CU-02: Gestión de personas autorizadas**
3. **CU-03: Creación de autorización temporal**
4. **CU-04: Verificación de identidad con DNIe + NFC**
5. **CU-05: Registro de recogida exitosa**
6. **CU-06: Denegación de recogida por falta de autorización**
7. **CU-07: Consulta de historial de recogidas**
8. **CU-08: Sincronización offline-online**

Formato de cada caso de uso:
```markdown
## CU-04: Verificación de identidad con DNIe + NFC

**Actor principal:** Profesor
**Precondiciones:** 
- Profesor autenticado en la app
- Persona física presente con DNIe 3.0/4.0
- Dispositivo con NFC habilitado

**Flujo principal:**
1. El profesor accede a "Recogidas del día"
2. Selecciona al niño a recoger
3. La app solicita el CAN del DNIe
4. El profesor pide el CAN a la persona (impreso en el DNIe)
5. El profesor introduce el CAN y pulsa "Iniciar escaneo"
6. La app solicita acercar el DNIe al teléfono
7. El profesor acerca el DNIe (zona posterior)
8. La app realiza el protocolo PACE (2-3 segundos)
9. La app lee y valida los datos del DNIe (3-5 segundos)
10. La app muestra la identidad verificada con foto
11. La app verifica automáticamente si existe autorización
12. Si hay autorización válida, muestra botón "Confirmar recogida"
13. El profesor confirma y se registra la recogida

**Flujo alternativo 4a: CAN incorrecto**
- El chip DNIe rechaza la autenticación PACE
- La app muestra error "CAN incorrecto"
- Volver al paso 3

**Postcondiciones:**
- Identidad verificada criptográficamente
- Recogida registrada con timestamp, identidad verificada y profesor
- Historial actualizado para el tutor
```

---

### PASO 5: Validación iOS

#### 5.1. Implementar IosNfcPlatformService

**Archivo:** `ColeHop/ColeHop/Platforms/iOS/Nfc/IosNfcPlatformService.cs`

**Referencia:** Doc 9303 ICAO + Apple Core NFC

```csharp
using CoreNFC;
using Foundation;

namespace ColeHop.Platforms.iOS.Nfc
{
	public class IosNfcPlatformService : INfcPlatformService
	{
		private NFCTagReaderSession? _session;

		public async Task<byte[]> TransceiveAsync(byte[] apdu)
		{
			// Implementación con NFCISO7816Tag
		}
	}
}
```

#### 5.2. Configurar permisos iOS

**Archivo:** `ColeHop/ColeHop/Platforms/iOS/Info.plist`

```xml
<key>NFCReaderUsageDescription</key>
<string>ColeHop necesita acceso NFC para leer el DNI electrónico</string>
<key>com.apple.developer.nfc.readersession.iso7816.select-identifiers</key>
<array>
	<string>A0000002471001</string>
</array>
```

#### 5.3. Probar en dispositivo iOS físico

**Requisitos:**
- iPhone 7 o superior
- iOS 13+
- DNIe 3.0 o 4.0

**Validaciones:**
1. Detección de tag NFC
2. Comunicación ISO-DEP
3. Protocolo PACE
4. Lectura de DGs

---

### PASO 6: Preparación de Entregables para la Segunda Entrega

#### 6.1. Código fuente completo

**Entregar:**
- Repositorio Git completo: `https://github.com/cas1201/TFM_SAL_DNIe`
- Tag de versión: `v2.0-segunda-entrega`
- README.md actualizado con instrucciones de compilación

#### 6.2. Backend desplegado

**Entregar:**
- URL del backend funcional (Azure App Service)
- Swagger UI accesible
- Base de datos provisionada
- Credenciales de prueba para evaluación

#### 6.3. APK/IPA de prueba

**Generar:**
```bash
# Android
cd ColeHop\ColeHop
dotnet publish -f net10.0-android -c Release
```

**Entregar:**
- `ColeHop-v2.0.apk` firmado para Android
- Instrucciones de instalación

#### 6.4. Documentación técnica

**Carpeta:** `C:\Users\U362407\Samu\Desa\Documentos\Entrega 2\`

**Contenido:**
1. `MEMORIA_TECNICA_SEGUNDA_ENTREGA.pdf`
   - Resumen ejecutivo
   - Arquitectura implementada
   - Decisiones técnicas (por qué ASP.NET Core, SQLite, etc.)
   - Análisis de seguridad del protocolo NFC
   - Resultados de testing
   - Capturas de pantalla de la app funcionando

2. `MANUAL_DESPLIEGUE.pdf`

3. `GUIA_USUARIO.pdf`

4. `CASOS_DE_USO.pdf`

5. `DIAGRAMAS_ACTUALIZADOS.pdf`
   - Arquitectura completa
   - Flujos de datos
   - Modelo de base de datos
   - Secuencias de interacción

6. `RESULTADOS_TESTING.pdf`
   - Cobertura de código
   - Resultados de tests unitarios
   - Resultados de tests de integración
   - Pruebas con DNIe físico (fotos/videos)

#### 6.5. Video demostración

**Grabar video de 5-10 minutos mostrando:**
1. Registro de tutor
2. Gestión de hijos y personas autorizadas
3. Creación de autorización
4. Lectura NFC del DNIe 3.0 en tiempo real
5. Verificación de autorización
6. Confirmación de recogida
7. Consulta de historial

**Formato:** MP4, 1080p, con audio explicativo

---

## 📅 CRONOGRAMA RECOMENDADO

### Semana 1-2: Backend
- Día 1-3: Crear proyecto ASP.NET Core, configurar DbContext
- Día 4-6: Implementar endpoints de autenticación
- Día 7-10: Implementar endpoints de tutores
- Día 11-14: Implementar endpoints de profesores, desplegar a Azure

### Semana 3: Base de Datos Local y Sincronización
- Día 15-17: Implementar LocalDatabase con SQLite
- Día 18-20: Crear SyncService
- Día 21: Integrar con servicios existentes

### Semana 4: Testing
- Día 22-24: Tests unitarios de servicios NFC
- Día 25-26: Tests de ViewModels
- Día 27-28: Tests de integración con backend

### Semana 5: iOS y Documentación
- Día 29-31: Implementar IosNfcPlatformService
- Día 32-33: Pruebas en iPhone
- Día 34-35: Documentación técnica

### Semana 6: Entregables
- Día 36-38: Generar APKs, preparar repositorio
- Día 39-40: Grabar video demostración
- Día 41-42: Revisión final y entrega

---

## ✅ CHECKLIST FINAL ANTES DE ENTREGAR

### Funcionalidad
- [ ] Backend desplegado y accesible públicamente
- [ ] Base de datos SQLite funcionando en la app
- [ ] Sincronización offline-online implementada
- [ ] Lectura NFC del DNIe 3.0 exitosa con dispositivo real
- [ ] Flujo completo tutor: registro → gestión hijos → autorizaciones
- [ ] Flujo completo profesor: recogidas → verificación NFC → registro
- [ ] App funciona sin conexión a internet (salvo sync)

### Seguridad
- [ ] Autenticación JWT implementada
- [ ] Limpieza de claves de sesión PACE tras uso
- [ ] Validación SOD (integridad de datos DNIe)
- [ ] HTTPS en todas las comunicaciones con backend
- [ ] Almacenamiento seguro de tokens (SecureStorage)

### Testing
- [ ] ≥50 tests unitarios pasando
- [ ] Cobertura de código ≥70% en servicios críticos
- [ ] Tests de integración con backend pasando
- [ ] Pruebas manuales con DNIe físico documentadas

### Documentación
- [ ] README.md completo con instrucciones de setup
- [ ] Manual de despliegue para backend y app
- [ ] Guía de usuario con capturas de pantalla
- [ ] Casos de uso detallados (≥8 casos)
- [ ] Diagramas actualizados (arquitectura, secuencia, ER)
- [ ] Análisis de seguridad del protocolo NFC documentado
- [ ] Memoria técnica (≥30 páginas)

### Entregables
- [ ] Repositorio Git con tag `v2.0-segunda-entrega`
- [ ] APK Android firmado y funcionando
- [ ] Backend en Azure con Swagger accesible
- [ ] Video demostración (5-10 minutos)
- [ ] Documentación en PDF (≥6 documentos)

---

## 🚀 CRITERIOS DE ÉXITO PARA LA SEGUNDA ENTREGA

### Técnicos
1. **Backend funcional:** Todos los endpoints responden correctamente
2. **Lectura NFC operativa:** Demostración en vivo con DNIe 3.0 real
3. **Persistencia:** Datos se guardan localmente y sincronizan con backend
4. **Testing:** Suite de tests ejecutándose y pasando

### Documentación
1. **Completitud:** Toda la funcionalidad implementada está documentada
2. **Diagramas:** Arquitectura, flujos y datos representados visualmente
3. **Casos de uso:** Cubriendo todos los flujos principales de usuario

### Seguridad
1. **Protocolo PACE:** Implementado según BSI TR-03110
2. **Validación criptográfica:** SOD verificado correctamente
3. **Gestión de secretos:** CAN y claves de sesión manejados de forma segura

---

## 📌 NOTAS IMPORTANTES

### Diferencias con la Primera Entrega
- **Primera entrega:** Prototipo funcional con datos mock, arquitectura definida
- **Segunda entrega:** Aplicación completa con backend real, base de datos, testing y lectura NFC operativa

### Recomendaciones para el TFM
1. **Énfasis en seguridad:** Documentar en profundidad el protocolo PACE y las decisiones de seguridad
2. **Evidencia de pruebas reales:** Incluir fotos/videos de la lectura del DNIe físico
3. **Análisis comparativo:** Comparar tu implementación manual de PACE con el SDK oficial de la FNMT
4. **Limitaciones:** Ser transparente sobre qué no se implementó (ej: validación de certificado CSCA)

### Recursos Clave del Proyecto
- **Contexto completo:** `ColeHop/CONTEXTO_PROYECTO_COLEHOP.md`
- **Implementación NFC:** `ColeHop/IMPLEMENTACION_LECTURA_NFC.md`
- **Especificación backend:** `ColeHop/CREACION_BACKEND.md`
- **Tareas pendientes:** `ColeHop/ACCIONES_PENDIENTES_DNI.md`

---

## 🎓 RELACIÓN CON LA MEMORIA DEL TFM

Este documento se alinea con la estructura de la memoria del TFM (`memoria_TFM_samuel_aragones_lozano.docx`) en los siguientes capítulos:

1. **Capítulo 3: Diseño e Implementación**
   - 3.1. Arquitectura del sistema → Implementada y documentada
   - 3.2. Implementación del protocolo PACE → Completado en paso 1-6 de `ACCIONES_PENDIENTES_DNI.md`
   - 3.3. Backend API REST → PASO 1 de este documento
   - 3.4. Base de datos y persistencia → PASO 2 de este documento

2. **Capítulo 4: Seguridad**
   - 4.1. Análisis del protocolo PACE → `IMPLEMENTACION_LECTURA_NFC.md` sección 4
   - 4.2. Secure Messaging → Implementado y documentado
   - 4.3. Validación de integridad (SOD) → Implementado en `SodValidator.cs`

3. **Capítulo 5: Pruebas y Validación**
   - 5.1. Tests unitarios → PASO 3 de este documento
   - 5.2. Tests de integración → PASO 3.2
   - 5.3. Pruebas con dispositivo real → PASO 3.3

4. **Capítulo 6: Conclusiones**
   - Logros alcanzados
   - Limitaciones identificadas
   - Trabajo futuro

---

## 📧 CONTACTO Y SOPORTE

Para dudas sobre la implementación, consultar:
- Documentación técnica en `ColeHop/*.md`
- Comentarios en el código fuente
- Especificación oficial BSI TR-03110 (protocolo PACE)
- ICAO Doc 9303 (eMRTD)

---

**Fecha de creación:** Enero 2025
**Versión:** 2.0 - Segunda Entrega
**Autor:** Samuel Aragonés Lozano
**TFM:** Aplicación móvil de verificación de recogidas escolares con DNIe 3.0 + NFC
