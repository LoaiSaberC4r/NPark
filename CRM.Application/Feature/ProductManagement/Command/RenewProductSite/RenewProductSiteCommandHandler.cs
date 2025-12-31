using BuildingBlock.Application.Abstraction;
using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
using CRM.Application.Specification.ProductSpecification;
using CRM.Domain.Entities;
using CRM.Domain.Resources;

namespace CRM.Application.Feature.ProductManagement.Command.RenewProductSite
{
    internal sealed record RenewProductSiteCommandHandler : ICommandHandler<RenewProductSiteCommand>
    {
        private readonly IGenericRepository<SiteProduct> _siteProductRepository;
        public RenewProductSiteCommandHandler(IGenericRepository<SiteProduct> siteProductRepository)
        {
            _siteProductRepository = siteProductRepository ?? throw new ArgumentNullException(nameof(siteProductRepository));
        }
        public async Task<Result> Handle(RenewProductSiteCommand request, CancellationToken cancellationToken)
        {
            var spec = new GetSiteProductByIdSpec(request.SiteProductId);
            var siteProduct = await _siteProductRepository.FirstOrDefaultWithSpecAsync(spec, cancellationToken);

            if (siteProduct is null)
            {
                return Result.Fail(new Error(
                    Code: "SiteProduct.NotFound",
                    Message: ErrorMessage.SiteProduct_NotFound,
                    Type: ErrorType.NotFound
                ));
            }

            // Domain rule: renewal date must be > current SupportEndDate
            if (request.RenewalDate <= siteProduct.SupportEndDate)
            {
                return Result.Fail(new Error(
                    Code: "SiteProduct.RenewalDate.MustBeLaterThanCurrent",
                    Message: ErrorMessage.SiteProduct_RenewalDate_MustBeLaterThanCurrent,
                    Type: ErrorType.Validation
                ));
            }
            siteProduct.RenewSupport(request.RenewalDate);
            siteProduct.Enroll(DateTime.Now);
            await _siteProductRepository.SaveChangesAsync(cancellationToken);
            return Result.Ok();
        }
    }
}