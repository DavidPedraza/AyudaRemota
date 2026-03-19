namespace RemoteCare.Api.Models
{
    public class Session
    {
        public int Id { get; set; }
        public string SessionId { get; set; } // UUID
        public int SeniorDeviceId { get; set; }
        public int SupportDeviceId { get; set; }
        public string Status { get; set; } // Active, Completed, Disconnected, Error
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime? EndedAt { get; set; }

        public int FramesSent { get; set; } = 0;
        public int FramesReceived { get; set; } = 0;
        public long BytesTransferred { get; set; } = 0;
        public int AverageLatencyMs { get; set; } = 0;

        public bool IsEncrypted { get; set; } = true;
        public string EndReason { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public Device SeniorDevice { get; set; }
        public Device SupportDevice { get; set; }
    }
}
