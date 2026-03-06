# RemoteCare Development Guide

## Quick Start (5 minutes)

```bash
# Clone repository
git clone https://github.com/yourusername/remotecare.git
cd remotecare

# Restore dependencies
dotnet restore

# Build solution
dotnet build

# Run migrations
cd src/RemoteCare.Api
dotnet ef database update

# Start development server
dotnet run

# Server runs on https://localhost:5001
# API available at https://localhost:5001/api
# Swagger UI at https://localhost:5001/swagger
```

---

## 1. Project Structure Walkthrough

### Backend (C#)
```
src/RemoteCare.Api/
├── Program.cs                    # Entrypoint, DI setup
├── Controllers/                  # HTTP endpoints
│   ├── AuthController.cs
│   ├── DeviceController.cs
│   └── SessionController.cs
├── Services/                     # Business logic
├── Data/                         # Entity Framework
│   ├── RemoteCareContext.cs
│   └── Migrations/
├── Models/                       # Domain entities
├── Dtos/                         # API models
├── Middleware/                   # Custom middleware
├── Localization/                 # i18n resources
├── Exceptions/                   # Custom exceptions
└── appsettings.json             # Configuration
```

### Android Apps
```
android-senior/                  # Senior device app
android-support/                 # Support device app
```

---

## 2. Setting Up Development Environment

### Prerequisites
- **.NET SDK 8.0+**: https://dotnet.microsoft.com/download
- **Visual Studio Code** or **Visual Studio 2022**
- **Android Studio** (for mobile development)
- **Git**

### Recommended VS Code Extensions
```
- C# (by Microsoft)
- REST Client (for testing APIs)
- SQLite (for database viewing)
- GitHub Copilot (optional)
```

### Local Configuration

**appsettings.Development.json**:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft": "Information"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=remotecare.db"
  },
  "Jwt": {
    "SecretKey": "dev-secret-key-min-32-chars-long",
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 7
  },
  "Encryption": {
    "Key": "dev-encryption-key-base64",
    "IV": "dev-iv-base64"
  },
  "Cors": {
    "AllowedOrigins": ["http://localhost:3000", "http://localhost:8100"]
  }
}
```

---

## 3. Development Workflow

### Adding a New Feature

#### Step 1: Create Entity/Model
```csharp
// Models/MyEntity.cs
public class MyEntity
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int UserId { get; set; }
    public User User { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

#### Step 2: Update DbContext
```csharp
// Data/RemoteCareContext.cs
public DbSet<MyEntity> MyEntities { get; set; }

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // Configuration...
    modelBuilder.Entity<MyEntity>(entity =>
    {
        entity.HasKey(e => e.Id);
        entity.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    });
}
```

#### Step 3: Create Migration
```bash
cd src/RemoteCare.Api
dotnet ef migrations add AddMyEntity
dotnet ef database update
```

#### Step 4: Create DTO
```csharp
// Dtos/MyEntity/CreateMyEntityRequest.cs
public class CreateMyEntityRequest
{
    [Required]
    public string Name { get; set; }
}

public class MyEntityDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

#### Step 5: Create Repository
```csharp
// Data/Repositories/IMyEntityRepository.cs
public interface IMyEntityRepository : IRepository<MyEntity>
{
    Task<MyEntity> GetByNameAsync(string name);
}

// Data/Repositories/MyEntityRepository.cs
public class MyEntityRepository : Repository<MyEntity>, IMyEntityRepository
{
    public MyEntityRepository(RemoteCareContext context) : base(context) { }

    public async Task<MyEntity> GetByNameAsync(string name)
    {
        return await _context.MyEntities
            .FirstOrDefaultAsync(e => e.Name == name);
    }
}
```

#### Step 6: Create Service
```csharp
// Services/IMyEntityService.cs
public interface IMyEntityService
{
    Task<MyEntityDto> CreateAsync(CreateMyEntityRequest request);
    Task<MyEntityDto> GetAsync(int id);
}

// Services/MyEntityService.cs
public class MyEntityService : IMyEntityService
{
    private readonly IMyEntityRepository _repository;
    private readonly IMapper _mapper;

    public MyEntityService(IMyEntityRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<MyEntityDto> CreateAsync(CreateMyEntityRequest request)
    {
        var entity = new MyEntity { Name = request.Name };
        await _repository.AddAsync(entity);
        return _mapper.Map<MyEntityDto>(entity);
    }

    public async Task<MyEntityDto> GetAsync(int id)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity == null)
            throw new NotFoundException("Entity not found");
        return _mapper.Map<MyEntityDto>(entity);
    }
}
```

#### Step 7: Register in DI
```csharp
// Program.cs
builder.Services.AddScoped<IMyEntityRepository, MyEntityRepository>();
builder.Services.AddScoped<IMyEntityService, MyEntityService>();
```

#### Step 8: Create Controller
```csharp
// Controllers/MyEntityController.cs
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MyEntityController : ControllerBase
{
    private readonly IMyEntityService _service;

    public MyEntityController(IMyEntityService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<ActionResult<MyEntityDto>> Create([FromBody] CreateMyEntityRequest request)
    {
        var result = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<MyEntityDto>> Get(int id)
    {
        var result = await _service.GetAsync(id);
        return Ok(result);
    }
}
```

#### Step 9: Test
```bash
# Run locally
dotnet run

# Test with curl or Postman
curl -X POST https://localhost:5001/api/myentity \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{"name": "Test"}'
```

---

## 4. Database Operations

### Viewing Data (SQLite)
```bash
# Install sqlite tools
dotnet tool install --global sqlite-utils

# Query database
sqlite3 remotecare.db "SELECT * FROM Users;"
```

### Adding a Column to Existing Table
```csharp
// Migration
public partial class AddNewColumnToUsers : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "PhoneNumber",
            table: "Users",
            type: "text",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "PhoneNumber",
            table: "Users");
    }
}

