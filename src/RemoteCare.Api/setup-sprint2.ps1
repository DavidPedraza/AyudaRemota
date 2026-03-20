# AyudaRemota - Sprint 2 Setup Script
# Device Registration + QR Generation + Device Pairing

Write-Host "`n=== Sprint 2: Device Management ===" -ForegroundColor Cyan

# Verificar ubicación
if (-not (Test-Path "RemoteCare.Api.csproj")) {
    Write-Host "❌ No estás en src/RemoteCare.Api" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Ubicación correcta" -ForegroundColor Green

# ============================================
# 1. CREAR DTOs PARA DEVICES
# ============================================
Write-Host "`n📝 Creando DTOs para Devices..." -ForegroundColor Cyan

$dtos = @{
    "Dtos/Device/RegisterDeviceRequest.cs" = @'
using System.ComponentModel.DataAnnotations;

namespace RemoteCare.Api.Dtos.Device
{
    public class RegisterDeviceRequest
    {
        [Required(ErrorMessage = "Device ID is required")]
        public string DeviceId { get; set; }

        [Required(ErrorMessage = "Device name is required")]
        [StringLength(100, MinimumLength = 3)]
        public string DeviceName { get; set; }

        [Required(ErrorMessage = "OS Version is required")]
        public string OsVersion { get; set; }

        [Required(ErrorMessage = "Manufacturer is required")]
        public string Manufacturer { get; set; }

        [Required(ErrorMessage = "Model is required")]
        public string Model { get; set; }

        [Required(ErrorMessage = "Role is required")]
        public string Role { get; set; } // Senior, Support
    }
}
'@

    "Dtos/Device/DeviceDto.cs" = @'
namespace RemoteCare.Api.Dtos.Device
{
    public class DeviceDto
    {
        public int Id { get; set; }
        public string DeviceId { get; set; }
        public string DeviceName { get; set; }
        public string Role { get; set; }
        public bool IsActive { get; set; }
        public DateTime RegisteredAt { get; set; }
        public DateTime? LastSeen { get; set; }
    }
}
'@

    "Dtos/Device/GenerateQrRequest.cs" = @'
using System.ComponentModel.DataAnnotations;

namespace RemoteCare.Api.Dtos.Device
{
    public class GenerateQrRequest
    {
        [Required(ErrorMessage = "Device ID is required")]
        public string DeviceId { get; set; }
    }
}
'@

    "Dtos/Device/GenerateQrResponse.cs" = @'
namespace RemoteCare.Api.Dtos.Device
{
    public class GenerateQrResponse
    {
        public string PairingCode { get; set; }
        public string QrData { get; set; } // Base64 encoded PNG
        public int ExpiresIn { get; set; } // seconds
        public DateTime ExpiresAt { get; set; }
    }
}
'@

    "Dtos/Device/PairDevicesRequest.cs" = @'
using System.ComponentModel.DataAnnotations;

namespace RemoteCare.Api.Dtos.Device
{
    public class PairDevicesRequest
    {
        [Required(ErrorMessage = "Pairing code is required")]
        public string PairingCode { get; set; }

        [Required(ErrorMessage = "Device ID is required")]
        public string DeviceId { get; set; }
    }
}
'@

    "Dtos/Device/PairDevicesResponse.cs" = @'
namespace RemoteCare.Api.Dtos.Device
{
    public class PairDevicesResponse
    {
        public int SessionId { get; set; }
        public string SessionUuid { get; set; }
        public DeviceDto SeniorDevice { get; set; }
        public DeviceDto SupportDevice { get; set; }
        public string Status { get; set; }
        public DateTime ConnectedAt { get; set; }
        public string WsUrl { get; set; }
    }
}
'@
}

foreach ($file in $dtos.Keys) {
    Set-Content -Path $file -Value $dtos[$file]
    Write-Host "✅ Creado: $file" -ForegroundColor Green
}

# ============================================
# 2. CREAR MODELO PAIRINGCODE
# ============================================
Write-Host "`n📝 Creando modelo PairingCode..." -ForegroundColor Cyan

Set-Content -Path "Models/PairingCode.cs" -Value @'
namespace RemoteCare.Api.Models
{
    public class PairingCode
    {
        public int Id { get; set; }
        public string Code { get; set; } // QR-ABC123DEF456
        public int SeniorDeviceId { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsUsed { get; set; } = false;
        public DateTime? UsedAt { get; set; }
        public int? UsedByDeviceId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Device SeniorDevice { get; set; }
    }
}
'@

Write-Host "✅ Creado: PairingCode.cs" -ForegroundColor Green

# ============================================
# 3. ACTUALIZAR DBCONTEXT
# ============================================
Write-Host "`n📝 Actualizando DbContext..." -ForegroundColor Cyan

$dbContextUpdate = @'
        public DbSet<PairingCode> PairingCodes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ... existing configurations ...

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
'@

Write-Host "⚠️  Actualizar Data/RemoteCareContext.cs manualmente:" -ForegroundColor Yellow
Write-Host "    - Agregar: public DbSet<PairingCode> PairingCodes { get; set; }" -ForegroundColor Yellow
Write-Host "    - En OnModelCreating, agregar la configuración de PairingCode" -ForegroundColor Yellow

# ============================================
# 4. CREAR QR GENERATOR SERVICE
# ============================================
Write-Host "`n📝 Creando QrGeneratorService..." -ForegroundColor Cyan

Set-Content -Path "Services/IQrGeneratorService.cs" -Value @'
namespace RemoteCare.Api.Services
{
    public interface IQrGeneratorService
    {
        string GenerateQrCode(string data, out byte[] qrImage);
        string GeneratePairingCode();
    }
}
'@

Set-Content -Path "Services/QrGeneratorService.cs" -Value @'
using QRCoder;

namespace RemoteCare.Api.Services
{
    public class QrGeneratorService : IQrGeneratorService
    {
        public string GenerateQrCode(string data, out byte[] qrImage)
        {
            using (var qrGenerator = new QRCodeGenerator())
            {
                var qrCodeData = qrGenerator.CreateQrCode(data, QRCodeGenerator.ECCLevel.Q);
                using (var qrCode = new PngByteQRCode(qrCodeData))
                {
                    qrImage = qrCode.GetGraphic(10);
                    return Convert.ToBase64String(qrImage);
                }
            }
        }

        public string GeneratePairingCode()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            var code = "QR-" + new string(Enumerable.Range(0, 12)
                .Select(_ => chars[random.Next(chars.Length)])
                .ToArray());
            return code;
        }
    }
}
'@

Write-Host "✅ Creado: QrGeneratorService.cs" -ForegroundColor Green

# ============================================
# 5. CREAR DEVICE SERVICE
# ============================================
Write-Host "`n📝 Creando DeviceService..." -ForegroundColor Cyan

Set-Content -Path "Services/IDeviceService.cs" -Value @'
using RemoteCare.Api.Dtos.Device;

namespace RemoteCare.Api.Services
{
    public interface IDeviceService
    {
        Task<DeviceDto> RegisterDeviceAsync(int userId, RegisterDeviceRequest request);
        Task<IEnumerable<DeviceDto>> GetUserDevicesAsync(int userId, string role = null);
        Task<DeviceDto> GetDeviceAsync(int deviceId);
        Task DeleteDeviceAsync(int userId, int deviceId);
    }
}
'@

Set-Content -Path "Services/DeviceService.cs" -Value @'
using RemoteCare.Api.Models;
using RemoteCare.Api.Data;
using RemoteCare.Api.Dtos.Device;
using RemoteCare.Api.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace RemoteCare.Api.Services
{
    public class DeviceService : IDeviceService
    {
        private readonly RemoteCareContext _context;
        private readonly ILogger<DeviceService> _logger;

        public DeviceService(RemoteCareContext context, ILogger<DeviceService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<DeviceDto> RegisterDeviceAsync(int userId, RegisterDeviceRequest request)
        {
            // Verificar que el dispositivo no está registrado
            var existingDevice = await _context.Devices
                .FirstOrDefaultAsync(d => d.DeviceId == request.DeviceId);

            if (existingDevice != null)
            {
                _logger.LogWarning($"Intento de registrar dispositivo duplicado: {request.DeviceId}");
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "DeviceId", new[] { "Este dispositivo ya está registrado" } }
                });
            }

            var device = new Device
            {
                DeviceId = request.DeviceId,
                DeviceName = request.DeviceName,
                UserId = userId,
                Role = request.Role ?? "Senior",
                OsVersion = request.OsVersion,
                Manufacturer = request.Manufacturer,
                Model = request.Model,
                IsActive = true,
                RegisteredAt = DateTime.UtcNow
            };

            _context.Devices.Add(device);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Dispositivo registrado: {device.DeviceId} para usuario {userId}");

            return MapToDto(device);
        }

        public async Task<IEnumerable<DeviceDto>> GetUserDevicesAsync(int userId, string role = null)
        {
            var query = _context.Devices
                .Where(d => d.UserId == userId && d.IsActive);

            if (!string.IsNullOrEmpty(role))
                query = query.Where(d => d.Role == role);

            var devices = await query.ToListAsync();
            return devices.Select(MapToDto);
        }

        public async Task<DeviceDto> GetDeviceAsync(int deviceId)
        {
            var device = await _context.Devices.FindAsync(deviceId);

            if (device == null)
                throw new NotFoundException("Dispositivo no encontrado");

            return MapToDto(device);
        }

        public async Task DeleteDeviceAsync(int userId, int deviceId)
        {
            var device = await _context.Devices.FindAsync(deviceId);

            if (device == null)
                throw new NotFoundException("Dispositivo no encontrado");

            if (device.UserId != userId)
                throw new UnauthorizedAccessException("No tienes permiso para eliminar este dispositivo");

            device.IsActive = false;
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Dispositivo desactivado: {deviceId}");
        }

        private DeviceDto MapToDto(Device device)
        {
            return new DeviceDto
            {
                Id = device.Id,
                DeviceId = device.DeviceId,
                DeviceName = device.DeviceName,
                Role = device.Role,
                IsActive = device.IsActive,
                RegisteredAt = device.RegisteredAt,
                LastSeen = device.LastSeen
            };
        }
    }
}
'@

