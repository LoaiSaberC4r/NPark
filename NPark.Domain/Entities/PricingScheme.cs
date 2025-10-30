using BuildingBlock.Domain.EntitiesHelper;
using NPark.Domain.Enums;

namespace NPark.Domain.Entities
{
    public class PricingScheme : Entity<Guid>
    {
        private List<ParkingMemberships> _parkingMemberships = new();

        public string Name { get; private set; } = string.Empty;
        public DurationType DurationType { get; private set; }
        public TimeSpan? StartTime { get; private set; }
        public TimeSpan? EndTime { get; private set; }
        public bool IsRepeated { get; private set; }
        public decimal Salary { get; private set; }
        public int? OrderPriority { get; private set; }
        public int? TotalDays { get; private set; }
        public int? TotalHours { get; private set; }
        public IReadOnlyCollection<ParkingMemberships> ParkingMemberships => _parkingMemberships;

        private PricingScheme()
        { }

        public static PricingScheme Create(string name, DurationType durationType, TimeSpan? startTime, TimeSpan? endTime, bool isRepeated, decimal salary, int? orderPriority, int? totalDays, int? totalHours) => new PricingScheme()
        {
            Name = name,
            DurationType = durationType,
            StartTime = startTime,
            EndTime = endTime,
            IsRepeated = isRepeated,
            Salary = salary,
            OrderPriority = orderPriority,
            TotalDays = totalDays,
            TotalHours = totalHours
        };

        public void UpdateName(string name) => Name = name;

        public void UpdateDurationType(DurationType durationType) => DurationType = durationType;

        public void UpdateStartTime(TimeSpan startTime) => StartTime = startTime;

        public void UpdateEndTime(TimeSpan endTime) => EndTime = endTime;

        public void UpdateIsRepeated(bool isRepeated) => IsRepeated = isRepeated;

        public void UpdateSalary(decimal salary) => Salary = salary;

        public void UpdateOrderPriority(int? orderPriority) => OrderPriority = orderPriority;
    }
}