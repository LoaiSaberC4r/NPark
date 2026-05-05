using BuildingBlock.Application.Abstraction.QrCode;
using QRCoder;
using System.Text.Json;

namespace BuildingBlock.Infrastracture.Service
{
    public class QRCodeService : IQRCodeService
    {
        public byte[] GenerateQRCode<T>(T data)
        {
            var jsonData = JsonSerializer.Serialize(data);

            return GenerateQrPng(jsonData);
        }

        public byte[] GenerateQRCodeFromText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentException("QR text must not be empty.", nameof(text));

            return GenerateQrPng(text.Trim());
        }

        private static byte[] GenerateQrPng(string payload)
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new QRCode(qrCodeData);
            using var ms = new MemoryStream();

            qrCode.GetGraphic(20).Save(ms, System.Drawing.Imaging.ImageFormat.Png);

            return ms.ToArray();
        }
    }
}