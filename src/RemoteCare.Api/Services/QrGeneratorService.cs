using QRCoder;

namespace RemoteCare.Api.Services
{
    public class QrGeneratorService : IQrGeneratorService
    {
        public string GenerateQrCode(string data, out byte[] qrImage)
        {
            using (var qrGenerator = new QRCodeGenerator())
            {
                var qrCodeData = qrGenerator.CreateQrCode(data, QRCodeGenerator.ECCLevel.Q);
                using (var qrCode = new PngByteQRCode(qrCodeData))
                {
                    qrImage = qrCode.GetGraphic(10);
                    return Convert.ToBase64String(qrImage);
                }
            }
        }

        public string GeneratePairingCode()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            var code = "QR-" + new string(Enumerable.Range(0, 12)
                .Select(_ => chars[random.Next(chars.Length)])
                .ToArray());
            return code;
        }
    }
}
