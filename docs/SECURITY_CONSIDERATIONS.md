# RemoteCare Security Considerations

## 1. Authentication & Authorization

### 1.1 JWT Configuration

**Token Structure**:
```
Header: { "alg": "HS256", "typ": "JWT" }
Payload: {
  "sub": "1",
  "email": "user@example.com",
  "role": "Senior",
  "iat": 1709615400,
  "exp": 1709616300,
  "iss": "remotecare",
  "aud": "remotecare-mobile"
}
Signature: HMACSHA256(base64(header) + "." + base64(payload), SECRET_KEY)
```

**Implementation**:
```csharp
public class JwtTokenGenerator
{
    private readonly string _secretKey;
    private readonly string _issuer = "remotecare";
    private readonly string _audience = "remotecare-mobile";
    private readonly int _accessTokenExpirationMinutes = 15;

    public string GenerateAccessToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_accessTokenExpirationMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomNumber);
        }
        return Convert.ToBase64String(randomNumber);
    }
}
```

**Configuration** (appsettings.json):
```json
{
  "Jwt": {
    "SecretKey": "your-super-secret-key-min-32-chars",
    "Issuer": "remotecare",
    "Audience": "remotecare-mobile",
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 7
  }
}
```

**Program.cs Setup**:
```csharp
var jwtSettings = builder.Configuration.GetSection("Jwt");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings["SecretKey"])),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSettings["Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        // WebSocket support
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken) &&
                    context.HttpContext.WebSockets.IsWebSocketRequest)
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });
```

### 1.2 Password Requirements

