namespace NPark.Application.Feature.TicketsManagement.Query.ValidateExitByVehicleNumber
{
    public sealed record ValidateExitByVehicleNumberQueryResponse
    {
        public bool IsValidForExit { get; init; }
    }
}