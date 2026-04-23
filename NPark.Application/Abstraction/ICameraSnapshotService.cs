using BuildingBlock.Domain.Results;

namespace NPark.Application.Abstraction
{
    public interface ICameraSnapshotService
    {
        Task<Result<CameraSnapshotResult>> GetSnapshotAsync(
            string url,
            string userName,
            string password,
            CancellationToken cancellationToken = default);
    }

    public sealed class CameraSnapshotResult
    {
        public byte[] Content { get; init; } = Array.Empty<byte>();
        public string ContentType { get; init; } = "image/jpeg";
        public string? FileName { get; init; }
    }
}