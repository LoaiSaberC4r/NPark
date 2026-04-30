namespace BuildingBlock.Application.Abstraction.QrCode
{
    public interface IQRCodeService
    {
        byte[] GenerateQRCode<T>(T data);

        byte[] GenerateQRCodeFromText(string text);
    }
}