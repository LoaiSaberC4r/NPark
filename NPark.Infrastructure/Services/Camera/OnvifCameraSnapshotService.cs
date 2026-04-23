using BuildingBlock.Domain.Results;
using Microsoft.Extensions.Logging;
using NPark.Application.Abstraction;
using System.Net.Http.Headers;
using System.Text;

namespace NPark.Infrastructure.Services.Camera
{
    internal sealed class OnvifCameraSnapshotService : ICameraSnapshotService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<OnvifCameraSnapshotService> _logger;

        public OnvifCameraSnapshotService(
            HttpClient httpClient,
            ILogger<OnvifCameraSnapshotService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<Result<CameraSnapshotResult>> GetSnapshotAsync(
            string url,
            string userName,
            string password,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return Result<CameraSnapshotResult>.Fail(
                    Error.Unknown("Camera.Url.Required", "Camera url is required."));
            }

            if (string.IsNullOrWhiteSpace(userName))
            {
                return Result<CameraSnapshotResult>.Fail(
                    Error.Unknown("Camera.UserName.Required", "Camera user name is required."));
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                return Result<CameraSnapshotResult>.Fail(
                    Error.Unknown("Camera.Password.Required", "Camera password is required."));
            }

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, url);

                var basicToken = Convert.ToBase64String(
                    Encoding.ASCII.GetBytes($"{userName}:{password}"));

                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Basic", basicToken);

                using var response = await _httpClient.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);

                    _logger.LogWarning(
                        "Camera direct snapshot failed. Url={Url}, StatusCode={StatusCode}, Body={Body}",
                        url,
                        (int)response.StatusCode,
                        errorBody);

                    return Result<CameraSnapshotResult>.Fail(
                        Error.Unknown(
                            "Camera.Snapshot.DownloadFailed",
                            $"Camera snapshot download failed with status code {(int)response.StatusCode}."));
                }

                var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
                var contentType = response.Content.Headers.ContentType?.MediaType ?? "image/jpeg";

                if (bytes.Length == 0)
                {
                    return Result<CameraSnapshotResult>.Fail(
                        Error.Unknown("Camera.Snapshot.Empty", "Camera returned empty snapshot content."));
                }

                return Result<CameraSnapshotResult>.Ok(new CameraSnapshotResult
                {
                    Content = bytes,
                    ContentType = contentType,
                    FileName = $"camera_snapshot_{DateTime.UtcNow:yyyyMMdd_HHmmss}.jpg"
                });
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Timeout while getting camera snapshot. Url={Url}", url);

                return Result<CameraSnapshotResult>.Fail(
                    Error.Unknown("Camera.Timeout", "Timeout while connecting to camera."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while getting direct camera snapshot. Url={Url}", url);

                return Result<CameraSnapshotResult>.Fail(
                    Error.Unknown("Camera.UnexpectedError", "Unexpected camera integration error."));
            }
        }
    }
}