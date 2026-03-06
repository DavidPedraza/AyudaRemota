# RemoteCare Database Schema

## Overview

Database design para RemoteCare usando Entity Framework Core con SQLite (desarrollo) y SQL Server (producción).

---

## 1. Entity-Relationship Diagram

```
┌─────────────┐
│   Users     │
├─────────────┤
│ id (PK)     │
│ email       │
│ firstName   │
│ lastName    │
│ password    │
│ role        │◄────────┐
│ createdAt   │         │
│ updatedAt   │         │
│ isActive    │         │
└─────────────┘         │
      │                 │
      │ 1:N             │
      │                 │
      ├────────────────►┌──────────────┐
      │                 │   Devices    │
      │                 ├──────────────┤
      │                 │ id (PK)      │
      │                 │ deviceId     │ (unique)
      │                 │ deviceName   │
      │                 │ userId (FK)  │
      │                 │ role         │
      │                 │ osVersion    │
      │                 │ isActive     │
      │                 │ registeredAt │
      │                 │ lastSeen     │
      │                 │ createdAt    │
      │                 └──────────────┘
      │                        │
      │                        │ N:N (Many-to-Many)
      │                        │
      │                 ┌──────────────┐
      │                 │   Sessions   │
      │                 ├──────────────┤
      │                 │ id (PK)      │
      │                 │ sessionId    │ (unique)
      │                 │ seniorDevId  │ (FK)
      │                 │ supportDevId │ (FK)
      │                 │ status       │
      │                 │ startedAt    │
      │                 │ endedAt      │
      │                 │ createdAt    │
      │                 └──────────────┘
      │
      └────────────────►┌──────────────┐
                        │  AuditLogs   │
                        ├──────────────┤
                        │ id (PK)      │
                        │ userId (FK)  │
                        │ action       │
                        │ resource     │
                        │ resourceId   │
                        │ result       │
                        │ timestamp    │
                        └──────────────┘
```

---

## 2. Table Definitions

### 2.1 Users Table

```sql
CREATE TABLE Users (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Email NVARCHAR(255) NOT NULL UNIQUE,
    FirstName NVARCHAR(100) NOT NULL,
    LastName NVARCHAR(100) NOT NULL,
    PasswordHash NVARCHAR(255) NOT NULL,
    Role NVARCHAR(50) NOT NULL DEFAULT 'Senior',  -- Senior, Support, Admin
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    LastLogin DATETIME2 NULL,

    -- Índices
    INDEX idx_email UNIQUE (Email),
    INDEX idx_role (Role),
    INDEX idx_createdAt (CreatedAt)
);
```

**C# Entity**:
```csharp
public class User
{
    public int Id { get; set; }
    public string Email { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string PasswordHash { get; set; }
    public string Role { get; set; } // Senior, Support, Admin
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? LastLogin { get; set; }

    // Navigation properties
    public ICollection<Device> Devices { get; set; } = new List<Device>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
```

---

### 2.2 Devices Table

```sql
CREATE TABLE Devices (
    Id INT PRIMARY KEY IDENTITY(1,1),
    DeviceId NVARCHAR(255) NOT NULL UNIQUE,  -- UUID from Android
    DeviceName NVARCHAR(255) NOT NULL,
    UserId INT NOT NULL,
    Role NVARCHAR(50) NOT NULL,  -- Senior, Support
    OsVersion NVARCHAR(50),
    Manufacturer NVARCHAR(100),
    Model NVARCHAR(100),
    IsActive BIT NOT NULL DEFAULT 1,
    RegisteredAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    LastSeen DATETIME2 NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

    -- Foreign keys
    CONSTRAINT fk_devices_users
        FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,

    -- Índices
    INDEX idx_deviceId UNIQUE (DeviceId),
    INDEX idx_userId (UserId),
    INDEX idx_role (Role),
    INDEX idx_isActive (IsActive)
);
```

**C# Entity**:
```csharp
public class Device
{
    public int Id { get; set; }
    public string DeviceId { get; set; }  // UUID
    public string DeviceName { get; set; }
    public int UserId { get; set; }
    public string Role { get; set; }  // Senior, Support
    public string OsVersion { get; set; }
    public string Manufacturer { get; set; }
    public string Model { get; set; }
    public bool IsActive { get; set; }
    public DateTime RegisteredAt { get; set; }
    public DateTime? LastSeen { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public User User { get; set; }
    public ICollection<Session> SessionsAsSenior { get; set; } = new List<Session>();
    public ICollection<Session> SessionsAsSupport { get; set; } = new List<Session>();
}
```

---

### 2.3 Sessions Table

