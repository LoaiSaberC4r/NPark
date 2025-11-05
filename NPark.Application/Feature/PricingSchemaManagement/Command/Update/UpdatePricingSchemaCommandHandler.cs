using BuildingBlock.Application.Abstraction;
using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
using Microsoft.Extensions.Logging;
using NPark.Domain.Entities;

namespace NPark.Application.Feature.PricingSchemaManagement.Command.Update
{
    public sealed class UpdatePricingSchemaCommandHandler : ICommandHandler<UpdatePricingSchemaCommand>
    {
        private readonly IGenericRepository<PricingScheme> _repository;
        private readonly ILogger<UpdatePricingSchemaCommandHandler> _logger;

        public UpdatePricingSchemaCommandHandler(IGenericRepository<PricingScheme> repository, ILogger<UpdatePricingSchemaCommandHandler> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result> Handle(UpdatePricingSchemaCommand request, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(request.Id, cancellationToken);
            if (request.IsRepeated)
            {
                entity!.Update(
                      request.Name,
                      Domain.Enums.DurationType.Hours,
                      null,
                      null,
                      request.IsRepeated,
                      request.Price,
                       null, request.TotalHours);

                await _repository.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Update repeated entity at {DateTime}", DateTime.UtcNow);
                return Result.Ok();
            }
            if (request.DurationType == Domain.Enums.DurationType.Hours)
            {
                entity!.Update(
                  request.Name,
                  request.DurationType,
                  request.StartTime,
                  request.EndTime,
                  request.IsRepeated,
                  request.Price,
                  null, request.TotalHours);
                await _repository.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Updated  entity by hours at {DateTime}", DateTime.UtcNow);
                return Result.Ok();
            }
            else if (request.DurationType == Domain.Enums.DurationType.Days)
            {
                entity!.Update(
            request.Name,
            request.DurationType,
            request.StartTime,
            request.EndTime,
            request.IsRepeated,
            request.Price,
             request.TotalDays, null);

                await _repository.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Updated entity by days at {DateTime}", DateTime.UtcNow);
                return Result.Ok();
            }
            else
            {
                entity!.Update(
                       request.Name,
                       request.DurationType,
                       request.StartTime,
                       request.EndTime,
                       request.IsRepeated,
                       request.Price,
                      365, null);

                await _repository.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Updated entity by days at {DateTime}", DateTime.UtcNow);
                return Result.Ok();
            }
        }
    }
}