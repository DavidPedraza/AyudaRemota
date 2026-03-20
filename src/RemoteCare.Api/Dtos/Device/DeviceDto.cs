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