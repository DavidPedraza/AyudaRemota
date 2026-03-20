# Script para crear todos los DTOs faltantes de Sprint 2

Write-Host "`n=== Creando DTOs para Sprint 2 ===" -ForegroundColor Cyan

# Verificar que estamos en la carpeta correcta
if (-not (Test-Path "RemoteCare.Api.csproj")) {
    Write-Host "❌ No estás en src/RemoteCare.Api" -ForegroundColor Red
    exit 1
}

# Crear carpeta si no existe
if (-not (Test-Path "Dtos/Device")) {
    mkdir "Dtos/Device" | Out-Null
    Write-Host "✅ Carpeta Dtos/Device creada" -ForegroundColor Green
}

# ============================================
# CREAR TODOS LOS DTOs
# ============================================

# 1. DeviceDto.cs
Write-Host "`n📝 Creando DeviceDto.cs..." -ForegroundColor Cyan
Set-Content -Path "Dtos/Device/DeviceDto.cs" -Value @'
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
Write-Host "✅ Creado: DeviceDto.cs" -ForegroundColor Green

# 2. RegisterDeviceRequest.cs
Write-Host "`n📝 Creando RegisterDeviceRequest.cs..." -ForegroundColor Cyan
Set-Content -Path "Dtos/Device/RegisterDeviceRequest.cs" -Value @'
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
Write-Host "✅ Creado: RegisterDeviceRequest.cs" -ForegroundColor Green

# 3. GenerateQrRequest.cs
Write-Host "`n📝 Creando GenerateQrRequest.cs..." -ForegroundColor Cyan
Set-Content -Path "Dtos/Device/GenerateQrRequest.cs" -Value @'
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
Write-Host "✅ Creado: GenerateQrRequest.cs" -ForegroundColor Green

# 4. GenerateQrResponse.cs
Write-Host "`n📝 Creando GenerateQrResponse.cs..." -ForegroundColor Cyan
Set-Content -Path "Dtos/Device/GenerateQrResponse.cs" -Value @'
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
Write-Host "✅ Creado: GenerateQrResponse.cs" -ForegroundColor Green

# 5. PairDevicesRequest.cs
Write-Host "`n📝 Creando PairDevicesRequest.cs..." -ForegroundColor Cyan
Set-Content -Path "Dtos/Device/PairDevicesRequest.cs" -Value @'
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
Write-Host "✅ Creado: PairDevicesRequest.cs" -ForegroundColor Green

# 6. PairDevicesResponse.cs
Write-Host "`n📝 Creando PairDevicesResponse.cs..." -ForegroundColor Cyan
Set-Content -Path "Dtos/Device/PairDevicesResponse.cs" -Value @'
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
Write-Host "✅ Creado: PairDevicesResponse.cs" -ForegroundColor Green
