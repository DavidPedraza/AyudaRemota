# i18n (Internationalization) Implementation Guide

## Overview

RemoteCare soportará múltiples idiomas desde el inicio con un diseño que permite agregar idiomas fácilmente sin cambiar código.

**Idiomas iniciales**:
- Spanish (es) - Predeterminado
- English (en)
- Catalan (ca) - Para agregar después

---

## 1. Architecture

### Server-Side (C# Backend)

**Sistema de localización**:
- Archivos JSON con traducciones
- Servicio centralizado `ILocalizationService`
- Selección de idioma por: Header `Accept-Language` o parámetro de usuario
- DTOs contienen claves, no textos (el frontend traduce)

### Client-Side (Android)

**Strings resources**:
- `strings.xml` - Spanish (default)
- `strings-en.xml` - English
- `strings-ca.xml` - Catalan

---

## 2. Backend Implementation

### 2.1 Localization Resources

**Estructura de carpetas**:
```
RemoteCare.Api/
├── Localization/
│   ├── Resources/
│   │   ├── es.json
│   │   ├── en.json
│   │   ├── ca.json
│   │   └── README.md
│   ├── LocalizationService.cs
│   └── LocalizationExtensions.cs
```

### 2.2 Resource Files

**Localization/Resources/es.json**:
```json
{
  "common": {
    "error": "Error",
    "success": "Éxito",
    "cancel": "Cancelar",
    "save": "Guardar",
    "delete": "Eliminar",
    "yes": "Sí",
    "no": "No"
  },
  "auth": {
    "register_success": "Registro exitoso",
    "login_success": "Inicio de sesión exitoso",
    "invalid_credentials": "Email o contraseña incorrectos",
    "email_exists": "El correo ya existe",
    "password_weak": "La contraseña es demasiado débil",
    "token_expired": "Token expirado",
    "unauthorized": "No autorizado"
  },
  "device": {
    "registered": "Dispositivo registrado",
    "device_not_found": "Dispositivo no encontrado",
    "pairing_success": "Dispositivos emparejados exitosamente",
    "pairing_failed": "Fallo al emparejar dispositivos",
    "invalid_pairing_code": "Código de emparejamiento inválido",
    "code_expired": "El código ha expirado"
  },
  "session": {
    "started": "Sesión iniciada",
    "ended": "Sesión finalizada",
    "active": "Activa",
    "completed": "Completada",
    "error": "Error",
    "monitoring": "Monitoreando...",
    "user_disconnect": "Desconexión del usuario"
  },
  "validation": {
    "required": "Este campo es requerido",
    "invalid_email": "Formato de email inválido",
    "min_length": "Mínimo {0} caracteres",
    "max_length": "Máximo {0} caracteres"
  }
}
```

**Localization/Resources/en.json**:
```json
{
  "common": {
    "error": "Error",
    "success": "Success",
    "cancel": "Cancel",
    "save": "Save",
    "delete": "Delete",
    "yes": "Yes",
    "no": "No"
  },
  "auth": {
    "register_success": "Registration successful",
    "login_success": "Login successful",
    "invalid_credentials": "Invalid email or password",
    "email_exists": "Email already exists",
    "password_weak": "Password is too weak",
    "token_expired": "Token expired",
    "unauthorized": "Unauthorized"
  },
  "device": {
    "registered": "Device registered",
    "device_not_found": "Device not found",
    "pairing_success": "Devices paired successfully",
    "pairing_failed": "Failed to pair devices",
    "invalid_pairing_code": "Invalid pairing code",
    "code_expired": "Code has expired"
  },
  "session": {
    "started": "Session started",
    "ended": "Session ended",
    "active": "Active",
    "completed": "Completed",
    "error": "Error",
    "monitoring": "Monitoring...",
    "user_disconnect": "User disconnected"
  },
  "validation": {
    "required": "This field is required",
    "invalid_email": "Invalid email format",
    "min_length": "Minimum {0} characters",
    "max_length": "Maximum {0} characters"
  }
}
```

### 2.3 LocalizationService