// Apply migration
dotnet ef database update
```

### Seeding Data
```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<User>().HasData(
        new User
        {
            Id = 1,
            Email = "admin@remotecare.com",
            FirstName = "Admin",
            LastName = "User",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
            Role = "Admin",
            CreatedAt = DateTime.UtcNow
        }
    );
}
```

---

## 5. Testing

### Unit Testing Example

```csharp
// Tests/Unit/Services/AuthServiceTests.cs
public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _authService = new AuthService(_userRepositoryMock.Object);
    }

    [Fact]
    public async Task Register_ValidRequest_ReturnsToken()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "test@example.com",
            Password = "Test123!@",
            FullName = "Test User",
            Role = "Senior"
        };

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        Assert.NotNull(result.Jwt);
        Assert.Equal("test@example.com", result.Email);
    }

    [Fact]
    public async Task Login_InvalidCredentials_ThrowsException()
    {
        // Arrange
        _userRepositoryMock.Setup(r => r.GetByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((User)null);

        var request = new LoginRequest
        {
            Email = "invalid@example.com",
            Password = "wrongpass"
        };

        // Act & Assert
        await Assert.ThrowsAsync<AuthenticationException>(
            () => _authService.LoginAsync(request));
    }
}
```

### Running Tests
```bash
dotnet test
dotnet test --verbosity quiet
dotnet test --filter ClassName=AuthServiceTests
```

---

## 6. API Testing

### Using REST Client (VS Code)

Create `requests.http`:
```http
### Variables
@baseUrl = https://localhost:5001
@token = your-jwt-token-here

### Register User
POST {{baseUrl}}/api/auth/register
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "SecurePass123!",
  "fullName": "John Doe",
  "role": "Senior"
}

### Login
POST {{baseUrl}}/api/auth/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "SecurePass123!"
}

### Register Device
POST {{baseUrl}}/api/device/register
Authorization: Bearer {{token}}
Content-Type: application/json

{
  "deviceId": "uuid-1234",
  "deviceName": "Samsung Galaxy",
  "osVersion": "14",
  "role": "Senior"
}
```

Then use VS Code's REST Client extension to send requests.

---

## 7. Debugging

### Debug in VS Code

Create `.vscode/launch.json`:
```json
{
    "version": "0.2.0",
    "configurations": [
        {
            "name": ".NET Core Launch (web)",
            "type": "coreclr",
            "request": "launch",
            "preLaunchTask": "build",
            "program": "${workspaceFolder}/src/RemoteCare.Api/bin/Debug/net8.0/RemoteCare.Api.dll",
            "args": [],
            "cwd": "${workspaceFolder}/src/RemoteCare.Api",
            "stopAtEntry": false,
            "serverReadyAction": {
                "pattern": "\\bNow listening on:\\s+(https?://\\S+)",
                "uriFormat": "{url}",
                "action": "openExternally"
            }
        }
    ]
}
```

### Logging
```csharp
// Inject ILogger
public class MyService
{
    private readonly ILogger<MyService> _logger;

    public MyService(ILogger<MyService> logger)
    {
        _logger = logger;
    }

    public void DoSomething()
    {
        _logger.LogInformation("Starting operation");
        _logger.LogWarning("Something might be wrong");
        _logger.LogError("Error occurred: {ErrorMessage}", ex.Message);
        _logger.LogDebug("Debug info: {Value}", value);
    }
}
```

---

## 8. Git Workflow

```bash
# Create feature branch
git checkout -b feature/my-feature

# Make changes, commit regularly
git add .
git commit -m "feat: Add new feature"

# Push to remote
git push origin feature/my-feature

# Create Pull Request (via GitHub UI)

# After review, merge and delete branch
git checkout master
git pull origin master
git branch -d feature/my-feature
```

### Commit Message Convention (Conventional Commits)
```
feat: Add new feature
fix: Fix a bug
docs: Update documentation
style: Code style changes
refactor: Refactor code
test: Add tests
chore: Maintenance tasks
```

---

## 9. Common Tasks

### Change API Port
```json
// appsettings.json
{
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://localhost:5000"
      },
      "Https": {
        "Url": "https://localhost:5001"
      }
    }
  }
}
```

### Enable CORS for Testing
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("Development", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

app.UseCors("Development");
```

### Add Swagger/OpenAPI
```csharp
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

app.UseSwagger();
app.UseSwaggerUI();
```

---

## 10. Troubleshooting

### Issue: Migration fails
```bash
# Reset database
dotnet ef database drop
dotnet ef database update

# Or remove last migration
dotnet ef migrations remove
```

### Issue: Port already in use
```bash
# Find process using port 5001
lsof -i :5001

# Kill process
kill -9 <PID>

# Or change port in appsettings.json
```

### Issue: SQLite database locked
```bash
# Close all connections and remove lock file
rm remotecare.db-wal
rm remotecare.db-shm
```

---

## Resources

- **ASP.NET Core Docs**: https://docs.microsoft.com/en-us/aspnet/core/
- **Entity Framework Core**: https://docs.microsoft.com/en-us/ef/core/
- **JWT Auth**: https://jwt.io/
- **.NET CLI Commands**: https://docs.microsoft.com/en-us/dotnet/core/tools/
