using System.ComponentModel.DataAnnotations;

namespace RemoteCare.Api.Dtos.Device
{
    public class GenerateQrRequest
    {
        [Required(ErrorMessage = "Device ID is required")]
        public string DeviceId { get; set; }
    }
}
