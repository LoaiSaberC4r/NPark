using BuildingBlock.Application.Abstraction;
using BuildingBlock.Domain.Results;
using Microsoft.Extensions.Logging;

namespace NPark.Application.Feature.DespanserManagement.Command.ExitAck
{
    public class ExitAckCommandHandler : ICommandHandler<ExitAckCommand>
    {
        private readonly ILogger<ExitAckCommandHandler> _logger;

        public ExitAckCommandHandler(ILogger<ExitAckCommandHandler> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result> Handle(ExitAckCommand request, CancellationToken cancellationToken)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(request);
            _logger.LogWarning("ExitAckCommandHandler is called {Payload}", json);
            return Result.Ok();
        }
    }
}