Write-Host "✅ Creado: DeviceService.cs" -ForegroundColor Green

# ============================================
# 6. CREAR DEVICE PAIRING SERVICE
# ============================================
Write-Host "`n📝 Creando DevicePairingService..." -ForegroundColor Cyan

Set-Content -Path "Services/IDevicePairingService.cs" -Value @'
using RemoteCare.Api.Dtos.Device;

namespace RemoteCare.Api.Services
{
    public interface IDevicePairingService
    {
        Task<GenerateQrResponse> GenerateQrAsync(int userId, string deviceId);
        Task<PairDevicesResponse> PairDevicesAsync(int userId, PairDevicesRequest request);
    }
}
'@

Set-Content -Path "Services/DevicePairingService.cs" -Value @'
using RemoteCare.Api.Models;
using RemoteCare.Api.Data;
using RemoteCare.Api.Dtos.Device;
using RemoteCare.Api.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace RemoteCare.Api.Services
{
    public class DevicePairingService : IDevicePairingService
    {
        private readonly RemoteCareContext _context;
        private readonly IQrGeneratorService _qrGeneratorService;
        private readonly ILogger<DevicePairingService> _logger;

        public DevicePairingService(
            RemoteCareContext context,
            IQrGeneratorService qrGeneratorService,
            ILogger<DevicePairingService> logger)
        {
            _context = context;
            _qrGeneratorService = qrGeneratorService;
            _logger = logger;
        }

        public async Task<GenerateQrResponse> GenerateQrAsync(int userId, string deviceId)
        {
            // Verificar que el dispositivo pertenece al usuario
            var device = await _context.Devices
                .FirstOrDefaultAsync(d => d.DeviceId == deviceId && d.UserId == userId);

            if (device == null)
                throw new NotFoundException("Dispositivo no encontrado");

            if (device.Role != "Senior")
                throw new ValidationException("Solo dispositivos Senior pueden generar QR");

            // Generar código único
            var code = _qrGeneratorService.GeneratePairingCode();
            var qrData = _qrGeneratorService.GenerateQrCode(code, out var qrImage);

            // Guardar en BD
            var pairingCode = new PairingCode
            {
                Code = code,
                SeniorDeviceId = device.Id,
                ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                IsUsed = false
            };

            _context.PairingCodes.Add(pairingCode);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"QR generado para dispositivo: {deviceId}");

            return new GenerateQrResponse
            {
                PairingCode = code,
                QrData = qrData,
                ExpiresIn = 300, // 5 minutes
                ExpiresAt = pairingCode.ExpiresAt
            };
        }

        public async Task<PairDevicesResponse> PairDevicesAsync(int userId, PairDevicesRequest request)
        {
            // Verificar código de emparejamiento
            var pairingCode = await _context.PairingCodes
                .Include(pc => pc.SeniorDevice)
                .ThenInclude(d => d.User)
                .FirstOrDefaultAsync(pc => pc.Code == request.PairingCode);

            if (pairingCode == null)
                throw new ValidationException("Código de emparejamiento inválido");

            if (pairingCode.IsUsed)
                throw new ValidationException("Este código ya ha sido utilizado");

            if (pairingCode.ExpiresAt < DateTime.UtcNow)
                throw new ValidationException("El código ha expirado");

            // Obtener dispositivo Support
            var supportDevice = await _context.Devices
                .FirstOrDefaultAsync(d => d.DeviceId == request.DeviceId && d.UserId == userId);

            if (supportDevice == null)
                throw new NotFoundException("Dispositivo support no encontrado");

            if (supportDevice.Role != "Support")
                throw new ValidationException("Solo dispositivos Support pueden vincularse");

            // Crear sesión
            var session = new Session
            {
                SessionId = Guid.NewGuid().ToString(),
                SeniorDeviceId = pairingCode.SeniorDeviceId,
                SupportDeviceId = supportDevice.Id,
                Status = "Active",
                StartedAt = DateTime.UtcNow,
                IsEncrypted = true
            };

            _context.Sessions.Add(session);

            // Marcar código como usado
            pairingCode.IsUsed = true;
            pairingCode.UsedAt = DateTime.UtcNow;
            pairingCode.UsedByDeviceId = supportDevice.Id;

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Dispositivos emparejados: {pairingCode.SeniorDeviceId} - {supportDevice.Id}");

            return new PairDevicesResponse
            {
                SessionId = session.Id,
                SessionUuid = session.SessionId,
                SeniorDevice = MapToDto(pairingCode.SeniorDevice),
                SupportDevice = MapToDto(supportDevice),
                Status = session.Status,
                ConnectedAt = session.StartedAt,
                WsUrl = $"wss://localhost:5000/hubs/screen?sessionId={session.SessionId}"
            };
        }

        private DeviceDto MapToDto(Device device)
        {
            return new DeviceDto
            {
                Id = device.Id,
                DeviceId = device.DeviceId,
                DeviceName = device.DeviceName,
                Role = device.Role,
                IsActive = device.IsActive,
                RegisteredAt = device.RegisteredAt,
                LastSeen = device.LastSeen
            };
        }
    }
}
'@

