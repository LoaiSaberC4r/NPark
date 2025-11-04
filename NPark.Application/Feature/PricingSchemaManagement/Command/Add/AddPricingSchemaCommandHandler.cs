using BuildingBlock.Application.Abstraction;
using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
using Microsoft.Extensions.Logging;
using NPark.Domain.Entities;

namespace NPark.Application.Feature.PricingSchemaManagement.Command.Add
{
    public class AddPricingSchemaCommandHandler : ICommandHandler<AddPricingSchemaCommand>
    {
        private readonly IGenericRepository<PricingScheme> _repository;
        private readonly ILogger<AddPricingSchemaCommandHandler> _logger;

        public AddPricingSchemaCommandHandler(IGenericRepository<PricingScheme> repository, ILogger<AddPricingSchemaCommandHandler> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result> Handle(AddPricingSchemaCommand request, CancellationToken cancellationToken)
        {
            if (request.IsRepeated)
            {
                var repeatedEntity = PricingScheme.Create(
                    request.Name,
                    Domain.Enums.DurationType.Hours,
                    null,
                    null,
                    request.IsRepeated,
                    request.Price,
                    request.OrderPriority,
                     null, request.TotalHours);

                _logger.LogInformation("Added repeated entity at {DateTime}", DateTime.UtcNow);

                await _repository.AddAsync(repeatedEntity, cancellationToken);

                await _repository.SaveChangesAsync(cancellationToken);
                return Result.Ok();
            }
            if (request.DurationType == Domain.Enums.DurationType.Hours)
            {
                var entityHour = PricingScheme.Create(
                request.Name,
                request.DurationType,
                request.StartTime,
                request.EndTime,
                request.IsRepeated,
                request.Price,
                null, null, request.TotalHours);
                await _repository.AddAsync(entityHour, cancellationToken);
                await _repository.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Added  entity by hours at {DateTime}", DateTime.UtcNow);
                return Result.Ok();
            }

            var entityDays = PricingScheme.Create(
                request.Name,
                request.DurationType,
                request.StartTime,
                request.EndTime,
                request.IsRepeated,
                request.Price,
                null, request.TotalDays, null);

            await _repository.AddAsync(entityDays, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Added entity by days at {DateTime}", DateTime.UtcNow);
            return Result.Ok();
        }
    }
}