using BuildingBlock.Domain.EntitiesHelper;

namespace NPark.Domain.Entities
{
    public sealed class TicketArchiveAttachment : Entity<Guid>
    {
        public Guid TicketArchiveId { get; private set; }

        public TicketArchive TicketArchive { get; private set; } = default!;

        public string FilePath { get; private set; } = string.Empty;

        private TicketArchiveAttachment()
        {
        }

        public static TicketArchiveAttachment Create(Guid ticketArchiveId, string filePath)
        {
            if (ticketArchiveId == Guid.Empty)
                throw new ArgumentException("Ticket archive id is required.", nameof(ticketArchiveId));

            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path is required.", nameof(filePath));

            return new TicketArchiveAttachment
            {
                Id = Guid.NewGuid(),
                TicketArchiveId = ticketArchiveId,
                FilePath = filePath.Trim()
            };
        }
    }
}