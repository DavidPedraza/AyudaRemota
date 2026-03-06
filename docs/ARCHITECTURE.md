# RemoteCare App - Arquitectura Detallada

## 1. Visión General

**RemoteCare** es una aplicación de soporte remoto que permite a un usuario "Soporte" monitorear remotamente la pantalla de un usuario "Senior" a través de una conexión segura iniciada mediante código QR.

### Actores
- **Usuario Senior**: Propietario del dispositivo monitoreado
- **Usuario Soporte**: Persona que ayuda a distancia

### Flujo principal
1. Senior abre app → genera código QR único
2. Soporte lee el QR → ambos dispositivos se emparejan
3. Se inicia sesión de monitoreo cifrada
4. Soporte ve pantalla del Senior en tiempo real

---

## 2. Arquitectura por Capas

```
┌─────────────────────────────────────────────────────┐
│         Android Apps (Kotlin)                       │
│   ┌──────────────┐          ┌──────────────┐       │
│   │ Senior App   │          │ Support App  │       │
│   └──────────────┘          └──────────────┘       │
└────────────┬──────────────────────────┬─────────────┘
             │                          │
             │     HTTPS + WSS          │
             └──────────┬───────────────┘
                        │
┌───────────────────────┴────────────────────────────┐
│    ASP.NET Core 8 API                              │
├────────────────────────────────────────────────────┤
│                                                    │
│  ┌────────────────────────────────────────────┐  │
│  │  Controllers Layer                          │  │
│  │  - AuthController                          │  │
│  │  - DeviceController                        │  │
│  │  - SessionController                       │  │
│  │  - ScreenStreamHub (SignalR)                │  │
│  └────────────────────────────────────────────┘  │
│                                                    │
│  ┌────────────────────────────────────────────┐  │
│  │  Application/Business Layer                │  │
│  │  - AuthService                             │  │
│  │  - DevicePairingService                    │  │
│  │  - SessionService                          │  │
│  │  - QrGeneratorService                      │  │
│  │  - EncryptionService                       │  │
│  │  - LocalizationService                     │  │
│  └────────────────────────────────────────────┘  │
│                                                    │
│  ┌────────────────────────────────────────────┐  │
│  │  Data Layer                                 │  │
│  │  - IUserRepository                         │  │
│  │  - IDeviceRepository                       │  │
│  │  - ISessionRepository                      │  │
│  │  - Repository Pattern Implementation       │  │
│  └────────────────────────────────────────────┘  │
│                                                    │
│  ┌────────────────────────────────────────────┐  │
│  │  Database (EF Core)                         │  │
│  │  - RemoteCareContext                       │  │
│  │  - Migrations                              │  │
│  └────────────────────────────────────────────┘  │
└────────────────────────────────────────────────────┘
             │
             │
┌────────────┴─────────────────────────────────────┐
│    SQLite / SQL Server Express                    │
│    - Users                                        │
│    - Devices                                      │
│    - Sessions                                     │
│    - AuditLogs                                    │
└───────────────────────────────────────────────────┘
```

---

## 3. Estructura de Carpetas

