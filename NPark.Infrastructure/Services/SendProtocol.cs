using Microsoft.Extensions.Logging;
using NPark.Application.Abstraction;
using System.Net.Http.Headers;

namespace NPark.Infrastructure.Services
{
    public class SendProtocol : ISendProtocol
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<SendProtocol> _logger;

        public SendProtocol(IHttpClientFactory httpClientFactory, ILogger<SendProtocol> logger)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> SendHttpBinaryAsync(
        string hostOrIp,
        byte[] bytes,
           CancellationToken ct = default,
        string pathOrRoute = "/config",
        int timeout = 3000
        )
        {
            try
            {
                if (bytes is null || bytes.Length == 0)
                {
                    _logger.LogWarning("HTTP binary send called with empty payload. Host: {Host} with payload: {Payload}", hostOrIp, bytes);
                    return false;
                }

                var host = hostOrIp.Trim();
                var path = NormalizeRoute(pathOrRoute);

                var client = _httpClientFactory.CreateClient("device-http");
                client.Timeout = TimeSpan.FromMilliseconds(timeout);

                var uri = new UriBuilder(Uri.UriSchemeHttp, host, -1, path).Uri;

                using var content = new ByteArrayContent(bytes);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                content.Headers.ContentLength = bytes.Length;

                using var req = new HttpRequestMessage(HttpMethod.Post, uri)
                {
                    Content = content
                };
                req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
                req.Headers.ConnectionClose = true;

                using var resp = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct)
                                             .ConfigureAwait(false);

                if (!resp.IsSuccessStatusCode)
                {
                    _logger.LogWarning("HTTP binary send failed. Url: {Url} Status: {Status}", uri, (int)resp.StatusCode);
                    return false;
                }

                _logger.LogInformation("HTTP binary send success. Url: {Url} Bytes: {payload} Status: {Status}", uri, bytes, (int)resp.StatusCode);
                return true;
            }
            catch (OperationCanceledException oce) when (!ct.IsCancellationRequested)
            {
                _logger.LogWarning(oce, "HTTP binary send timed out. Host: {Host} Timeout: {Timeout}ms", hostOrIp, timeout);
                return false;
            }
            catch (HttpRequestException hre)
            {
                _logger.LogError(hre, "HTTP binary request error to {Host}", hostOrIp);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected HTTP binary error to {Host}", hostOrIp);
                return false;
            }
        }

        private static string NormalizeRoute(string? path)
        {
            if (string.IsNullOrWhiteSpace(path)) return "/";
            path = path.Trim();
            if (!path.StartsWith("/")) path = "/" + path;
            return path;
        }
    }
}