**Policy**:
- Minimum 8 characters
- 1 uppercase letter (A-Z)
- 1 lowercase letter (a-z)
- 1 number (0-9)
- 1 special character (!@#$%^&*)

**Implementation**:
```csharp
public class PasswordValidator
{
    public static (bool IsValid, string Error) Validate(string password)
    {
        if (password.Length < 8)
            return (false, "Password must be at least 8 characters");

        if (!Regex.IsMatch(password, @"[A-Z]"))
            return (false, "Password must contain uppercase letter");

        if (!Regex.IsMatch(password, @"[a-z]"))
            return (false, "Password must contain lowercase letter");

        if (!Regex.IsMatch(password, @"[0-9]"))
            return (false, "Password must contain number");

        if (!Regex.IsMatch(password, @"[!@#$%^&*]"))
            return (false, "Password must contain special character");

        return (true, string.Empty);
    }
}
```

**Hashing**:
```csharp
// Register
var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, 12);

// Login
bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
```

### 1.3 Role-Based Access Control (RBAC)

```csharp
public enum UserRole
{
    Senior,    // Dispositivo monitoreado
    Support,   // Dispositivo que monitorea
    Admin      // Administrador del sistema
}

// Controller authorization
[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    [HttpGet("users")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetUsers()
    {
        // Solo admins pueden acceder
        return Ok();
    }
}

// Custom authorization
[Authorize]
public class SessionController : ControllerBase
{
    [HttpPost("{sessionId}/end")]
    public async Task<IActionResult> EndSession(int sessionId)
    {
        var session = await _sessionService.GetSessionAsync(sessionId);
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

        // Validar que el usuario es participante de la sesión
        if (session.SeniorDevice.UserId != userId &&
            session.SupportDevice.UserId != userId)
            return Forbid();

        return Ok();
    }
}
```

---

## 2. Data Encryption

### 2.1 Transport Layer (TLS 1.3)

**HTTPS Configuration**:
```csharp
// appsettings.json
{
  "Kestrel": {
    "EndpointDefaults": {
      "Protocols": "Http2;Http3"
    },
    "Endpoints": {
      "HttpsInlineCertAndKey": {
        "Url": "https://localhost:5001",
        "Protocols": "Http1AndHttp2",
        "Certificate": {
          "Path": "path/to/cert.pfx",
          "Password": "cert-password"
        }
      }
    }
  }
}

// Program.cs
app.UseHttpsRedirection();
app.UseHsts(); // HSTS header
```

**HSTS Header**:
```csharp
app.Use(async (context, next) =>
{
    context.Response.Headers.Add(
        "Strict-Transport-Security",
        "max-age=31536000; includeSubDomains");
    await next();
});
```

### 2.2 At-Rest Encryption (Sensible Data)

```csharp
public interface IEncryptionService
{
    string Encrypt(string plainText);
    string Decrypt(string cipherText);
}

public class EncryptionService : IEncryptionService
{
    private readonly byte[] _key;
    private readonly byte[] _iv;

    public EncryptionService(IConfiguration config)
    {
        _key = Convert.FromBase64String(config["Encryption:Key"]); // 32 bytes for AES-256
        _iv = Convert.FromBase64String(config["Encryption:IV"]);   // 16 bytes
    }

    public string Encrypt(string plainText)
    {
        using (var aes = Aes.Create())
        {
            aes.Key = _key;
            aes.IV = _iv;

            var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using (var ms = new MemoryStream())
            {
                using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                using (var sw = new StreamWriter(cs))
                {
                    sw.Write(plainText);
                }
                return Convert.ToBase64String(ms.ToArray());
            }
        }
    }

    public string Decrypt(string cipherText)
    {
        using (var aes = Aes.Create())
        {
            aes.Key = _key;
            aes.IV = _iv;

            var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using (var ms = new MemoryStream(Convert.FromBase64String(cipherText)))
            {
                using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                using (var sr = new StreamReader(cs))
                {
                    return sr.ReadToEnd();
                }
            }
        }
    }
}
```

**Generate Keys**:
```csharp
// One-time generation
using (var aes = Aes.Create())
{
    aes.KeySize = 256;
    aes.GenerateKey();
    aes.GenerateIV();

    string key = Convert.ToBase64String(aes.Key);
    string iv = Convert.ToBase64String(aes.IV);
    // Store safely in appsettings
}
```

### 2.3 Screen Frame Encryption (Optional)

Para máxima privacidad, opcionalmente encriptar frames:

```csharp
public class ScreenFrameService
{
    private readonly IEncryptionService _encryptionService;

    public async Task SendScreenFrameAsync(
        string sessionId,
        byte[] frameData,
        bool encrypt = true)
    {
        string frameBase64 = Convert.ToBase64String(frameData);

        if (encrypt)
        {
            frameBase64 = _encryptionService.Encrypt(frameBase64);
        }

        await _hub.Clients.Group(sessionId)
            .SendAsync("ReceiveScreenFrame", frameBase64);
    }
}
```

---

## 3. Input Validation & Sanitization

### 3.1 Validation Attributes

```csharp
public class RegisterRequest
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; }

    [Required(ErrorMessage = "Password is required")]
    [StringLength(100, MinimumLength = 8,
        ErrorMessage = "Password must be 8-100 characters")]
    [RegularExpression(@"^(?=.*[A-Z])(?=.*[a-z])(?=.*\d)(?=.*[!@#$%^&*])",
        ErrorMessage = "Password must meet complexity requirements")]
    public string Password { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 3)]
    public string FullName { get; set; }
}
```

### 3.2 FluentValidation

```csharp
public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email")
            .Must(BeUniqueEmail).WithMessage("Email already exists");

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8)
            .Must(PassComplexityCheck).WithMessage("Password too weak");

        RuleFor(x => x.FullName)
            .NotEmpty()
            .Length(3, 100);
    }

    private bool BeUniqueEmail(string email)
    {
        // Check database
        return true;
    }

    private bool PassComplexityCheck(string password)
    {
        return Regex.IsMatch(password,
            @"^(?=.*[A-Z])(?=.*[a-z])(?=.*\d)(?=.*[!@#$%^&*])");
    }
}
```

### 3.3 Output Sanitization

```csharp
// Never return sensitive data
public class UserDto
{
    public int Id { get; set; }
    public string Email { get; set; }
    public string FullName { get; set; }
    // NO: PasswordHash
    // NO: LastLogin
    // NO: RefreshTokens
}

// Filter sensitive fields
var userDto = new UserDto
{
    Id = user.Id,
    Email = user.Email,
    FullName = user.FullName
    // Passwords, tokens NEVER returned
};
```

---

## 4. Security Headers

```csharp
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Add("Referrer-Policy", "no-referrer");
    context.Response.Headers.Add("Permissions-Policy", "geolocation=(), microphone=(), camera=()");

    // CSP for browsers
    context.Response.Headers.Add(
        "Content-Security-Policy",
        "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'");

    await next();
});
```

---

## 5. Rate Limiting & DDoS Protection

```csharp
builder.Services.AddRateLimiter(rateLimiterOptions =>
{
    rateLimiterOptions.AddFixedWindowLimiter("auth-limiter", options =>
    {
        options.PermitLimit = 5;
        options.Window = TimeSpan.FromMinutes(1);
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = 2;
    });

    rateLimiterOptions.AddFixedWindowLimiter("device-limiter", options =>
    {
        options.PermitLimit = 10;
        options.Window = TimeSpan.FromMinutes(1);
    });
});

app.UseRateLimiter();

// Apply to endpoints
app.MapPost("/api/auth/login", LoginHandler)
    .RequireRateLimiting("auth-limiter");

app.MapPost("/api/device/register", RegisterDeviceHandler)
    .RequireRateLimiting("device-limiter");
```

---

## 6. CORS Configuration

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowMobileApps", policy =>
    {
        policy
            .WithOrigins("https://remotecare.example.com")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()
            .WithExposedHeaders("X-Total-Count");
    });
});