```
RemoteCare/
│
├── src/
│   ├── RemoteCare.Api/
│   │   ├── Program.cs                 # Entry point
│   │   ├── appsettings.json          # Config base
│   │   ├── appsettings.Development.json
│   │   ├── appsettings.Production.json
│   │   │
│   │   ├── Controllers/              # API endpoints
│   │   │   ├── AuthController.cs
│   │   │   ├── DeviceController.cs
│   │   │   ├── SessionController.cs
│   │   │   ├── HealthController.cs
│   │   │   └── AdminController.cs
│   │   │
│   │   ├── Hubs/                     # SignalR hubs
│   │   │   └── ScreenStreamHub.cs
│   │   │
│   │   ├── Services/                 # Business logic
│   │   │   ├── IAuthService.cs
│   │   │   ├── AuthService.cs
│   │   │   ├── IDevicePairingService.cs
│   │   │   ├── DevicePairingService.cs
│   │   │   ├── ISessionService.cs
│   │   │   ├── SessionService.cs
│   │   │   ├── IQrGeneratorService.cs
│   │   │   ├── QrGeneratorService.cs
│   │   │   ├── IEncryptionService.cs
│   │   │   ├── EncryptionService.cs
│   │   │   ├── ILocalizationService.cs
│   │   │   └── LocalizationService.cs
│   │   │
│   │   ├── Data/                     # Data access layer
│   │   │   ├── RemoteCareContext.cs
│   │   │   ├── Repositories/
│   │   │   │   ├── IRepository.cs
│   │   │   │   ├── Repository.cs
│   │   │   │   ├── IUserRepository.cs
│   │   │   │   ├── UserRepository.cs
│   │   │   │   ├── IDeviceRepository.cs
│   │   │   │   ├── DeviceRepository.cs
│   │   │   │   ├── ISessionRepository.cs
│   │   │   │   └── SessionRepository.cs
│   │   │   └── Migrations/           # EF Core migrations
│   │   │       ├── 20240305_Initial.cs
│   │   │       └── RemoteCareContextModelSnapshot.cs
│   │   │
│   │   ├── Models/                   # Domain models
│   │   │   ├── User.cs
│   │   │   ├── Device.cs
│   │   │   ├── Session.cs
│   │   │   ├── AuditLog.cs
│   │   │   └── Enums/
│   │   │       ├── DeviceRole.cs
│   │   │       ├── SessionStatus.cs
│   │   │       └── AuditAction.cs
│   │   │
│   │   ├── Dtos/                     # Data transfer objects
│   │   │   ├── Auth/
│   │   │   │   ├── RegisterRequest.cs
│   │   │   │   ├── LoginRequest.cs
│   │   │   │   ├── AuthResponse.cs
│   │   │   │   └── RefreshTokenRequest.cs
│   │   │   ├── Device/
│   │   │   │   ├── RegisterDeviceRequest.cs
│   │   │   │   ├── PairDeviceRequest.cs
│   │   │   │   └── DeviceDto.cs
│   │   │   └── Session/
│   │   │       ├── SessionDto.cs
│   │   │       └── SessionHistoryDto.cs
│   │   │
│   │   ├── Middleware/               # Custom middleware
│   │   │   ├── JwtMiddleware.cs
│   │   │   ├── ErrorHandlingMiddleware.cs
│   │   │   └── LoggingMiddleware.cs
│   │   │
│   │   ├── Localization/             # i18n resources
│   │   │   ├── Resources/
│   │   │   │   ├── es.json
│   │   │   │   ├── en.json
│   │   │   │   └── ca.json
│   │   │   └── LocalizationExtensions.cs
│   │   │
│   │   ├── Utilities/                # Helper functions
│   │   │   ├── JwtTokenGenerator.cs
│   │   │   ├── PasswordHasher.cs
│   │   │   └── ValidationHelper.cs
│   │   │
│   │   ├── Exceptions/               # Custom exceptions
│   │   │   ├── ValidationException.cs
│   │   │   ├── AuthenticationException.cs
│   │   │   ├── PairingException.cs
│   │   │   └── NotFoundException.cs
│   │   │
│   │   └── RemoteCare.Api.csproj
│   │
│   └── RemoteCare.Common/           # Shared utilities (opcional)
│       ├── Constants/
│       │   ├── ValidationRules.cs
│       │   ├── ErrorMessages.cs
│       │   └── ApiRoutes.cs
│       └── RemoteCare.Common.csproj
│
├── tests/
│   └── RemoteCare.Api.Tests/
│       ├── Unit/
│       │   ├── Services/
│       │   │   ├── AuthServiceTests.cs
│       │   │   └── DevicePairingServiceTests.cs
│       │   └── Utilities/
│       ├── Integration/
│       │   ├── AuthControllerTests.cs
│       │   └── DeviceControllerTests.cs
│       └── RemoteCare.Api.Tests.csproj
│
├── android-senior/
│   ├── app/src/main/
│   │   ├── java/com/remotecare/senior/
│   │   │   ├── MainActivity.kt
│   │   │   ├── models/
│   │   │   ├── services/
│   │   │   │   ├── ScreenCaptureService.kt
│   │   │   │   ├── WebSocketService.kt
│   │   │   │   └── QrReaderService.kt
│   │   │   ├── ui/
│   │   │   │   ├── screens/
│   │   │   │   ├── components/
│   │   │   │   └── theme/
│   │   │   └── utils/
│   │   └── res/
│   │       ├── values/
│   │       │   ├── strings.xml (Spanish)
│   │       │   ├── strings-en.xml
│   │       │   └── colors.xml
│   │       └── layout/
│   └── build.gradle
│
├── android-support/
│   ├── app/src/main/
│   │   ├── java/com/remotecare/support/
│   │   │   ├── MainActivity.kt
│   │   │   ├── models/
│   │   │   ├── services/
│   │   │   │   ├── WebSocketService.kt
│   │   │   │   └── QrScannerService.kt
│   │   │   ├── ui/
│   │   │   │   ├── screens/
│   │   │   │   ├── components/
│   │   │   │   └── theme/
│   │   │   └── utils/
│   │   └── res/
│   └── build.gradle
│
├── docs/
│   ├── ARCHITECTURE.md               # Este archivo
│   ├── API_SPECIFICATION.md
│   ├── DATABASE_SCHEMA.md
│   ├── SECURITY_CONSIDERATIONS.md
│   ├── DEVELOPMENT_GUIDE.md
│   ├── i18n_IMPLEMENTATION.md
│   ├── DEPLOYMENT_GUIDE.md
│   ├── TROUBLESHOOTING.md
│   └── CONTRIBUTING.md
│
├── .gitignore
├── README.md
├── CLAUDE.md                        # Instrucciones para Claude Code
├── RemoteCare.sln
└── .github/
    └── workflows/                   # CI/CD (opcional para después)
        └── build.yml
```

