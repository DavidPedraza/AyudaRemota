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
