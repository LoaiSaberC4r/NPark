using BuildingBlock.Domain.Specification;
using NPark.Domain.Entities;

namespace NPark.Application.Feature.TicketsManagement.Command.Add
{
    public sealed class GetActiveSubscriberTicketSpec : Specification<Ticket>
    {
        public GetActiveSubscriberTicketSpec(
            string? cardNumber,
            string? subscriberNationalId,
            string? vehicleNumber)
        {
            AddCriteria(x =>
                x.IsSubscriber &&
                x.EndDate == null &&
                (
                    (!string.IsNullOrWhiteSpace(cardNumber) && x.CardNumber == cardNumber) ||
                    (!string.IsNullOrWhiteSpace(subscriberNationalId) && x.SubscriberNationalId == subscriberNationalId) ||
                    (!string.IsNullOrWhiteSpace(vehicleNumber) && x.VehicleNumber == vehicleNumber)
                ));
        }
    }
}