# 📱 ColeHop - Contexto Completo del Proyecto

## 📖 Índice
1. [Visión del Proyecto](#visión-del-proyecto)
2. [Propósito y Objetivos](#propósito-y-objetivos)
3. [Arquitectura y Estructura](#arquitectura-y-estructura)
4. [Directrices de Diseño](#directrices-de-diseño)
5. [Patrones y Convenciones](#patrones-y-convenciones)
6. [Tecnologías Utilizadas](#tecnologías-utilizadas)
7. [Flujos Principales](#flujos-principales)
8. [Lo Que NO Se Quiere](#lo-que-no-se-quiere)
9. [Guía de Desarrollo](#guía-de-desarrollo)

---

## 🎯 Visión del Proyecto

### ¿Qué es ColeHop?

**ColeHop** es una aplicación móvil para **verificación segura de recogidas escolares** mediante el uso de **DNIe 3.0** (Documento Nacional de Identidad electrónico español versión 3.0) con tecnología **NFC**.

### Problema que Resuelve

En los centros escolares, existe la necesidad de verificar la identidad de las personas que recogen a los menores de forma **segura, rápida y fiable**. ColeHop elimina:
- Verificación manual de identidades
- Uso de listas en papel
- Autorización sin verificación biométrica
- Suplantación de identidad
- Procesos lentos en horas punta

### Valor Diferencial

- ✅ **Verificación criptográfica** mediante DNIe 3.0
- ✅ **Lectura NFC segura** con protocolo PACE
- ✅ **Trazabilidad completa** de todas las recogidas
- ✅ **Sin conexión a internet** necesaria para verificación NFC
- ✅ **Gestión de autorizaciones** flexible por parte de tutores

---

## 🎯 Propósito y Objetivos

### Objetivos Principales

#### 1. **Seguridad Máxima**
- Verificación criptográfica de identidad mediante DNIe 3.0
- Protocolo PACE (Password Authenticated Connection Establishment)
- Validación de firma electrónica del documento
- Lectura de datos biométricos (DG2 - foto, DG7 - firma manuscrita)

#### 2. **Facilidad de Uso**
- Interfaz simple e intuitiva
- Proceso de recogida en menos de 30 segundos
- Sin necesidad de formación técnica
- Feedback visual claro en cada paso

#### 3. **Trazabilidad Total**
- Registro de todas las recogidas con timestamp
- Identificación del profesor que autoriza
- Identificación verificada de quien recoge
- Historial completo consultable

#### 4. **Flexibilidad**
- Autorizaciones temporales o permanentes
- Gestión por parte de tutores legales
- Calendario de autorizaciones personalizado
- Múltiples personas autorizadas por menor

### Usuarios Principales

1. **Profesores/Personal del Centro**
   - Verifican identidad mediante DNIe + NFC
   - Autorizan recogidas
   - Consultan lista diaria de recogidas

2. **Tutores Legales (Padres/Madres)**
   - Gestionan personas autorizadas
   - Crean autorizaciones temporales
   - Consultan historial de recogidas

3. **Administración del Centro**
   - Supervisan el sistema
   - Consultan estadísticas
   - Gestionan usuarios profesores

---

## 🏗️ Arquitectura y Estructura

### Principio: Clean Architecture

El proyecto sigue **Clean Architecture** con separación estricta de responsabilidades:

```
ColeHop/
├── Core/                          # Capa de dominio (sin dependencias)
│   ├── Services/                  # Interfaces de servicios
│   │   ├── Auth/
│   │   │   ├── IAuthService.cs
│   │   │   └── Dtos/
│   │   ├── Nfc/
│   │   │   ├── INfcService.cs
│   │   │   ├── INfcPlatformService.cs
│   │   │   └── Dtos/
│   │   └── Pickup/
│   │       ├── IPickupService.cs
│   │       └── Dtos/
│   └── (Sin implementaciones concretas)
│
├── Model/                         # Modelos de dominio
│   ├── Domain/                    # Entidades de negocio
│   │   └── PickupLog.cs
│   └── Identity/
│       └── UserRole.cs
│
├── Services/                      # Implementaciones de servicios
│   ├── Auth/
│   │   └── AuthService.cs         # Autenticación y sesiones
│   ├── Nfc/
│   │   ├── NfcService.cs          # Orquestación NFC
│   │   └── Dnie/                  # Lógica específica DNIe
│   │       ├── DnieFileReader.cs
│   │       ├── PaceAuthenticator.cs
│   │       └── SodValidator.cs
│   └── Pickup/
│       └── PickupService.cs       # Lógica de recogidas
│
├── ViewModel/                     # ViewModels MVVM
│   ├── BaseViewModel.cs           # Clase base común
│   ├── LoginViewmodel.cs
│   ├── DashboardTeacherViewModel.cs
│   ├── DashboardTutorViewModel.cs
│   ├── DailyPickupListViewModel.cs
│   ├── NfcScanViewModel.cs
│   └── ...
│
├── View/                          # Vistas XAML
│   ├── LoginPage.xaml
│   ├── DashboardTeacherPage.xaml
│   ├── DashboardTutorPage.xaml
│   ├── DailyPickupListPage.xaml
│   ├── NfcScanPage.xaml
│   └── ...
│
├── Platforms/                     # Código específico de plataforma
│   ├── Android/
│   │   └── Nfc/
│   │       └── AndroidNfcPlatformService.cs
│   └── iOS/
│       └── Nfc/
│           └── IosNfcPlatformService.cs
│
├── Converters/                    # Conversores XAML
│   ├── InvertedBoolConverter.cs
│   └── ...
│
├── AppShell.xaml(.cs)            # Navegación Shell dinámica
└── MauiProgram.cs                # Configuración DI
```

### Principios Arquitecturales

#### 1. **Separación de Concerns**
- **Core:** Solo interfaces y DTOs (sin lógica)
- **Model:** Solo entidades de dominio (sin lógica de negocio)
- **Services:** Implementaciones concretas de lógica de negocio
- **ViewModel:** Lógica de presentación con CommunityToolkit.Mvvm
- **View:** Solo XAML sin code-behind (excepto inicialización básica)

#### 2. **Dependency Injection**
- Todas las dependencias se inyectan vía constructor
- Registro en `MauiProgram.cs`
- Sin uso de service locator o static instances
- Ciclo de vida `Singleton` para servicios stateless

#### 3. **Inyección de Plataforma**
- Código específico de plataforma en `Platforms/`
- Interfaces comunes en `Core/`
- Registro condicional según plataforma (`#if ANDROID`)

---

## 🎨 Directrices de Diseño

### Filosofía de Diseño

**Minimalista, Profesional, Funcional**

El diseño de ColeHop prioriza:
1. **Claridad** sobre decoración
2. **Funcionalidad** sobre estética
3. **Accesibilidad** sobre originalidad

### Paleta de Colores

```xml
<!-- Colores principales -->
<Color x:Key="Primary">#512BD4</Color>           <!-- Morado principal -->
<Color x:Key="Secondary">#DFD8F7</Color>         <!-- Morado claro -->
<Color x:Key="Tertiary">#2B0B98</Color>          <!-- Morado oscuro -->

<!-- Estados -->
<Color x:Key="Success">#4CAF50</Color>           <!-- Verde éxito -->
<Color x:Key="Warning">#FF9800</Color>           <!-- Naranja advertencia -->
<Color x:Key="Error">#F44336</Color>             <!-- Rojo error -->

<!-- Neutros -->
<Color x:Key="Gray100">#FAFAFA</Color>
<Color x:Key="Gray900">#212121</Color>
```

### Componentes UI

#### 1. **Botones**
- **Primarios:** Fondo con color Primary, texto blanco
- **Secundarios:** Fondo blanco, borde Primary (2px), texto Primary
- **Altura estándar:** 60px
- **CornerRadius estándar:** 12px
- **Font:** Bold, tamaño 16

```xaml
<!-- Botón Primario -->
<Button 
	Text="Acción principal"
	BackgroundColor="{DynamicResource Primary}"
	TextColor="White"
	CornerRadius="12"
	HeightRequest="60"
	FontSize="16"
	FontAttributes="Bold" />

<!-- Botón Secundario -->
<Button 
	Text="Acción secundaria"
	BackgroundColor="White"
	TextColor="{DynamicResource Primary}"
	BorderColor="{DynamicResource Primary}"
	BorderWidth="2"
	CornerRadius="12"
	HeightRequest="60"
	FontSize="16"
	FontAttributes="Bold" />
```

#### 2. **Tarjetas Informativas**
- Uso de `Border` con `RoundRectangle`
- Padding: 20px
- Sombras sutiles (opcional)
- StrokeThickness: 0 (sin borde)

```xaml
<Border
	StrokeThickness="0"
	BackgroundColor="{DynamicResource Primary}"
	Padding="20"
	StrokeShape="RoundRectangle 16">
	<VerticalStackLayout Spacing="8">
		<Label Text="Título" FontSize="18" TextColor="White" FontAttributes="Bold" />
		<Label Text="Descripción" FontSize="14" TextColor="White" Opacity="0.9" />
	</VerticalStackLayout>
</Border>
```

#### 3. **Espaciado Consistente**
- **Padding general:** 20px
- **Spacing entre elementos:** 20px
- **Margin de títulos:** 0,20,0,40

#### 4. **Tipografía**
- **Títulos principales:** 28px, Bold
- **Subtítulos:** 18px, Bold
- **Texto normal:** 14-16px
- **Fuente:** OpenSans (Regular y Semibold)

#### 5. **Listas**
- Uso de `CollectionView` con `ItemTemplate`
- Separadores con `BoxView` de 1px, color LightGray
- Items con padding 16px vertical

### Estados Visuales

#### Estados de Recogida
```csharp
// Colores según estado
Pending → Gray
Completed → Green
```

#### Feedback de NFC
- **Esperando:** Texto informativo en negro
- **Escaneando:** Color Primary con indicador de actividad
- **Éxito:** Verde con icono de check
- **Error:** Rojo con mensaje descriptivo

---

## 📐 Patrones y Convenciones

### Patrón MVVM con CommunityToolkit

#### ViewModels
```csharp
public sealed partial class XxxViewModel : BaseViewModel
{
	// Propiedades observables con source generator
	[ObservableProperty]
	private string _propiedad = valorInicial;

	// Constructor con inyección de dependencias
	public XxxViewModel(IAuthService auth) : base(auth) { }

	// Comandos con source generator
	[RelayCommand]
	private async Task AccionAsync()
	{
		// Lógica del comando
	}

	// Inicialización si necesario
	public void Initialize()
	{
		// Cargar datos
	}
}
```

**Reglas:**
- ✅ Todos los ViewModels heredan de `BaseViewModel`
- ✅ Propiedades privadas con `_camelCase`
- ✅ Uso de `[ObservableProperty]` para propiedades observables
- ✅ Uso de `[RelayCommand]` para comandos
- ✅ Métodos async terminan en `Async`
- ✅ Constructor recibe `IAuthService` mínimo
- ✅ Clase `sealed partial`

#### Services
```csharp
public sealed class XxxService : IXxxService
{
	private readonly IDependencia _dependencia;

	public XxxService(IDependencia dependencia)
	{
		_dependencia = dependencia;
	}

	public async Task<T> MetodoAsync()
	{
		// Implementación
	}
}
```

**Reglas:**
- ✅ Todas las implementaciones son `sealed`
- ✅ Implementan interfaz de `Core.Services`
- ✅ Dependencias inyectadas por constructor
- ✅ Métodos públicos documentados con comentarios útiles

### Navegación

#### Patrón Observer para Autenticación
```csharp
// En AuthService
public event EventHandler<UserRole?>? AuthenticationStateChanged;

// En AppShell
_auth.AuthenticationStateChanged += OnAuthenticationStateChanged;

private void OnAuthenticationStateChanged(object? sender, UserRole? role)
{
	if (role.HasValue)
		ConfigureShellForRole(role.Value);
	else
		ShowLoginPage();
}
```

**Reglas:**
- ✅ AppShell escucha evento de autenticación
- ✅ Items del Shell se crean dinámicamente (no estáticos en XAML)
- ✅ Rutas simples sin barras en items raíz (`"tutor"`, `"teacher"`)
- ✅ Navegación interna con rutas completas (`"pickup/list"`, `"nfc/scan"`)

#### Navegación Estándar
```csharp
// Navegación hacia adelante
await Shell.Current.GoToAsync("ruta");

// Navegación con parámetros
await Shell.Current.GoToAsync($"ruta?param={valor}");

// Navegación hacia atrás
await Shell.Current.GoToAsync("..");
```

### Convenciones de Nombres

#### Archivos
- ViewModels: `XxxViewModel.cs`
- Views: `XxxPage.xaml` + `XxxPage.xaml.cs`
- Services: `XxxService.cs`
- Interfaces: `IXxxService.cs`
- DTOs: `XxxDto.cs` o nombre descriptivo
- Records: `XxxItem` (para modelos simples)

#### Variables y Propiedades
```csharp
// Campos privados
private readonly IDependencia _dependencia;
private string _nombreCampo;

// Propiedades públicas
public string NombrePropiedad { get; set; }

// Constantes
private const string NombreConstante = "valor";
```

### Comentarios

#### ✅ Comentarios Útiles
```csharp
// Explicar decisiones arquitecturales
// Obtener contexto de Android desde Platform API (no desde DI)
_context = Platform.CurrentActivity?.ApplicationContext;

// Documentar código temporal
// Datos simulados para desarrollo/testing
var result = new List<DailyPickupItem> { ... };

// Explicar flujos complejos
// Notificar cambio de estado - AppShell escucha este evento
AuthenticationStateChanged?.Invoke(this, role);

// Documentar compatibilidad
// Android 13+ (API 33+)
#pragma warning disable CA1416
tag = intent.GetParcelableExtra(...);
```

#### ❌ Comentarios Redundantes
```csharp
// ❌ NO hacer esto
// Crear usuario
var user = new User();

// ❌ NO hacer esto
// Llamar al método
await MetodoAsync();
```

---

## 🔧 Tecnologías Utilizadas

### Framework y Plataforma
- **.NET 10** (versión preliminar)
- **.NET MAUI** (Multi-platform App UI)
- **C# 12+**

### Librerías Principales
- **CommunityToolkit.Mvvm 8.x** - Patrón MVVM con source generators
- **Microsoft.Maui.Controls** - Controles UI de MAUI

### Plataformas Soportadas
- ✅ **Android 21+** (Android 5.0 Lollipop en adelante)
- ⏳ **iOS 14+** (pendiente de implementación completa)

### Tecnología NFC
- **Android NFC API** (Android.Nfc namespace)
- **ISO-DEP (ISO 14443-4)** - Comunicación con tarjetas inteligentes
- **APDU Commands** - Comandos para lectura DNIe
- **PACE Protocol** - Autenticación segura con CAN

### Almacenamiento
- **SecureStorage** (MAUI) - Almacenamiento seguro de tokens y sesión
- Sin base de datos local (datos simulados en memoria)

---

## 🔄 Flujos Principales

### 1. Flujo de Autenticación

```
[Inicio App]
	↓
[Mostrar LoginPage]
	↓
[Usuario selecciona rol] → Profesor / Tutor
	↓
[SimulateLoginAsync(role)]
	↓
[Guardar sesión en SecureStorage]
	↓
[Disparar evento AuthenticationStateChanged]
	↓
[AppShell recibe evento]
	↓
[ConfigureShellForRole(role)]
	↓
[Crear ShellContent dinámico según rol]
	↓
[Navegar a Dashboard correspondiente]
```

### 2. Flujo de Recogida Escolar (Profesor)

```
[Dashboard Profesor]
	↓
[Ver "Lista de recogidas del día"]
	↓
[DailyPickupListPage] ← Carga lista con IPickupService
	↓
[Profesor selecciona niño a recoger]
	↓
[Navegar a NfcScanPage con childId + childName]
	↓
[Profesor introduce CAN del DNIe]
	↓
[Pulsar "Iniciar escaneo"]
	↓
┌─────────────────────────────────────────┐
│ [Proceso NFC]                            │
│   1. BeginDnieReadingAsync(can)         │
│   2. HandleIntent() recibe tag NFC       │
│   3. Ejecutar PACE con CAN               │
│   4. Seleccionar aplicación DNIe (APDU)  │
│   5. Leer archivos: DG1, DG2, DG11, SOD  │
│   6. Validar SOD (firma digital)         │
│   7. Extraer datos de identidad          │
│   8. Disparar evento IdentityVerified    │
└─────────────────────────────────────────┘
	↓
[OnIdentityVerified() en ViewModel]
	↓
[CheckAuthorizationAsync()] ← Verificar si persona está autorizada
	↓
[¿Autorizado?]
	├─ SÍ → [ConfirmPickupAsync()]
	│           ↓
	│       [Registrar en PickupLog]
	│           ↓
	│       [Mostrar éxito]
	│           ↓
	│       [Volver a lista]
	│
	└─ NO → [Mostrar error "No autorizado"]
				↓
			[Volver a escanear o cancelar]
```

### 3. Flujo de Gestión de Autorizaciones (Tutor)

```
[Dashboard Tutor]
	↓
[Ver "Mis hijos"]
	↓
[ChildrenPage] ← Lista de hijos (simulada)
	↓
[Ver "Personas autorizadas"]
	↓
[AuthorizedPersonPage] ← Lista de autorizados (simulada)
	↓
[Ver "Calendario de autorizaciones"]
	↓
[AuthorizationPage]
	↓
[Seleccionar hijo(s)]
	↓
[Seleccionar persona autorizada]
	↓
[Seleccionar fecha(s)]
	↓
[Crear autorización]
	↓
[Guardar en backend] (pendiente implementación real)
	↓
[Confirmación]
```

### 4. Flujo de Restauración de Sesión

```
[App.OnStart()]
	↓
[AppShell.InitializeShell()]
	↓
[Mostrar LoginPage inmediatamente]
	↓
[En background: TryRestoreSessionAsync()]
	↓
[Leer SecureStorage: userId, token, role]
	↓
[¿Sesión válida?]
	├─ SÍ → [Restaurar _currentSession]
	│           ↓
	│       [ConfigureShellForRole(role)]
	│           ↓
	│       [Usuario ve su dashboard]
	│
	└─ NO → [Usuario ve LoginPage]
				↓
			[Debe autenticarse]
```

---

## ❌ Lo Que NO Se Quiere

### Diseño y UI

❌ **NO usar emojis** en textos de botones o interfaz  
❌ **NO usar iconos decorativos** innecesarios  
❌ **NO usar animaciones complejas** que ralenticen  
❌ **NO usar colores estridentes** o degradados excesivos  
❌ **NO sobrecargar** la interfaz con información  
❌ **NO usar fuentes decorativas** o difíciles de leer  

### Código

❌ **NO mezclar lógica de negocio en ViewModels** (debe estar en Services)  
❌ **NO usar code-behind** para lógica (solo para inicialización básica)  
❌ **NO usar static instances** o singletons manuales  
❌ **NO usar magic strings** para rutas de navegación (registrarlas)  
❌ **NO duplicar código** entre plataformas si es evitable  
❌ **NO crear dependencias circulares** entre capas  
❌ **NO ignorar warnings** del compilador  

### Arquitectura

❌ **NO acceder directamente a Services** desde Views  
❌ **NO poner implementaciones** en la carpeta `Core/`  
❌ **NO mezclar concerns** (UI + Lógica + Datos juntos)  
❌ **NO usar ViewModels compartidos** entre múltiples Views sin BaseViewModel  
❌ **NO hardcodear rutas** de navegación en múltiples lugares  

### Comentarios

❌ **NO usar emojis** en comentarios (🚫 ❌ ✅)  
❌ **NO comentar lo obvio** ("crear variable", "llamar método")  
❌ **NO dejar TODOs** sin contexto o sin fecha límite  
❌ **NO comentar código** en lugar de eliminarlo (usar control de versiones)  

### Navegación

❌ **NO usar rutas anidadas** en ShellContent raíz (ej: "dashboard/tutor")  
❌ **NO mezclar items estáticos en XAML** con dinámicos en code-behind  
❌ **NO navegar sin await** cuando se usa Shell.GoToAsync()  
❌ **NO crear múltiples instancias** del mismo ViewModel sin necesidad  

---

## 📚 Guía de Desarrollo

### Agregar un Nuevo Feature

#### 1. Definir Contratos (Core)
```csharp
// Core/Services/MiFeature/IMiFeatureService.cs
namespace ColeHop.Core.Services.MiFeature
{
	public interface IMiFeatureService
	{
		Task<ResultDto> HacerAlgoAsync(string parametro);
	}
}

// Core/Services/MiFeature/Dtos/ResultDto.cs
namespace ColeHop.Core.Services.MiFeature.Dtos
{
	public sealed record ResultDto(string Valor, bool Exito);
}
```

#### 2. Implementar Service
```csharp
// Services/MiFeature/MiFeatureService.cs
namespace ColeHop.Services.MiFeature
{
	public sealed class MiFeatureService : IMiFeatureService
	{
		private readonly IDependencia _dep;

		public MiFeatureService(IDependencia dep)
		{
			_dep = dep;
		}

		public async Task<ResultDto> HacerAlgoAsync(string parametro)
		{
			// Implementación
			return new ResultDto("resultado", true);
		}
	}
}
```

#### 3. Crear ViewModel
```csharp
// ViewModel/MiFeatureViewModel.cs
namespace ColeHop.ViewModel
{
	public sealed partial class MiFeatureViewModel : BaseViewModel
	{
		private readonly IMiFeatureService _miService;

		[ObservableProperty]
		private string _resultado = string.Empty;

		public MiFeatureViewModel(
			IAuthService auth,
			IMiFeatureService miService) : base(auth)
		{
			_miService = miService;
		}

		[RelayCommand]
		private async Task CargarDatosAsync()
		{
			try
			{
				IsBusy = true;
				var result = await _miService.HacerAlgoAsync("param");
				Resultado = result.Valor;
			}
			finally
			{
				IsBusy = false;
			}
		}
	}
}
```

#### 4. Crear View
```xaml
<!-- View/MiFeaturePage.xaml -->
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage
	x:Class="ColeHop.View.MiFeaturePage"
	xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
	xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
	Title="Mi Feature">

	<ScrollView>
		<VerticalStackLayout Padding="20" Spacing="20">

			<Label 
				Text="{Binding Resultado}"
				FontSize="16" />

			<Button 
				Text="Cargar datos"
				Command="{Binding CargarDatosCommand}"
				IsEnabled="{Binding IsBusy, Converter={StaticResource InvertedBoolConverter}}" />

		</VerticalStackLayout>
	</ScrollView>
</ContentPage>
```

#### 5. Registrar en DI
```csharp
// MauiProgram.cs
builder.Services.AddSingleton<IMiFeatureService, MiFeatureService>();
builder.Services.AddTransient<MiFeatureViewModel>();
builder.Services.AddTransient<MiFeaturePage>();
```

#### 6. Registrar Ruta de Navegación
```csharp
// AppShell.xaml.cs - RegisterRoutes()
Routing.RegisterRoute("mifeature", typeof(MiFeaturePage));
```

#### 7. Navegar desde otro ViewModel
```csharp
[RelayCommand]
private async Task GoToMiFeatureAsync()
{
	await Shell.Current.GoToAsync("mifeature");
}
```

### Debugging NFC

Para depurar el flujo de NFC:

1. **Habilitar logs detallados** en `NfcService.cs`
2. **Usar DNIe de prueba** con CAN conocido
3. **Verificar que NFC está habilitado** en el dispositivo
4. **Comprobar permisos** de NFC en AndroidManifest.xml
5. **Capturar excepciones** específicas de IsoDep
6. **Validar respuestas APDU** (SW1 SW2 = 90 00 para éxito)

### Testing

Actualmente el proyecto usa **datos simulados**:
- AuthService: Crea sesiones ficticias
- PickupService: Devuelve listas hardcodeadas
- No hay tests unitarios aún

**Para agregar tests:**
1. Crear proyecto `ColeHop.Tests`
2. Usar xUnit o NUnit
3. Mockear interfaces con Moq
4. Testear ViewModels y Services por separado

---

## 📝 Checklist para Nuevos Desarrolladores

Antes de empezar a programar, asegúrate de:

- [ ] Leer este documento completo
- [ ] Entender Clean Architecture y sus capas
- [ ] Familiarizarte con CommunityToolkit.Mvvm
- [ ] Revisar los ViewModels existentes como referencia
- [ ] Entender el patrón Observer en AppShell
- [ ] Conocer las directrices de diseño UI
- [ ] Configurar el entorno (.NET 10, Visual Studio 2026+)
- [ ] Clonar el repositorio y compilar sin errores
- [ ] Ejecutar la app en emulador/dispositivo Android

---

## 🚀 Estado Actual del Proyecto

### ✅ Completado

- Arquitectura Clean implementada
- Navegación dinámica con AppShell
- Autenticación básica con roles (simulada)
- Dashboards para Profesor y Tutor
- Lista de recogidas diarias (simulada)
- Integración NFC con Android (DNIe 3.0)
- Protocolo PACE implementado
- Lectura de archivos DNIe (DG1, DG2, DG11, SOD)
- Gestión de autorizaciones (UI simulada)
- Estandarización de código sin warnings
- Documentación completa del proyecto

### ⏳ Pendiente

- Backend real para autenticación
- Base de datos (SQLite/Realm)
- Conexión con API REST
- Tests unitarios y de integración
- Implementación completa iOS
- Validación real de SOD (firma digital)
- Historial completo de recogidas
- Notificaciones push
- Exportación de informes
- Panel de administración web

### 🐛 Conocidos Issues

- NfcScanViewModel: El flujo completo de verificación está pendiente de testing con DNIe real
- Los datos son todos simulados (no persisten entre sesiones)
- No hay manejo de errores de red (porque no hay red aún)
- La validación SOD es placeholder (falta implementación criptográfica real)

---

## 📧 Información del Proyecto

- **Nombre:** ColeHop
- **Repositorio:** https://github.com/cas1201/TFM_SAL_DNIe
- **Framework:** .NET 10 MAUI
- **Plataforma principal:** Android
- **Tipo:** Trabajo Fin de Máster (TFM)
- **Tecnología clave:** DNIe 3.0 + NFC

---

## 📄 Licencia y Uso

Este proyecto es un TFM (Trabajo Fin de Máster) académico. No está diseñado para uso en producción sin:
- Implementación completa del backend
- Validación criptográfica real
- Auditoría de seguridad
- Cumplimiento RGPD/LOPD
- Testing exhaustivo

---

**Última actualización:** 06/05/2026  
**Versión del documento:** 1.0  
**Autor:** Equipo ColeHop / TFM_SAL_DNIe

---

## 🎓 Conceptos Clave para Entender

### 1. DNIe 3.0
Documento Nacional de Identidad electrónico español que incorpora un chip NFC con:
- Datos personales (DG1)
- Fotografía del titular (DG2)
- Datos adicionales (DG11)
- Firma digital de los datos (SOD - Security Object Document)

### 2. PACE (Password Authenticated Connection Establishment)
Protocolo criptográfico que permite establecer un canal seguro con el DNIe usando el CAN (Card Access Number) como contraseña.

### 3. APDU (Application Protocol Data Unit)
Comandos de comunicación con tarjetas inteligentes siguiendo la norma ISO 7816-4.

### 4. ISO-DEP
Tecnología de comunicación NFC basada en ISO 14443-4 para tarjetas inteligentes.

### 5. Clean Architecture
Arquitectura en capas donde las dependencias apuntan hacia dentro (dominio). Las capas externas (UI, datos) dependen de las internas (dominio), nunca al revés.

### 6. MVVM (Model-View-ViewModel)
Patrón de diseño que separa:
- **Model:** Datos y lógica de negocio
- **View:** Interfaz de usuario (XAML)
- **ViewModel:** Intermediario que expone datos para la View

### 7. Shell Navigation (MAUI)
Sistema de navegación declarativo de MAUI que permite navegar entre páginas mediante rutas URI.

---

**¡Este documento debe ser tu referencia principal durante todo el desarrollo!**

Si algo no está claro o necesitas más detalles sobre alguna sección, consulta:
- Los documentos de estandarización en la raíz del proyecto
- El código existente como ejemplo de referencia
- La documentación oficial de .NET MAUI

**¡Bienvenido al proyecto ColeHop! 🎉**
