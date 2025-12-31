namespace CRM.Application.Feature.ProductManagement.Query.GetClosedProducts
{
    public sealed record GetClosedProductsResponse
    {
        public Guid SiteProductId { get; set; }
        public string? SiteNameAr { get; set; }
        public string SiteNameEn { get; set; } = string.Empty;
        public string? ProductNameAr { get; set; }
        public string ProductNameEn { get; set; } = string.Empty;
        public DateTime EndDate { get; set; }
        public string? AccountNameAr { get; set; }
        public string AccountNameEn { get; set; } = string.Empty;
    }
}