Write-Host "✅ Creado: DevicePairingService.cs" -ForegroundColor Green

# ============================================
# 7. CREAR DEVICE CONTROLLER
# ============================================
Write-Host "`n📝 Creando DeviceController..." -ForegroundColor Cyan

Set-Content -Path "Controllers/DeviceController.cs" -Value @'
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using RemoteCare.Api.Services;
using RemoteCare.Api.Dtos.Device;
using RemoteCare.Api.Exceptions;

namespace RemoteCare.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DeviceController : ControllerBase
    {
        private readonly IDeviceService _deviceService;
        private readonly IDevicePairingService _pairingService;
        private readonly ILogger<DeviceController> _logger;

        public DeviceController(
            IDeviceService deviceService,
            IDevicePairingService pairingService,
            ILogger<DeviceController> logger)
        {
            _deviceService = deviceService;
            _pairingService = pairingService;
            _logger = logger;
        }

        /// <summary>
        /// Registrar nuevo dispositivo
        /// </summary>
        [HttpPost("register")]
        public async Task<ActionResult<DeviceDto>> RegisterDevice([FromBody] RegisterDeviceRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                if (userId == 0)
                    return Unauthorized();

                var device = await _deviceService.RegisterDeviceAsync(userId, request);
                return CreatedAtAction(nameof(GetDevice), new { id = device.Id }, device);
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { errors = ex.Errors });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar dispositivo");
                return StatusCode(500, new { error = "Error al registrar dispositivo" });
            }
        }

        /// <summary>
        /// Generar código QR para emparejamiento
        /// </summary>
        [HttpPost("generate-qr")]
        public async Task<ActionResult<GenerateQrResponse>> GenerateQr([FromBody] GenerateQrRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                if (userId == 0)
                    return Unauthorized();

                var response = await _pairingService.GenerateQrAsync(userId, request.DeviceId);
                return Ok(response);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar QR");
                return StatusCode(500, new { error = "Error al generar QR" });
            }
        }

        /// <summary>
        /// Emparejar dispositivos usando código QR
        /// </summary>
        [HttpPost("pair")]
        public async Task<ActionResult<PairDevicesResponse>> PairDevices([FromBody] PairDevicesRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                if (userId == 0)
                    return Unauthorized();

                var response = await _pairingService.PairDevicesAsync(userId, request);
                return Ok(response);
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al emparejar dispositivos");
                return StatusCode(500, new { error = "Error al emparejar dispositivos" });
            }
        }

        /// <summary>
        /// Obtener mis dispositivos
        /// </summary>
        [HttpGet("my-devices")]
        public async Task<ActionResult<IEnumerable<DeviceDto>>> GetMyDevices([FromQuery] string role = null)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                if (userId == 0)
                    return Unauthorized();

                var devices = await _deviceService.GetUserDevicesAsync(userId, role);
                return Ok(devices);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener dispositivos");
                return StatusCode(500, new { error = "Error al obtener dispositivos" });
            }
        }

        /// <summary>
        /// Obtener dispositivo por ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<DeviceDto>> GetDevice(int id)
        {
            try
            {
                var device = await _deviceService.GetDeviceAsync(id);
                return Ok(device);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener dispositivo");
                return StatusCode(500, new { error = "Error al obtener dispositivo" });
            }
        }

        /// <summary>
        /// Eliminar dispositivo
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteDevice(int id)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                if (userId == 0)
                    return Unauthorized();

                await _deviceService.DeleteDeviceAsync(userId, id);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar dispositivo");
                return StatusCode(500, new { error = "Error al eliminar dispositivo" });
            }
        }
    }
}
'@

