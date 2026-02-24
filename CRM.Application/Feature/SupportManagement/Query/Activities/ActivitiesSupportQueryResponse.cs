namespace CRM.Application.Feature.SupportManagement.Query.Activities
{
    public sealed record ActivitiesSupportQueryResponse
    {
        public int AssignedCount { get; init; }
        public int ClosedCount { get; init; }
        public int RejectedCount { get; init; }
    }
}