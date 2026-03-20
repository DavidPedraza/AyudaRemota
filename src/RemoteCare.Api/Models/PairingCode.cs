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
