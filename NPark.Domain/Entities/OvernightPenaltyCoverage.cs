namespace NPark.Domain.Entities;

public sealed class OvernightPenaltyCoverage
{
    private OvernightPenaltyCoverage() { }

    public OvernightPenaltyCoverage(Guid ticketId, DateOnly businessDate, Guid paymentId)
    {
        TicketIdSnapshot = ticketId;
        BusinessDate = businessDate;
        PaymentId = paymentId;
    }

    public Guid TicketIdSnapshot { get; private set; }
    public DateOnly BusinessDate { get; private set; }
    public Guid PaymentId { get; private set; }
}