---

## 4. Flujo de Comunicación

### 4.1 Autenticación
```
┌────────────┐                                    ┌─────────────┐
│   Mobile   │                                    │   Backend   │
└────────────┘                                    └─────────────┘
     │                                                   │
     │──────── POST /api/auth/register ────────────────>│
     │         {email, password, name}                  │
     │                                                   │ Validar
     │                                                   │ HashPassword
     │                                                   │ CreateUser
     │<───────── 200 OK {jwt, refreshToken} ──────────│
     │                                                   │
     │──────── POST /api/auth/login ──────────────────>│
     │         {email, password}                        │
     │                                                   │ ValidatePassword
     │                                                   │ GenerateJWT
     │<───────── 200 OK {jwt, refreshToken} ──────────│
```

### 4.2 Emparejamiento de dispositivos (QR)
```
┌────────────────────┐              ┌─────────────┐           ┌────────────────────┐
│   Senior Device    │              │   Backend   │           │  Support Device    │
└────────────────────┘              └─────────────┘           └────────────────────┘
     │                                    │                         │
     │─── POST /api/device/qr ────────────>│                        │
     │     {deviceId, userId}              │                        │
     │                                     │ GenerateUniqueCode     │
     │                                     │ StoreInRedis (5min)    │
     │<──── 200 OK {qrCode, pairingCode}──│                        │
     │                                     │                         │
     │   (Display QR)                      │                        │
     │                                     │        (Scan QR)       │
     │                                     │  POST /api/device/pair |
     │                                     │  {pairingCode, uid}   <──
     │                                     │ VerifyCode             │
     │                                     │ CreateSession          │
     │                                     │ LinkDevices            │
     │<──── WebSocket opened for session ─────────────────────────>│
     │                                     │        WSS ready       │
```

### 4.3 Transmisión de pantalla (en tiempo real)
```
Senior Device (ScreenCapture)        Backend (SignalR Hub)        Support Device
         │                                    │                           │
         │─ Connect to WSS Hub ─────────────>│                           │
         │                                    │<────── Connect ──────────│
         │                                    │                           │
         │─ Send Frame 1 (base64) ──────────>│                           │
         │                                    │─────── Frame 1 ────────>│
         │                                    │       (broadcast to       │
         │─ Send Frame 2 (base64) ──────────>│        group)             │
         │                                    │─────── Frame 2 ────────>│
         │  [30 FPS = 33ms/frame]             │                          │
         │                                    │<────── ACK ──────────────│
         │                                    │                           │
         │─ Disconnect WSS ──────────────────>│                           │
         │                                    │──── Disconnect ────────>│
```

