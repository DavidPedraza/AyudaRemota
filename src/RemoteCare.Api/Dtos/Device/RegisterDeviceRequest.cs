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
