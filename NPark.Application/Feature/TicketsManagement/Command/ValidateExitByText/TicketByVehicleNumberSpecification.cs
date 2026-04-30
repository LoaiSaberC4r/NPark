using BuildingBlock.Domain.Specification;
using NPark.Domain.Entities;

namespace NPark.Application.Feature.TicketsManagement.Command.ValidateExitByText
{
    public sealed class TicketByVehicleNumberSpecification : Specification<Ticket>
    {
        public TicketByVehicleNumberSpecification(string vehicleNumber)
        {
            AddCriteria(t =>
                t.VehicleNumber != null &&
                t.VehicleNumber.Trim().Replace(" ", "") == vehicleNumber.Trim().Replace(" ", "")

                && t.EndDate == null);

            AddOrderByDescending(t => t.StartDate);
        }
    }
}