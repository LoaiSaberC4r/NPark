namespace CRM.Application.Shared.Dto
{
    public sealed record AdminDashboardDto
    {
        public long TotalClients { get; set; }
        public long TotalPendingTickets { get; set; }
        public long TotalAssignedTickets { get; set; }
        public long TotalClosedTickets { get; set; }
        public long TotalProductsEndedWithing30Days { get; set; }
        public long TtoalProductsEnded { get; set; }
    }
}