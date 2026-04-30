using BuildingBlock.Domain.Specification;
using NPark.Domain.Entities;

namespace NPark.Application.Feature.TicketsManagement.Command.ExitTicket
{
    internal sealed class GetTicketForExitArchiveSpec : Specification<Ticket>
    {
        public GetTicketForExitArchiveSpec(Guid ticketId)
        {
            AddCriteria(x => x.Id == ticketId);

            Include(x => x.TicketAttachments);

            // Optional, but useful because TicketArchive currently copies navigation references.
            Include(x => x.User);
            Include(x => x.UserCollector);
            Include(x => x.ParkingGate);
        }
    }
}