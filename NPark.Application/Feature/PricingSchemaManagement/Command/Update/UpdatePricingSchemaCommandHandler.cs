using BuildingBlock.Application.Abstraction;
using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
using Microsoft.Extensions.Logging;
using NPark.Domain.Entities;
using NPark.Domain.Enums;
using NPark.Domain.Resource;

namespace NPark.Application.Feature.PricingSchemaManagement.Command.Update
{
    public sealed class UpdatePricingSchemaCommandHandler : ICommandHandler<UpdatePricingSchemaCommand>
    {
        private readonly IGenericRepository<PricingScheme> _repository;
        private readonly ILogger<UpdatePricingSchemaCommandHandler> _logger;

        public UpdatePricingSchemaCommandHandler(
            IGenericRepository<PricingScheme> repository,
            ILogger<UpdatePricingSchemaCommandHandler> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result> Handle(
            UpdatePricingSchemaCommand request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var entity = await _repository.GetByIdAsync(request.Id, cancellationToken);

            if (entity is null)
            {
                return Result.Fail(
                    new Error(
                        Code: "PricingScheme.NotFound",
                        Message: ErrorMessage.NotFound,
                        Type: ErrorType.NotFound));
            }

            /*
             * EnterPriceSchema update:
             * لو الريكويست داخل كـ EnterPriceSchema
             * أو الـ entity نفسها أصلًا EnterPriceSchema
             * يبقى نثبت كل القيم حسب business rule بتاع سعر الدخول.
             */
            if (request.IsEnterPriceSchema || entity.IsEnterPriceSchema)
            {
                entity.UpdateEnterPriceSchema(
                    name: request.Name,
                    salary: request.Price);

                await _repository.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Updated enter price schema. PricingSchemeId={PricingSchemeId}, DateTime={DateTime}",
                    entity.Id,
                    DateTime.UtcNow);

                return Result.Ok();
            }

            if (request.IsRepeated)
            {
                entity.Update(
                    name: request.Name,
                    durationType: DurationType.Hours,
                    startTime: null,
                    endTime: null,
                    isRepeated: true,
                    salary: request.Price,
                    totalDays: null,
                    totalHours: request.TotalHours);

                await _repository.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Updated repeated pricing scheme. PricingSchemeId={PricingSchemeId}, DateTime={DateTime}",
                    entity.Id,
                    DateTime.UtcNow);

                return Result.Ok();
            }

            if (request.DurationType == DurationType.Hours)
            {
                entity.Update(
                    name: request.Name,
                    durationType: request.DurationType,
                    startTime: request.StartTime,
                    endTime: request.EndTime,
                    isRepeated: false,
                    salary: request.Price,
                    totalDays: null,
                    totalHours: request.TotalHours);

                await _repository.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Updated pricing scheme by hours. PricingSchemeId={PricingSchemeId}, DateTime={DateTime}",
                    entity.Id,
                    DateTime.UtcNow);

                return Result.Ok();
            }

            if (request.DurationType == DurationType.Days)
            {
                entity.Update(
                    name: request.Name,
                    durationType: request.DurationType,
                    startTime: request.StartTime,
                    endTime: request.EndTime,
                    isRepeated: false,
                    salary: request.Price,
                    totalDays: request.TotalDays,
                    totalHours: null);

                await _repository.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Updated pricing scheme by days. PricingSchemeId={PricingSchemeId}, DateTime={DateTime}",
                    entity.Id,
                    DateTime.UtcNow);

                return Result.Ok();
            }

            entity.Update(
                name: request.Name,
                durationType: request.DurationType,
                startTime: request.StartTime,
                endTime: request.EndTime,
                isRepeated: false,
                salary: request.Price,
                totalDays: 365,
                totalHours: null);

            await _repository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Updated yearly pricing scheme. PricingSchemeId={PricingSchemeId}, DateTime={DateTime}",
                entity.Id,
                DateTime.UtcNow);

            return Result.Ok();
        }
    }
}