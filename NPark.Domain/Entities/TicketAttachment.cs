using BuildingBlock.Domain.EntitiesHelper;

namespace NPark.Domain.Entities
{
    public sealed class TicketAttachment : Entity<Guid>
    {
        public Guid TicketId { get; set; }
        public Ticket Ticket { get; set; } = null!;
        public string FilePath { get; set; } = string.Empty;

        private TicketAttachment()
        { }

        public static TicketAttachment Create(Guid ticketId, string filePath) => new TicketAttachment()
        {
            TicketId = ticketId,
            FilePath = filePath
        };
    }
}