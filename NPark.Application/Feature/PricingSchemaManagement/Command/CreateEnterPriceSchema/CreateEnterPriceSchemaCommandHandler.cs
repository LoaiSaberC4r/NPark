using BuildingBlock.Application.Abstraction;
using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
using Microsoft.Extensions.Logging;
using NPark.Domain.Entities;
using NPark.Domain.Resource;

namespace NPark.Application.Feature.PricingSchemaManagement.Command.CreateEnterPriceSchema
{
    internal sealed class CreateEnterPriceSchemaCommandHandler
       : ICommandHandler<CreateEnterPriceSchemaCommand, CreateEnterPriceSchemaCommandResponse>
    {
        private readonly IGenericRepository<PricingScheme> _pricingSchemeRepository;
        private readonly ILogger<CreateEnterPriceSchemaCommandHandler> _logger;

        public CreateEnterPriceSchemaCommandHandler(
            IGenericRepository<PricingScheme> pricingSchemeRepository,
            ILogger<CreateEnterPriceSchemaCommandHandler> logger)
        {
            _pricingSchemeRepository = pricingSchemeRepository
                ?? throw new ArgumentNullException(nameof(pricingSchemeRepository));

            _logger = logger
                ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<CreateEnterPriceSchemaCommandResponse>> Handle(
            CreateEnterPriceSchemaCommand request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var name = request.Name.Trim();

                var duplicateSpec = new EnterPriceSchemaByNameSpec(name);

                var existingSchema = await _pricingSchemeRepository
                    .FirstOrDefaultWithSpecAsync(duplicateSpec, cancellationToken);

                if (existingSchema is not null)
                {
                    return Result<CreateEnterPriceSchemaCommandResponse>.Fail(
                        new Error(
                            Code: "EnterPriceSchema.Name.AlreadyExists",
                            Message: ErrorMessage.EnterPriceSchema_Name_AlreadyExists,
                            Type: ErrorType.Conflict));
                }

                var entity = PricingScheme.CreateEnterPriceSchema(
                    name: name,
                    salary: request.Salary);

                await _pricingSchemeRepository.AddAsync(entity, cancellationToken);
                await _pricingSchemeRepository.SaveChangesAsync(cancellationToken);

                var response = new CreateEnterPriceSchemaCommandResponse
                {
                    Id = entity.Id,
                    Name = entity.Name,
                    Salary = entity.Salary,
                    IsEnterPriceSchema = entity.IsEnterPriceSchema
                };

                return Result<CreateEnterPriceSchemaCommandResponse>.Ok(response);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("CreateEnterPriceSchema operation was canceled.");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while creating enter price schema.");

                return Result<CreateEnterPriceSchemaCommandResponse>.Fail(
                    new Error(
                        Code: "EnterPriceSchema.Create.Unexpected",
                        Message: ErrorMessage.EnterPriceSchema_Create_Unexpected,
                        Type: ErrorType.Infrastructure));
            }
        }
    }
}