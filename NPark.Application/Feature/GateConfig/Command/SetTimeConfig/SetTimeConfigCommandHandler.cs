using BuildingBlock.Application.Abstraction;
using BuildingBlock.Domain.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NPark.Application.Abstraction;
using NPark.Application.Shared.Dto;

namespace NPark.Application.Feature.GateConfig.Command.SetTimeConfig
{
    public class SetTimeConfigCommandHandler : ICommandHandler<SetTimeConfigCommand>
    {
        private readonly ISendProtocol _sendProtocol;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<SetTimeConfigCommandHandler> _logger;

        public SetTimeConfigCommandHandler(ISendProtocol sendProtocol, IHttpContextAccessor httpContextAccessor, ILogger<SetTimeConfigCommandHandler> logger)
        {
            _sendProtocol = sendProtocol ?? throw new ArgumentNullException(nameof(sendProtocol));
            _httpContextAccessor = httpContextAccessor;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result> Handle(SetTimeConfigCommand request, CancellationToken cancellationToken)
        {
            // var host = GetHostDomain();
            var host = "192.168.1.62";
            if (host == null)
            {
                return Result.Fail(new Error("HttpContextUnavailable", "Unable to access HttpContext to determine host domain.",
                    ErrorType.Security));
            }
            var packet = StartConfigPacket.FromDateTime(new DateTime(2010, 3, 15, 8, 45, 30), gracePeriod: 10, gateNo: 1).ToFullPacket();
            _logger.LogInformation("Sending packet {packet} to {host}", packet, host);
            var okHttp = await _sendProtocol.SendHttpBinaryAsync(host, packet, cancellationToken);
            if (!okHttp)
            {
                return Result.Fail(new Error("Failed to send Http request", $"Unable to send Http request to the host domain{host}.",
                 ErrorType.Security));
            }

            return Result.Ok();
        }

        private string? GetHostDomain()
        {
            var http = _httpContextAccessor.HttpContext;
            if (http == null) return null;

            var scheme = http.Request.Scheme;
            var host = http.Request.Host.Value;

            return $"{scheme}://{host}";
        }
    }
}