```sql
CREATE TABLE Sessions (
    Id INT PRIMARY KEY IDENTITY(1,1),
    SessionId NVARCHAR(255) NOT NULL UNIQUE,  -- UUID
    SeniorDeviceId INT NOT NULL,
    SupportDeviceId INT NOT NULL,
    Status NVARCHAR(50) NOT NULL DEFAULT 'Active',  -- Active, Completed, Disconnected, Error
    StartedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    EndedAt DATETIME2 NULL,

    -- Statistics
    FramesSent INT DEFAULT 0,
    FramesReceived INT DEFAULT 0,
    BytesTransferred BIGINT DEFAULT 0,
    AverageLatencyMs INT DEFAULT 0,

    -- Metadata
    IsEncrypted BIT NOT NULL DEFAULT 1,
    EndReason NVARCHAR(255) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

    -- Foreign keys
    CONSTRAINT fk_sessions_senior
        FOREIGN KEY (SeniorDeviceId) REFERENCES Devices(Id) ON DELETE CASCADE,
    CONSTRAINT fk_sessions_support
        FOREIGN KEY (SupportDeviceId) REFERENCES Devices(Id) ON DELETE CASCADE,

    -- Índices
    INDEX idx_sessionId UNIQUE (SessionId),
    INDEX idx_seniorDeviceId (SeniorDeviceId),
    INDEX idx_supportDeviceId (SupportDeviceId),
    INDEX idx_status (Status),
    INDEX idx_startedAt (StartedAt),
    INDEX idx_composite (SeniorDeviceId, SupportDeviceId, Status)
);
```

**C# Entity**:
```csharp
public class Session
{
    public int Id { get; set; }
    public string SessionId { get; set; }  // UUID
    public int SeniorDeviceId { get; set; }
    public int SupportDeviceId { get; set; }
    public string Status { get; set; }  // Active, Completed, Disconnected, Error
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }

    // Statistics
    public int FramesSent { get; set; }
    public int FramesReceived { get; set; }
    public long BytesTransferred { get; set; }
    public int AverageLatencyMs { get; set; }

    // Metadata
    public bool IsEncrypted { get; set; }
    public string EndReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public Device SeniorDevice { get; set; }
    public Device SupportDevice { get; set; }
}
```

---

### 2.4 RefreshTokens Table

```sql
CREATE TABLE RefreshTokens (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Token NVARCHAR(500) NOT NULL UNIQUE,
    UserId INT NOT NULL,
    ExpiresAt DATETIME2 NOT NULL,
    IsRevoked BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT fk_refreshTokens_users
        FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,

    INDEX idx_token (Token),
    INDEX idx_userId (UserId),
    INDEX idx_expiresAt (ExpiresAt)
);
```

**C# Entity**:
```csharp
public class RefreshToken
{
    public int Id { get; set; }
    public string Token { get; set; }
    public int UserId { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }
    public DateTime CreatedAt { get; set; }

    public User User { get; set; }
}
```

---

### 2.5 AuditLogs Table

```sql
CREATE TABLE AuditLogs (
    Id INT PRIMARY KEY IDENTITY(1,1),
    UserId INT NOT NULL,
    Action NVARCHAR(255) NOT NULL,  -- Login, Register, DeviceRegistered, SessionStarted, etc.
    Resource NVARCHAR(255),  -- Device, Session, User, etc.
    ResourceId INT NULL,
    Result NVARCHAR(50) NOT NULL DEFAULT 'Success',  -- Success, Failure
    Details NVARCHAR(MAX) NULL,  -- JSON
    IpAddress NVARCHAR(50) NULL,
    UserAgent NVARCHAR(MAX) NULL,
    Timestamp DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT fk_auditLogs_users
        FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,

    INDEX idx_userId (UserId),
    INDEX idx_action (Action),
    INDEX idx_timestamp (Timestamp),
    INDEX idx_composite (UserId, Action, Timestamp)
);
```

**C# Entity**:
```csharp
public class AuditLog
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Action { get; set; }
    public string Resource { get; set; }
    public int? ResourceId { get; set; }
    public string Result { get; set; }
    public string Details { get; set; }  // JSON
    public string IpAddress { get; set; }
    public string UserAgent { get; set; }
    public DateTime Timestamp { get; set; }

    public User User { get; set; }
}
```

---

### 2.6 PairingCodes Table (Caché/Ephemeral)

```sql
CREATE TABLE PairingCodes (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Code NVARCHAR(255) NOT NULL UNIQUE,
    SeniorDeviceId INT NOT NULL,
    ExpiresAt DATETIME2 NOT NULL,
    IsUsed BIT NOT NULL DEFAULT 0,
    UsedAt DATETIME2 NULL,
    UsedByDeviceId INT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT fk_pairingCodes_seniorDevice
        FOREIGN KEY (SeniorDeviceId) REFERENCES Devices(Id) ON DELETE CASCADE,

    INDEX idx_code UNIQUE (Code),
    INDEX idx_seniorDeviceId (SeniorDeviceId),
    INDEX idx_expiresAt (ExpiresAt)
);
```

