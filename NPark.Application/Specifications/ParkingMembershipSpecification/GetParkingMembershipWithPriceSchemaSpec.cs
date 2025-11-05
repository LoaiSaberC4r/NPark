using BuildingBlock.Domain.Specification;
using NPark.Application.Feature.ParkingMembershipsManagement.Query.GetAll;
using NPark.Domain.Entities;
using NPark.Domain.FileNames;

namespace NPark.Application.Specifications.ParkingMembershipSpecification
{
    public sealed class GetParkingMembershipWithPriceSchemaSpec : Specification<ParkingMemberships, GetAllParkingMembershipQueryResponse>
    {
        public GetParkingMembershipWithPriceSchemaSpec(GetAllParkingMembershipQuery request)
        {
            Include(x => x.PricingScheme);
            Include(x => x.Attachments);
            if (request.SearchText != null)
            {
                AddCriteria(x => x.Name.Contains(request.SearchText) ||
                x.VehicleNumber.Contains(request.SearchText) ||
                x.Phone.Contains(request.SearchText) ||
                x.NationalId.Contains(request.SearchText)

                );
            }
            ApplyPaging(request.PageNumber, request.PageSize);
            if (request.OrderSort == BuildingBlock.Domain.Enums.OrderSort.Newest)
            {
                AddOrderByDescending(x => x.CreatedAt);
            }
            else
            {
                AddOrderBy(x => x.CreatedAt);
            }
            Select(x => new GetAllParkingMembershipQueryResponse
            {
                Id = x.Id,
                Name = x.Name,
                VehicleNumber = x.VehicleNumber,
                Phone = x.Phone,
                NationalId = x.NationalId,
                CreatedAt = x.CreatedAt,
                StartTime = x.PricingScheme.StartTime,
                EndTime = x.PricingScheme.EndTime,
                EndDate = x.EndDate,
                CardNumber = x.CardNumber,
                VehicleImage = x.Attachments.Select(x => new GetAllParkingAttachment
                {
                    FilePath = $"Media/{FileNames.ParkingMemberships}/{x.FilePath}",
                    Id = x.Id,
                }).ToList(),
            });
        }
    }
}