using BuildingBlock.Domain.EntitiesHelper;
using NPark.Domain.Enums;

namespace NPark.Domain.Entities
{
    public class PricingScheme : Entity<Guid>
    {
        private readonly List<ParkingMemberships> _parkingMemberships = new();

        public string Name { get; private set; } = string.Empty;
        public DurationType DurationType { get; private set; }
        public TimeSpan? StartTime { get; private set; }
        public TimeSpan? EndTime { get; private set; }
        public bool IsRepeated { get; private set; }
        public decimal Salary { get; private set; }
        public int? TotalDays { get; private set; }
        public int? TotalHours { get; private set; }

        public bool IsEnterPriceSchema { get; private set; }

        public IReadOnlyCollection<ParkingMemberships> ParkingMemberships => _parkingMemberships;

        private PricingScheme()
        {
        }

        public static PricingScheme Create(
            string name,
            DurationType durationType,
            TimeSpan? startTime,
            TimeSpan? endTime,
            bool isRepeated,
            decimal salary,
            int? totalDays,
            int? totalHours)
        {
            return new PricingScheme
            {
                Name = name.Trim(),
                DurationType = durationType,
                StartTime = startTime,
                EndTime = endTime,
                IsRepeated = isRepeated,
                Salary = salary,
                TotalDays = totalDays,
                TotalHours = totalHours,
                IsEnterPriceSchema = false
            };
        }

        public static PricingScheme CreateEnterPriceSchema(
            string name,
            decimal salary)
        {
            return new PricingScheme
            {
                Name = name.Trim(),
                DurationType = DurationType.Days,
                StartTime = null,
                EndTime = null,
                IsRepeated = false,
                Salary = salary,
                TotalDays = 1,
                TotalHours = null,
                IsEnterPriceSchema = true
            };
        }

        public void Update(
            string name,
            DurationType durationType,
            TimeSpan? startTime,
            TimeSpan? endTime,
            bool isRepeated,
            decimal salary,
            int? totalDays,
            int? totalHours)
        {
            Name = name.Trim();
            DurationType = durationType;
            StartTime = startTime;
            EndTime = endTime;
            IsRepeated = isRepeated;
            Salary = salary;
            TotalDays = totalDays;
            TotalHours = totalHours;
        }

        public void UpdateEnterPriceSchema(
            string name,
            decimal salary)
        {
            Name = name.Trim();
            DurationType = DurationType.Days;
            StartTime = null;
            EndTime = null;
            IsRepeated = false;
            Salary = salary;
            TotalDays = 1;
            TotalHours = null;
            IsEnterPriceSchema = true;
        }

        public void UpdateName(string name) => Name = name.Trim();

        public void UpdateDurationType(DurationType durationType) => DurationType = durationType;

        public void UpdateStartTime(TimeSpan startTime) => StartTime = startTime;

        public void UpdateEndTime(TimeSpan endTime) => EndTime = endTime;

        public void UpdateIsRepeated(bool isRepeated) => IsRepeated = isRepeated;

        public void UpdateSalary(decimal salary) => Salary = salary;
    }
}