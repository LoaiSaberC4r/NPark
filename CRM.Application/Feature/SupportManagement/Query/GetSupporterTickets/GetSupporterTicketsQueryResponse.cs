using CRM.Application.Feature.TicketManagement.Query.GetAllSuperAdmin;

namespace CRM.Application.Feature.SupportManagement.Query.GetSupporterTickets
{
    public sealed record GetSupporterTicketsQueryResponse
    {
        public IReadOnlyCollection<GetAllSuperAdminQueryResponse> AssignedTickets { get; init; } =
            new List<GetAllSuperAdminQueryResponse>();
        public IReadOnlyCollection<GetAllSuperAdminQueryResponse> ClosedTickets { get; init; } =
             new List<GetAllSuperAdminQueryResponse>();
        public IReadOnlyCollection<GetAllSuperAdminQueryResponse> RejectedTickets { get; init; } =
             new List<GetAllSuperAdminQueryResponse>();
    }
}