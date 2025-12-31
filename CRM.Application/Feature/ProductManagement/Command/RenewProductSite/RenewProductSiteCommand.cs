using BuildingBlock.Application.Abstraction;

namespace CRM.Application.Feature.ProductManagement.Command.RenewProductSite
{
    public sealed record RenewProductSiteCommand : ICommand
    {
        public Guid SiteProductId { get; init; }
        public DateTime RenewalDate { get; init; }
    }
}