**Localization/LocalizationService.cs**:
```csharp
public interface ILocalizationService
{
    string Get(string key, string culture = "es");
    string Get(string key, string[] args, string culture = "es");
    bool TryGet(string key, out string value, string culture = "es");
}

public class LocalizationService : ILocalizationService
{
    private readonly Dictionary<string, Dictionary<string, object>> _resources;
    private readonly ILogger<LocalizationService> _logger;
    private readonly string _defaultCulture = "es";

    public LocalizationService(ILogger<LocalizationService> logger)
    {
        _logger = logger;
        _resources = new Dictionary<string, Dictionary<string, object>>();
        LoadResources();
    }

    private void LoadResources()
    {
        var cultures = new[] { "es", "en", "ca" };
        var assembly = GetType().Assembly;

        foreach (var culture in cultures)
        {
            try
            {
                var resourceName = $"RemoteCare.Api.Localization.Resources.{culture}.json";
                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                    {
                        _logger.LogWarning($"Resource file not found: {resourceName}");
                        continue;
                    }

                    using (var reader = new StreamReader(stream))
                    {
                        var json = reader.ReadToEnd();
                        var data = JsonDocument.Parse(json);
                        _resources[culture] = FlattenJson(data.RootElement);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading culture {culture}: {ex.Message}");
            }
        }

        _logger.LogInformation($"Loaded localization resources for: {string.Join(", ", _resources.Keys)}");
    }

    // Convertir JSON anidado a diccionario plano
    // "auth.login_success" -> valor
    private Dictionary<string, object> FlattenJson(JsonElement element, string prefix = "")
    {
        var result = new Dictionary<string, object>();

        foreach (var property in element.EnumerateObject())
        {
            var key = string.IsNullOrEmpty(prefix)
                ? property.Name
                : $"{prefix}.{property.Name}";

            if (property.Value.ValueKind == JsonValueKind.Object)
            {
                var nested = FlattenJson(property.Value, key);
                foreach (var kvp in nested)
                {
                    result[kvp.Key] = kvp.Value;
                }
            }
            else
            {
                result[key] = property.Value.GetString();
            }
        }

        return result;
    }

    public string Get(string key, string culture = "es")
    {
        if (!_resources.ContainsKey(culture))
            culture = _defaultCulture;

        if (_resources[culture].TryGetValue(key, out var value))
        {
            return value?.ToString() ?? key;
        }

        _logger.LogWarning($"Localization key not found: {key} for culture: {culture}");
        return key;
    }

    public string Get(string key, string[] args, string culture = "es")
    {
        var value = Get(key, culture);

        if (args != null && args.Length > 0)
        {
            for (int i = 0; i < args.Length; i++)
            {
                value = value.Replace($"{{{i}}}", args[i]);
            }
        }

        return value;
    }

    public bool TryGet(string key, out string value, string culture = "es")
    {
        if (!_resources.ContainsKey(culture))
            culture = _defaultCulture;

        if (_resources[culture].TryGetValue(key, out var result))
        {
            value = result?.ToString() ?? string.Empty;
            return true;
        }

        value = null;
        return false;
    }
}
```

### 2.4 Localization Extensions

**Localization/LocalizationExtensions.cs**:
```csharp
public static class LocalizationExtensions
{
    public static IServiceCollection AddLocalization(
        this IServiceCollection services)
    {
        services.AddSingleton<ILocalizationService, LocalizationService>();
        return services;
    }

    // Extensión para obtener cultura de HTTP context
    public static string GetCulture(this HttpContext context)
    {
        // 1. Check query parameter
        if (context.Request.Query.TryGetValue("culture", out var cultureParm))
            return cultureParm.ToString();

        // 2. Check user preference (si autenticado)
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            // Obtener cultura del usuario desde BD
            // (opcional, agregar después)
        }

        // 3. Check Accept-Language header
        var acceptLanguage = context.Request.Headers["Accept-Language"].ToString();
        if (!string.IsNullOrEmpty(acceptLanguage))
        {
            var culture = acceptLanguage.Split(',')[0].Split('-')[0];
            if (new[] { "es", "en", "ca" }.Contains(culture))
                return culture;
        }

        // 4. Default
        return "es";
    }
}
```

### 2.5 Register in Program.cs

```csharp
// Program.cs
builder.Services.AddLocalization();

var app = builder.Build();

// Request localization middleware (opcional)
app.Use(async (context, next) =>
{
    var culture = context.GetCulture();
    context.Items["Culture"] = culture;
    await next();
});

app.Run();
```

### 2.6 Using Localization in Services

```csharp
public class AuthService
{
    private readonly IUserRepository _userRepository;
    private readonly ILocalizationService _localizationService;

    public AuthService(
        IUserRepository userRepository,
        ILocalizationService localizationService)
    {
        _userRepository = userRepository;
        _localizationService = localizationService;
    }

    public async Task<AuthResponse> LoginAsync(
        LoginRequest request,
        string culture = "es")
    {
        var user = await _userRepository.GetByEmailAsync(request.Email);

        if (user == null)
        {
            throw new AuthenticationException(
                _localizationService.Get("auth.invalid_credentials", culture));
        }

        // ... rest of logic
    }
}
```

