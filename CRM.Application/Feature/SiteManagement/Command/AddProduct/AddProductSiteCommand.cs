using BuildingBlock.Application.Abstraction;

namespace CRM.Application.Feature.SiteManagement.Command.AddProduct
{
    public sealed record AddProductSiteCommand : ICommand
    {
        public Guid SiteId { get; init; }
        public Guid ProductId { get; init; }
        public DateTime SupportEndDate { get; init; }
    }
}