---

## 5. Tecnologías y Dependencias

### Backend (.NET)
```
- ASP.NET Core 8.0
- Entity Framework Core 8.0
- SignalR (WebSockets)
- JWT Bearer Authentication
- QRCoder (Generación QR)
- System.Security.Cryptography (Encriptación)
- Serilog (Logging)
- FluentValidation (Validaciones)
```

### Android
```
- Kotlin 1.9+
- Jetpack Compose (UI)
- AndroidX & lifecycle
- Okhttp3 + Retrofit2 (HTTP)
- OkHttp WebSocket
- ML Kit Vision (QR scanning)
- Android ScreenCapture API
- Material 3 Design
```

### Base de datos
```
- SQLite (Desarrollo)
- SQL Server Express 2022 (Producción)
```

---

## 6. Patrones de Diseño

### 6.1 Repository Pattern
```csharp
public interface IRepository<T> where T : class
{
    Task<T> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(int id);
}

public class Repository<T> : IRepository<T> where T : class
{
    protected readonly RemoteCareContext _context;
    public Repository(RemoteCareContext context) => _context = context;
    // Implementation...
}
```

### 6.2 Service Layer
```csharp
public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<AuthResponse> RefreshTokenAsync(string refreshToken);
}

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IEncryptionService _encryptionService;
    // Implementation...
}
```

### 6.3 Dependency Injection
```csharp
// Program.cs
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IDevicePairingService, DevicePairingService>();
builder.Services.AddScoped<ISessionService, SessionService>();
builder.Services.AddSingleton<ILocalizationService, LocalizationService>();
```

---

## 7. Seguridad

### 7.1 Autenticación
- JWT con HS256 o RS256
- Refresh tokens (almacenados en BD)
- Token expiration: 15 min (access), 7 días (refresh)

### 7.2 Encriptación
- TLS 1.3 para comunicación en tránsito
- AES-256-GCM para datos sensibles en reposo
- Passwords hasheadas con bcrypt (12 rounds)

### 7.3 Autorización
- Role-based access control (RBAC)
- Roles: User, Admin
- Validación de Device ownership en endpoints

### 7.4 Validación
- Input validation en controllers
- Rate limiting en endpoints de auth
- CSRF protection si es necesario

---

## 8. Escalabilidad y Rendimiento

### 8.1 Caché
- Redis (opcional para producción)
- In-memory cache para QR codes (5 min TTL)

### 8.2 Base de datos
- Índices en userId, deviceId, sessionId
- Lazy loading deshabilitado (usar explicit include)
- Pagination en listados

### 8.3 Async/Await
- Controllers async
- Services async
- Repository queries async

---

## 9. Monitoreo y Logging

### 9.1 Logging
- Serilog con structured logging
- Niveles: Debug, Information, Warning, Error, Fatal
- Información sensible NOT logged

### 9.2 Auditing
- AuditLog table para acciones críticas
- Quién, Qué, Cuándo, Resultado
- Retención: 1 año

---

## 10. Ciclo de Desarrollo

```
Sprint 1 (2 semanas)
├── Setup inicial
├── Autenticación (Register/Login)
├── Device registration
└── QR generation

Sprint 2 (2 semanas)
├── Device pairing
├── Session management
├── SignalR hub
└── Android Senior app (basic)

Sprint 3 (2 semanas)
├── Screen capture Android
├── WebSocket streaming
└── Android Support app (viewer)

Sprint 4 (1 semana)
├── Optimization
├── Security review
├── Testing
└── Documentation
```

---

## 11. Próximos Documentos

Para entender completamente el proyecto, lee en orden:

1. **API_SPECIFICATION.md** - Endpoints detallados
2. **DATABASE_SCHEMA.md** - Diseño de tablas
3. **SECURITY_CONSIDERATIONS.md** - Detalles de seguridad
4. **DEVELOPMENT_GUIDE.md** - Cómo empezar a codificar
5. **i18n_IMPLEMENTATION.md** - Cómo agregar idiomas
6. **DEPLOYMENT_GUIDE.md** - Cómo desplegar
