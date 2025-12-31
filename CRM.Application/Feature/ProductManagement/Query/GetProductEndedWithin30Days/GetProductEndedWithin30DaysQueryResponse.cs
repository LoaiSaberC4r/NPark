namespace CRM.Application.Feature.ProductManagement.Query.GetProductEndedWithin30Days
{
    public class GetProductEndedWithin30DaysQueryResponse
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