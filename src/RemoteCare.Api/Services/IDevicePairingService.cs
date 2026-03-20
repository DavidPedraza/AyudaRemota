using RemoteCare.Api.Dtos.Device;

namespace RemoteCare.Api.Services
{
    public interface IDevicePairingService
    {
        Task<GenerateQrResponse> GenerateQrAsync(int userId, string deviceId);
        Task<PairDevicesResponse> PairDevicesAsync(int userId, PairDevicesRequest request);
    }
}
