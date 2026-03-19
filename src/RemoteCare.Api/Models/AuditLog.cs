namespace RemoteCare.Api.Models
{
    public class AuditLog
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Action { get; set; }
        public string Resource { get; set; }
        public int? ResourceId { get; set; }
        public string Result { get; set; }
        public string Details { get; set; }
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public User User { get; set; }
    }
}