### 2.7 Error Response with Localization

```csharp
// En middleware de error handling
app.UseExceptionHandler((app) =>
{
    app.Run(async context =>
    {
        var exceptionHandlerPathFeature =
            context.Features.GetRequiredFeature<IExceptionHandlerPathFeature>();

        var localizationService = context.RequestServices
            .GetRequiredService<ILocalizationService>();

        var culture = context.GetCulture();

        var response = new ErrorResponse
        {
            Error = localizationService.Get("common.error", culture),
            Message = exceptionHandlerPathFeature.Error.Message,
            Timestamp = DateTime.UtcNow
        };

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsJsonAsync(response);
    });
});
```

---

## 3. Frontend Implementation (Android)

### 3.1 Strings Resources - Spanish

**android-senior/app/src/main/res/values/strings.xml**:
```xml
<?xml version="1.0" encoding="utf-8"?>
<resources>
    <string name="app_name">RemoteCare Senior</string>

    <!-- Common -->
    <string name="error">Error</string>
    <string name="success">Éxito</string>
    <string name="cancel">Cancelar</string>
    <string name="save">Guardar</string>
    <string name="ok">Aceptar</string>

    <!-- Auth -->
    <string name="login_title">Iniciar Sesión</string>
    <string name="register_title">Registrarse</string>
    <string name="email_hint">Correo electrónico</string>
    <string name="password_hint">Contraseña</string>
    <string name="full_name_hint">Nombre completo</string>
    <string name="login_button">Iniciar Sesión</string>
    <string name="register_button">Registrarse</string>
    <string name="login_success">¡Bienvenido!</string>
    <string name="register_success">Registro exitoso</string>
    <string name="invalid_credentials">Email o contraseña incorrectos</string>

    <!-- Device -->
    <string name="device_pairing">Emparejamiento de Dispositivos</string>
    <string name="generate_qr">Generar Código QR</string>
    <string name="scan_qr">Escanear QR</string>
    <string name="pairing_success">Dispositivos emparejados exitosamente</string>
    <string name="monitoring">Monitoreando...</string>

    <!-- Session -->
    <string name="active">Activa</string>
    <string name="ended">Finalizada</string>
    <string name="disconnect">Desconectar</string>
</resources>
```

### 3.2 Strings Resources - English

**android-senior/app/src/main/res/values-en/strings.xml**:
```xml
<?xml version="1.0" encoding="utf-8"?>
<resources>
    <string name="app_name">RemoteCare Senior</string>

    <!-- Common -->
    <string name="error">Error</string>
    <string name="success">Success</string>
    <string name="cancel">Cancel</string>
    <string name="save">Save</string>
    <string name="ok">OK</string>

    <!-- Auth -->
    <string name="login_title">Login</string>
    <string name="register_title">Register</string>
    <string name="email_hint">Email</string>
    <string name="password_hint">Password</string>
    <string name="full_name_hint">Full Name</string>
    <string name="login_button">Login</string>
    <string name="register_button">Register</string>
    <string name="login_success">Welcome!</string>
    <string name="register_success">Registration successful</string>
    <string name="invalid_credentials">Invalid email or password</string>

    <!-- Device -->
    <string name="device_pairing">Device Pairing</string>
    <string name="generate_qr">Generate QR Code</string>
    <string name="scan_qr">Scan QR</string>
    <string name="pairing_success">Devices paired successfully</string>
    <string name="monitoring">Monitoring...</string>

    <!-- Session -->
    <string name="active">Active</string>
    <string name="ended">Ended</string>
    <string name="disconnect">Disconnect</string>
</resources>
```

### 3.3 Using Strings in Kotlin

```kotlin
// En Activities/Fragments
val appName = getString(R.string.app_name)
val loginTitle = getString(R.string.login_title)
val errorMsg = getString(R.string.invalid_credentials)

// Android automáticamente selecciona el idioma basado en locale del sistema
// Si sistema está en Español → usa strings.xml
// Si sistema está en English → usa strings-en.xml
```

### 3.4 Programmatic Language Change (Opcional)

