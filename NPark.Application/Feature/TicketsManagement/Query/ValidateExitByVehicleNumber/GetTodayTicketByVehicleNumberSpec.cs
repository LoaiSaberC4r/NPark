using BuildingBlock.Domain.Specification;
using NPark.Domain.Entities;

namespace NPark.Application.Feature.TicketsManagement.Query.ValidateExitByVehicleNumber
{
    internal sealed class GetTodayTicketByVehicleNumberSpec
         : Specification<Ticket>
    {
        public GetTodayTicketByVehicleNumberSpec(string vehicleNumber, DateTime dayStart, DateTime dayEnd)
        {
            AddCriteria(x =>
                x.VehicleNumber != null &&
                x.VehicleNumber == vehicleNumber &&
                x.StartDate >= dayStart &&
                x.StartDate < dayEnd &&
                x.EndDate == null);

            AddOrderByDescending(x => x.StartDate);
        }
    }
}