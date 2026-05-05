using BuildingBlock.Application.Abstraction;
using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
using Microsoft.Extensions.Logging;
using NPark.Domain.Entities;
using NPark.Domain.Resource;

namespace NPark.Application.Feature.PricingSchemaManagement.Query.GetEnterPriceSchemasForSelection
{
    internal sealed class GetEnterPriceSchemasForSelectionQueryHandler
       : IQueryHandler<GetEnterPriceSchemasForSelectionQuery, IReadOnlyList<GetEnterPriceSchemasForSelectionResponse>>
    {
        private readonly IGenericRepository<PricingScheme> _pricingSchemeRepository;
        private readonly ILogger<GetEnterPriceSchemasForSelectionQueryHandler> _logger;

        public GetEnterPriceSchemasForSelectionQueryHandler(
            IGenericRepository<PricingScheme> pricingSchemeRepository,
            ILogger<GetEnterPriceSchemasForSelectionQueryHandler> logger)
        {
            _pricingSchemeRepository = pricingSchemeRepository
                ?? throw new ArgumentNullException(nameof(pricingSchemeRepository));

            _logger = logger
                ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<IReadOnlyList<GetEnterPriceSchemasForSelectionResponse>>> Handle(
            GetEnterPriceSchemasForSelectionQuery request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var spec = new GetEnterPriceSchemasForSelectionSpec();

                var schemas = await _pricingSchemeRepository
                    .ListWithSpecAsync(spec, cancellationToken);

                return Result<IReadOnlyList<GetEnterPriceSchemasForSelectionResponse>>.Ok(schemas);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("GetEnterPriceSchemasForSelection operation was canceled.");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while getting enter price schemas for selection.");

                return Result<IReadOnlyList<GetEnterPriceSchemasForSelectionResponse>>.Fail(
                    new Error(
                        Code: "EnterPriceSchema.Selection.Unexpected",
                        Message: ErrorMessage.EnterPriceSchema_Selection_Unexpected,
                        Type: ErrorType.Infrastructure));
            }
        }
    }
}