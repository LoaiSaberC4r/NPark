namespace NPark.Application.Abstraction
{
    public interface ISendProtocol
    {
        Task<bool> SendHttpBinaryAsync(
           string hostOrIp,
           byte[] bytes,
           CancellationToken ct = default,
           string pathOrRoute = "/config",
           int timeout = 3000
           );
    }
}