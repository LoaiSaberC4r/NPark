using BuildingBlock.Application.Abstraction;
using BuildingBlock.Domain.SharedDto;
using NPark.Application.Feature.TicketsManagement.Query.GetExitTicketForAdmin;

namespace NPark.Application.Feature.TicketsManagement.Query.GetTicketDetails
{
    public sealed class GetTicketDetailsQuery : SearchParameters, IQuery<Pagination<GetTicketDetailsQueryResponse>>
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        public Guid? GateId { get; set; }
        public bool? IsCollected { get; set; }
        public bool? IsSubscriber { get; set; }
    }
}