Write-Host "✅ Creado: DeviceController.cs" -ForegroundColor Green

# ============================================
# 8. COMPILAR
# ============================================
Write-Host "`n🔨 Compilando proyecto..." -ForegroundColor Cyan
dotnet build

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n✅ Compilación exitosa" -ForegroundColor Green
    Write-Host @"

Sprint 2 - Próximos pasos MANUALES:

1. ACTUALIZAR DbContext:
   Abre Data/RemoteCareContext.cs y agrega:

   public DbSet<PairingCode> PairingCodes { get; set; }

   En OnModelCreating(), agrega la configuración de PairingCode (ver comentarios arriba)

2. ACTUALIZAR Program.cs:
   Agrega estos servicios después de builder.Services.AddScoped<IAuthService, AuthService>();

   builder.Services.AddScoped<IDeviceService, DeviceService>();
   builder.Services.AddScoped<IDevicePairingService, DevicePairingService>();
   builder.Services.AddScoped<IQrGeneratorService, QrGeneratorService>();

3. CREAR MIGRATION:
   cd ..
   dotnet ef migrations add AddDeviceManagement

4. ACTUALIZAR BD:
   dotnet ef database update

5. COMPILAR Y EJECUTAR:
   dotnet build
   dotnet run

6. PROBAR EN SWAGGER:
   https://localhost:5000/swagger

   Endpoints nuevos:
   - POST /api/device/register
   - POST /api/device/generate-qr
   - POST /api/device/pair
   - GET /api/device/my-devices
   - DELETE /api/device/{id}

¡Sprint 2 listo para completar!
"@ -ForegroundColor Green
} else {
    Write-Host "`n❌ Error en compilación" -ForegroundColor Red
}
