namespace RemoteCare.Api.Services
{
    public interface IQrGeneratorService
    {
        string GenerateQrCode(string data, out byte[] qrImage);
        string GeneratePairingCode();
    }
}
