using NPark.Domain.Enums;
using BuildingBlock.Domain.Specification;
using NPark.Application.Feature.TicketsManagement.Query.GetTicketInsidePark;
using NPark.Domain.Entities;

namespace NPark.Application.Specifications.TicketSpecification
{
    public sealed class GetTicketInsideParkSpecification : Specification<Ticket, GetTicketInsideParkQueryResponse>
    {
        public GetTicketInsideParkSpecification(Guid[] acknowledgedExitIds)
        {
            AddCriteria(t => t.EndDate == null &&
                t.EntryConfirmationStatus != EntryConfirmationStatus.Pending && !acknowledgedExitIds.Contains(t.Id));
            Select(x => new GetTicketInsideParkQueryResponse
            {
                isCollectedByCashier = x.IsCashierCollected,
                Price = x.Price,
                StartDate = x.StartDate,
                TicketNumber = x.UniqueCode,
                TicketInfo = x.VehicleNumber,
                Id = x.Id
            }
            );
        }
    }
}