app.UseCors("AllowMobileApps");
```

---

## 7. Logging Security Events

```csharp
public class AuditService
{
    private readonly RemoteCareContext _context;
    private readonly ILogger<AuditService> _logger;

    public async Task LogAsync(
        int userId,
        string action,
        string resource,
        int? resourceId,
        string result,
        HttpContext httpContext)
    {
        var auditLog = new AuditLog
        {
            UserId = userId,
            Action = action,
            Resource = resource,
            ResourceId = resourceId,
            Result = result,
            IpAddress = httpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = httpContext.Request.Headers["User-Agent"].ToString(),
            Timestamp = DateTime.UtcNow
        };

        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync();

        // Structured logging
        _logger.LogInformation(
            "AuditLog: {UserId} performed {Action} on {Resource}:{ResourceId}. Result: {Result}",
            userId, action, resource, resourceId, result);
    }
}
```

---

## 8. WebSocket Security (SignalR)

```csharp
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = false; // No detailed errors in production
    options.HandshakeTimeout = TimeSpan.FromSeconds(5);
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
});

// Secure Hub
public class ScreenStreamHub : Hub
{
    private readonly ISessionService _sessionService;
    private readonly IAuditService _auditService;

    [Authorize]
    public async Task SendScreenFrame(string sessionId, string frameData)
    {
        // Validate user is part of session
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        var session = await _sessionService.GetSessionAsync(sessionId);

        var isParticipant = session.SeniorDevice.UserId == userId ||
                           session.SupportDevice.UserId == userId;

        if (!isParticipant)
        {
            throw new HubException("Unauthorized");
        }

        // Validate frame data
        if (string.IsNullOrWhiteSpace(frameData) || frameData.Length > 10_000_000) // 10MB max
        {
            throw new HubException("Invalid frame data");
        }

        // Log and broadcast
        await _auditService.LogAsync(userId, "ScreenFrameSent", "Session", int.Parse(sessionId), "Success", Context.GetHttpContext());

        await Clients.Group(sessionId).SendAsync("ReceiveScreenFrame", frameData);
    }
}
```

---

## 9. Device Security

### 9.1 Device Identification

```csharp
public class DeviceRegistrationRequest
{
    [Required]
    public string DeviceId { get; set; }  // UUID from Android

    [Required]
    public string DeviceName { get; set; }

    [Required]
    public string OsVersion { get; set; }

    [Required]
    public string Manufacturer { get; set; }

    [Required]
    public string Model { get; set; }

