namespace RemoteCare.Api.Dtos.Device
{
    public class GenerateQrResponse
    {
        public string PairingCode { get; set; }
        public string QrData { get; set; } // Base64 encoded PNG
        public int ExpiresIn { get; set; } // seconds
        public DateTime ExpiresAt { get; set; }
    }
}
