using BuildingBlock.Application.Abstraction;
using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
using CRM.Domain.Entities;

namespace CRM.Application.Feature.SiteManagement.Command.AddProduct
{
    public sealed record AddProductSiteCommandHandler : ICommandHandler<AddProductSiteCommand>
    {
        private readonly IGenericRepository<SiteProduct> _siteProductRepository;
        private readonly IGenericRepository<Site> _siteRepository;
        private readonly IGenericRepository<Product> _productRepository;
        public AddProductSiteCommandHandler(IGenericRepository<SiteProduct> siteProductRepository, IGenericRepository<Site> siteRepository, IGenericRepository<Product> productRepository)
        {
            _siteProductRepository = siteProductRepository ?? throw new ArgumentNullException(nameof(siteProductRepository));
            _siteRepository = siteRepository ?? throw new ArgumentNullException(nameof(siteRepository));
            _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        }
        public async Task<Result> Handle(AddProductSiteCommand request, CancellationToken cancellationToken)
        {
            bool siteExists = await _siteRepository.IsExistAsync(x => x.Id == request.SiteId, cancellationToken);
            bool productExists = await _productRepository.IsExistAsync(x => x.Id == request.ProductId, cancellationToken);
            if (!siteExists || !productExists)
                return Result.Fail(new Error(
                    Code: "SiteProduct.NotFound",
                    Message: "Site or Product not found"
                    , Type: ErrorType.NotFound
                    ));

            var siteProduct = SiteProduct.Create(
                siteId: request.SiteId,
                productId: request.ProductId,
                supportEndDate: request.SupportEndDate
                );

            await _siteProductRepository.AddAsync(siteProduct, cancellationToken);
            await _siteProductRepository.SaveChangesAsync(cancellationToken);
            return Result.Ok();
        }
    }
}