    // On Android side:
    // val deviceId = UUID.randomUUID().toString()
    // val manufacturer = Build.MANUFACTURER
    // val model = Build.MODEL
    // val version = Build.VERSION.RELEASE
}
```

### 9.2 Device Verification (Optional)

```csharp
public class DeviceVerificationService
{
    public async Task<bool> VerifyDeviceAsync(int deviceId, string expectedDeviceId)
    {
        var device = await _context.Devices.FindAsync(deviceId);

        // Verify device hasn't been cloned
        return device.DeviceId == expectedDeviceId;
    }
}
```

---

## 10. Secure Configuration Management

**appsettings.json** (NO secrets):
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  },
  "AllowedHosts": "*"
}
```

**appsettings.Production.json**:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning"
    }
  },
  "AllowedHosts": "api.remotecare.com"
}
```

**User Secrets** (Local Development):
```bash
dotnet user-secrets init
dotnet user-secrets set "Jwt:SecretKey" "your-secret-key-here"
dotnet user-secrets set "Encryption:Key" "base64-key-here"
dotnet user-secrets set "Encryption:IV" "base64-iv-here"
```

**Azure Key Vault** (Production):
```csharp
builder.Configuration.AddAzureKeyVault(
    new Uri($"https://{keyVaultName}.vault.azure.net/"),
    new DefaultAzureCredential());
```

---

## 11. Privacy Compliance

### 11.1 GDPR Compliance

- ✅ Consent para recolectar datos
- ✅ Data export functionality
- ✅ Right to be forgotten (delete account)
- ✅ Data retention policies

### 11.2 Implement Data Deletion

```csharp
public async Task DeleteUserAsync(int userId)
{
    var user = await _context.Users
        .Include(u => u.Devices)
        .Include(u => u.AuditLogs)
        .Include(u => u.RefreshTokens)
        .FirstOrDefaultAsync(u => u.Id == userId);

    if (user == null)
        throw new NotFoundException("User not found");

    // Delete related data
    _context.Devices.RemoveRange(user.Devices);
    _context.AuditLogs.RemoveRange(user.AuditLogs);
    _context.RefreshTokens.RemoveRange(user.RefreshTokens);
    _context.Users.Remove(user);

    await _context.SaveChangesAsync();
}
```

---

## 12. Security Checklist

- [ ] HTTPS/TLS 1.3 enabled
- [ ] JWT with appropriate expiration
- [ ] Password hashing with bcrypt
- [ ] Rate limiting on auth endpoints
- [ ] Input validation and sanitization
- [ ] CORS configured restrictively
- [ ] Security headers set
- [ ] Logging sensitive events
- [ ] Database encryption keys managed securely
- [ ] No secrets in code/config files
- [ ] WebSocket authentication
- [ ] Device ownership validation
- [ ] Audit logging enabled
- [ ] GDPR compliance implemented
- [ ] Regular security testing

---

## 13. Incident Response

### 13.1 Breach Protocol

1. **Detect**: Monitor for suspicious activity
2. **Contain**: Disable compromised accounts
3. **Investigate**: Review audit logs
4. **Notify**: Inform affected users
5. **Remediate**: Update security measures
6. **Review**: Conduct post-mortem

### 13.2 Account Compromise

```csharp
public async Task LockAccountAsync(int userId, string reason)
{
    var user = await _context.Users.FindAsync(userId);
    user.IsActive = false;

    var tokens = await _context.RefreshTokens
        .Where(t => t.UserId == userId && !t.IsRevoked)
        .ToListAsync();

    foreach (var token in tokens)
    {
        token.IsRevoked = true;
    }

    await _auditService.LogAsync(
        1, // Admin
        "AccountLocked",
        "User",
        userId,
        "Success",
        reason);

    await _context.SaveChangesAsync();
}
```

---

## References

- OWASP Top 10: https://owasp.org/www-project-top-ten/
- CWE/SANS Top 25: https://cwe.mitre.org/top25/
- NIST Cybersecurity Framework: https://www.nist.gov/cyberframework
