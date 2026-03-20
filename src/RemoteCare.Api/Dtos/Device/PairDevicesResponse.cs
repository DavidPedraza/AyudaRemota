namespace RemoteCare.Api.Dtos.Device
{
    public class PairDevicesResponse
    {
        public int SessionId { get; set; }
        public string SessionUuid { get; set; }
        public DeviceDto SeniorDevice { get; set; }
        public DeviceDto SupportDevice { get; set; }
        public string Status { get; set; }
        public DateTime ConnectedAt { get; set; }
        public string WsUrl { get; set; }
    }
}
