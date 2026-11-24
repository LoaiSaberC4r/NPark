using BuildingBlock.Domain.Specification;
using NPark.Domain.Entities;

namespace NPark.Application.Feature.TicketsManagement.Command.ValidateExitByText
{
    public sealed class TicketByCardNumberSpecification : Specification<Ticket>
    {
        public TicketByCardNumberSpecification(string cardNumber, string? normalizedCardNumber = null)
        {
            AddCriteria(t => t.CardNumber != null &&
                (t.CardNumber == cardNumber || t.CardNumber == normalizedCardNumber) &&
                t.EndDate == null);
            AddOrderByDescending(t => t.StartDate);
        }
    }
}
