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
