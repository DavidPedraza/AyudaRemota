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