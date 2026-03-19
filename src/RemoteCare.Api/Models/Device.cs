namespace RemoteCare.Api.Models
{
    public class Device
    {
        public int Id { get; set; }
        public string DeviceId { get; set; } // UUID from Android
        public string DeviceName { get; set; }
        public int UserId { get; set; }
        public string Role { get; set; } // Senior, Support
        public string OsVersion { get; set; }
        public string Manufacturer { get; set; }
        public string Model { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastSeen { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public User User { get; set; }
        public ICollection<Session> SessionsAsSenior { get; set; } = new List<Session>();
        public ICollection<Session> SessionsAsSupport { get; set; } = new List<Session>();
    }
}