```kotlin
// Para permitir cambio de idioma dentro de la app
fun setAppLanguage(context: Context, language: String) {
    val locale = when (language) {
        "en" -> Locale("en")
        "ca" -> Locale("ca")
        else -> Locale("es")
    }

    val config = context.resources.configuration
    config.setLocale(locale)
    context.createConfigurationContext(config)

    // Guardar preferencia
    val sharedPref = context.getSharedPreferences("app_prefs", Context.MODE_PRIVATE)
    sharedPref.edit().putString("language", language).apply()

    // Reiniciar activity para aplicar cambios
    // (usar LiveData y recompose en Jetpack Compose)
}
```

---

## 4. Adding a New Language

### Para agregar Catalan (ca):

#### 1. Backend
```bash
# Crear archivo
touch src/RemoteCare.Api/Localization/Resources/ca.json

# Agregar traducciones (copiar es.json y traducir)
```

#### 2. Frontend (Android)
```bash
# Crear carpeta
mkdir -p android-senior/app/src/main/res/values-ca

# Crear archivo strings.xml con traducciones
```

#### 3. Registro en sistema (Backend)
```csharp
// En LocalizationService.cs
private readonly string[] _supportedCultures = { "es", "en", "ca" };
```

---

## 5. Guidelines for Translations

### 5.1 Translation Keys

**Bueno**:
- `auth.login_success` - Claro y específico
- `validation.required` - Reutilizable
- `device.pairing_failed` - Contexto

**Malo**:
- `msg1`, `msg2` - Sin contexto
- `error_occurred` - Demasiado genérico

### 5.2 Translation Rules

- Mantener keys en snake_case
- Evitar cambios frecuentes en keys (back-compat)
- Documentar new keys en README.md
- Traducir TODAS las UI strings
- NO traducir IDs técnicos ni nombres propios

### 5.3 Handling Variables

```json
{
  "validation": {
    "min_length": "Mínimo {0} caracteres"
  }
}
```

**Usage**:
```csharp
var message = _localizationService.Get("validation.min_length", new[] { "8" });
// Output: "Mínimo 8 caracteres"
```

---

## 6. Testing Localizations

### 6.1 Unit Tests

```csharp
public class LocalizationServiceTests
{
    private readonly LocalizationService _service;

    public LocalizationServiceTests()
    {
        var logger = new Mock<ILogger<LocalizationService>>();
        _service = new LocalizationService(logger.Object);
    }

    [Theory]
    [InlineData("es")]
    [InlineData("en")]
    [InlineData("ca")]
    public void Get_ExistingKey_ReturnsTranslation(string culture)
    {
        var result = _service.Get("common.error", culture);
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public void Get_WithPlaceholders_ReplacesVariables()
    {
        var result = _service.Get(
            "validation.min_length",
            new[] { "8" },
            "es");

        Assert.Contains("8", result);
    }
}
```

### 6.2 Android Tests

```kotlin
class LocalizationTest {
    @Test
    fun verifyStringsLoaded() {
        val context = InstrumentationRegistry.getInstrumentation().targetContext
        val appName = context.getString(R.string.app_name)
        assertNotNull(appName)
        assertTrue(appName.isNotEmpty())
    }
}
```

---

## 7. Localization Checklist

- [ ] Todos los strings en archivos de recursos (no hardcoded)
- [ ] Keys organizadas lógicamente (común, auth, device, etc.)
- [ ] Archivos de idiomas sincronizados (mismo número de keys)
- [ ] Documentación de nuevas keys
- [ ] Tests para localization service
- [ ] Validación de placeholders
- [ ] Soporte para plurales (si aplica)
- [ ] Testing en múltiples idiomas
- [ ] Revisión por hablantes nativos (si es posible)

---

## 8. Common i18n Patterns

### Plurals (Future Enhancement)
```json
{
  "session.frame_count": {
    "one": "1 frame capturado",
    "other": "{0} frames capturados"
  }
}
```

### Date/Time Formatting
```csharp
public string FormatDateTime(DateTime dateTime, string culture)
{
    var cultureInfo = new CultureInfo(culture);
    return dateTime.ToString("g", cultureInfo);
    // es: 5/3/2024 10:30
    // en: 3/5/2024 10:30 AM
}
```

### Number Formatting
```csharp
public string FormatNumber(double number, string culture)
{
    var cultureInfo = new CultureInfo(culture);
    return number.ToString("N2", cultureInfo);
    // es: 1.234,56
    // en: 1,234.56
}
```

---

## References

- Microsoft i18n Docs: https://docs.microsoft.com/en-us/aspnet/core/fundamentals/localization
- Android Localization: https://developer.android.com/guide/topics/resources/localization
- Unicode CLDR: https://cldr.unicode.org/
