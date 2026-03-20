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
