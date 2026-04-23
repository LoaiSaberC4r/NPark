using BuildingBlock.Domain.Specification;
using NPark.Application.Feature.ParkingSystemConfigurationManagement.Command.Update;
using NPark.Application.Feature.ParkingSystemConfigurationManagement.Query.GetSystemConfiguration;
using NPark.Domain.Entities;
using NPark.Domain.Enums;

namespace NPark.Application.Specifications.ParkingSystemConfigurationSpec
{
    public sealed class GetParkingSystemConfigurationSpecification
       : Specification<ParkingSystemConfiguration, GetSystemConfigurationQueryResponse>
    {
        public GetParkingSystemConfigurationSpecification()
        {
            AddCriteria(x => x.Id == 1);

            Include(x => x.PricingScheme);
            Include(x => x.ParkingGates);

            Select(x => new GetSystemConfigurationQueryResponse
            {
                AllowedParkingSlots = x.AllowedParkingSlots,
                EntryGatesCount = x.EntryGatesCount,
                ExitGatesCount = x.ExitGatesCount,
                PriceType = x.PriceType,
                VehiclePassengerData = x.VehiclePassengerData,
                PrintType = x.PrintType,
                DateTimeFlag = x.DateTimeFlag,
                TicketIdFlag = x.TicketIdFlag,
                FeesFlag = x.FeesFlag,
                gracePeriodMinutes = x.GracePeriod == null
                    ? null
                    : (int)x.GracePeriod.Value.TotalMinutes,
                PricingSchemaId = x.PricingSchemaId,
                PricingSchemaName = x.PricingScheme == null
                    ? string.Empty
                    : x.PricingScheme.Name,

                ExitGates = x.ParkingGates
                    .Where(g => g.GateType == GateType.Exit)
                    .Select(g => new GateInfo
                    {
                        GateId = g.Id,
                        GateNumber = g.GateNumber,
                        LprIp = g.LprIp,
                        PcIp = g.PcIp,
                        ControlIp = g.ControlIp ?? string.Empty,
                        AssignedTo = g.AssignedTo,
                        IsAssigned = g.IsAssigned,
                        ONVIFCameraSetting = g.ONVIFCameraSetting == null
                            ? null
                            : new ONVIFCameraSettingDto
                            {
                                UrlRequest = g.ONVIFCameraSetting.UrlRequest,
                                UserName = g.ONVIFCameraSetting.UserName,
                                Password = g.ONVIFCameraSetting.Password
                            }
                    })
                    .ToList(),

                EntryGates = x.ParkingGates
                    .Where(g => g.GateType == GateType.Entrance)
                    .Select(g => new GateInfo
                    {
                        GateId = g.Id,
                        GateNumber = g.GateNumber,
                        LprIp = g.LprIp,
                        PcIp = g.PcIp,
                        ControlIp = g.ControlIp ?? string.Empty,
                        AssignedTo = g.AssignedTo,
                        IsAssigned = g.IsAssigned,
                        ONVIFCameraSetting = g.ONVIFCameraSetting == null
                            ? null
                            : new ONVIFCameraSettingDto
                            {
                                UrlRequest = g.ONVIFCameraSetting.UrlRequest,
                                UserName = g.ONVIFCameraSetting.UserName,
                                Password = g.ONVIFCameraSetting.Password
                            }
                    })
                    .ToList()
            });
        }
    }
}