**C# Entity**:
```csharp
public class PairingCode
{
    public int Id { get; set; }
    public string Code { get; set; }  // QR-ABC123DEF456
    public int SeniorDeviceId { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; }
    public DateTime? UsedAt { get; set; }
    public int? UsedByDeviceId { get; set; }
    public DateTime CreatedAt { get; set; }

    public Device SeniorDevice { get; set; }
}
```

---

## 3. DbContext Configuration

```csharp
public class RemoteCareContext : DbContext
{
    public RemoteCareContext(DbContextOptions<RemoteCareContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Device> Devices { get; set; }
    public DbSet<Session> Sessions { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<PairingCode> PairingCodes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.Role);
            entity.HasIndex(e => e.CreatedAt);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
        });

        // Device configuration
        modelBuilder.Entity<Device>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.DeviceId).IsUnique();
            entity.HasIndex(e => new { e.UserId, e.IsActive });
            entity.HasOne(e => e.User)
                .WithMany(u => u.Devices)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Session configuration
        modelBuilder.Entity<Session>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.SessionId).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => new { e.SeniorDeviceId, e.SupportDeviceId, e.Status });

            entity.HasOne(e => e.SeniorDevice)
                .WithMany(d => d.SessionsAsSenior)
                .HasForeignKey(e => e.SeniorDeviceId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.SupportDevice)
                .WithMany(d => d.SessionsAsSupport)
                .HasForeignKey(e => e.SupportDeviceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // RefreshToken configuration
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Token).IsUnique();
            entity.HasOne(e => e.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // AuditLog configuration
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.Action, e.Timestamp });
            entity.HasOne(e => e.User)
                .WithMany(u => u.AuditLogs)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // PairingCode configuration
        modelBuilder.Entity<PairingCode>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasOne(e => e.SeniorDevice)
                .WithMany()
                .HasForeignKey(e => e.SeniorDeviceId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
```

---

## 4. Initial Migration

```bash
# Crear proyecto
dotnet new webapi -n RemoteCare.Api
cd RemoteCare.Api

# Instalar dependencias
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.EntityFrameworkCore.Tools

# Crear migration inicial
dotnet ef migrations add Initial --output-dir Data/Migrations

# Aplicar migration
dotnet ef database update
```

---

## 5. Seed Data (opcional)

```csharp
public static class DbSeeder
{
    public static void Seed(RemoteCareContext context)
    {
        if (context.Users.Any())
            return;

        // Admin user
        var admin = new User
        {
            Email = "admin@remotecare.com",
            FirstName = "Admin",
            LastName = "User",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
            Role = "Admin",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        context.Users.Add(admin);
        context.SaveChanges();
    }
}

// Program.cs
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<RemoteCareContext>();
    DbSeeder.Seed(context);
}
```

---

## 6. Query Performance Optimization

### Índices importantes:
1. **Users.Email** - Búsquedas por email (login)
2. **Devices.UserId, Devices.IsActive** - Listar dispositivos del usuario
3. **Sessions.Status, Sessions.StartedAt** - Sesiones activas/historial
4. **AuditLogs.UserId, AuditLogs.Timestamp** - Auditoría

### Lazy Loading
Deshabilitado por defecto. Usar explicit includes:

```csharp
var device = await context.Devices
    .Include(d => d.User)
    .Include(d => d.SessionsAsSenior)
    .FirstOrDefaultAsync(d => d.Id == id);
```

### Pagination
```csharp
var sessions = await context.Sessions
    .Where(s => s.SeniorDeviceId == deviceId)
    .OrderByDescending(s => s.StartedAt)
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();
```

---

## 7. Data Retention Policy

- **Users**: Mantener indefinidamente
- **Devices**: Mantener indefinidamente (soft delete posible)
- **Sessions**: 1 año de historial
- **AuditLogs**: 1 año
- **RefreshTokens**: Eliminar después de expiración
- **PairingCodes**: Eliminar después de 5 minutos (expiración)

---

## 8. Backup Strategy

- **Diario**: Backup incremental
- **Semanal**: Backup completo
- **Mensual**: Backup completo almacenado en almacenamiento frío
- **Retención**: 1 año para auditoría

---

## 9. Data Privacy Considerations

- ✅ Passwords hasheadas (bcrypt)
- ✅ Datos sensibles no loguados
- ✅ GDPR compliance (derecho al olvido)
- ✅ Encriptación TLS en tránsito
- ⚠️ Frames de pantalla: No almacenados (solo transmitidos en vivo)
