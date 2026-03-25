using Microsoft.EntityFrameworkCore;
using NPark.Application.Shared.Dto;
using NPark.Domain.Entities;

namespace NPark.Infrastructure.Services
{
    public interface IDashboardService
    {
        Task<DashboardDto> GetDashboardAsync(
            DateTime? date = null,
            CancellationToken cancellationToken = default);
    }

    public sealed class DashboardService : IDashboardService
    {
        private readonly NParkDBContext _context;

        public DashboardService(NParkDBContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<DashboardDto> GetDashboardAsync(
            DateTime? date = null,
            CancellationToken cancellationToken = default)
        {
            var targetDate = date.HasValue
                ? DateOnly.FromDateTime(date.Value)
                : DateOnly.FromDateTime(DateTime.Now);

            var start = targetDate.ToDateTime(TimeOnly.MinValue);
            var end = start.AddDays(1);

            // =========================================================
            // 1) BASE QUERIES
            // =========================================================
            var liveTicketsQuery = _context.Set<Ticket>().AsNoTracking();
            var archivedTicketsQuery = _context.Set<TicketArchive>().AsNoTracking();

            // دخلت في اليوم
            var archivedEnteredTodayQuery = archivedTicketsQuery
                .Where(t => t.StartDate >= start && t.StartDate < end);

            // خرجت في اليوم
            var archivedExitedTodayQuery = archivedTicketsQuery
                .Where(t => t.EndDate >= start && t.EndDate < end);

            // Dataset الدخول الجديد حسب الـ business:
            // - كل التذاكر الموجودة حاليا في Ticket table
            // - + TicketArchive اللي StartDate بتاعها في اليوم
            var entryLiveTicketsCount = await liveTicketsQuery.CountAsync(cancellationToken);
            var entryArchivedTicketsCount = await archivedEnteredTodayQuery.CountAsync(cancellationToken);
            var enterTicketsCount = entryLiveTicketsCount + entryArchivedTicketsCount;

            // Dataset الخروج الجديد حسب الـ business:
            // - TicketArchive اللي EndDate بتاعها في اليوم
            var exitTicketsCount = await archivedExitedTodayQuery.CountAsync(cancellationToken);

            // =========================================================
            // 2) CURRENT VEHICLES INSIDE + OCCUPANCY
            // =========================================================
            // حسب البيزنس الجديد:
            // "All ticket is still in ticket Table only"
            var currentVehiclesInside = entryLiveTicketsCount;
            var activeTicketsCount = currentVehiclesInside;

            var parkingConfig = await _context.Set<ParkingSystemConfiguration>()
                .AsNoTracking()
                .OrderByDescending(c => c.Id)
                .FirstOrDefaultAsync(cancellationToken);

            var totalParkingSlots = parkingConfig?.AllowedParkingSlots ?? 0;

            decimal occupancyPercentage = 0m;
            int estimatedFreeSlots = 0;

            if (totalParkingSlots > 0)
            {
                occupancyPercentage = (decimal)currentVehiclesInside / totalParkingSlots * 100m;
                estimatedFreeSlots = Math.Max(0, totalParkingSlots - currentVehiclesInside);
            }

            // =========================================================
            // 3) DAILY REVENUE DATASET
            // =========================================================
            // حسب البيزنس:
            // الإيراد = كل التذاكر من Ticket + TicketArchive
            // على أساس اليوم اللي دخلت فيه (StartDate)
            var liveEnteredTodayQuery = liveTicketsQuery
                .Where(t => t.StartDate >= start && t.StartDate < end);

            var revenueLiveRows = await liveEnteredTodayQuery
                .Select(t => new RevenueRow
                {
                    IsCollected = t.IsCollected,
                    IsSubscriber = t.IsSubscriber,
                    TotalPrice = t.TotalPrice,
                    ExceedPrice = t.ExceedPrice,
                    CollectedBy = t.CollectedBy,
                    CollectorName = t.UserCollector != null ? t.UserCollector.Name : null,
                    CollectedDate = t.CollectedDate
                })
                .ToListAsync(cancellationToken);

            var revenueArchiveRows = await archivedEnteredTodayQuery
                .Select(t => new RevenueRow
                {
                    IsCollected = t.IsCollected,
                    IsSubscriber = t.IsSubscriber,
                    TotalPrice = t.TotalPrice,
                    ExceedPrice = t.ExceedPrice,
                    CollectedBy = t.CollectedBy,
                    CollectorName = t.UserCollector != null ? t.UserCollector.Name : null,
                    CollectedDate = t.CollectedDate
                })
                .ToListAsync(cancellationToken);

            var revenueRows = revenueLiveRows
                .Concat(revenueArchiveRows)
                .ToList();

            var totalRevenue = revenueRows.Sum(x => x.TotalPrice);
            var collectedRevenue = revenueRows
                .Where(x => x.IsCollected)
                .Sum(x => x.TotalPrice);
            var pendingRevenue = revenueRows
                .Where(x => !x.IsCollected)
                .Sum(x => x.TotalPrice);

            // =========================================================
            // 4) MEMBERSHIP / WALK-IN
            // =========================================================
            var membershipTicketsCount = revenueRows.Count(x => x.IsSubscriber);
            var walkInTicketsCount = revenueRows.Count(x => !x.IsSubscriber);

            var membershipRelatedRevenue = revenueRows
                .Where(x => x.IsSubscriber)
                .Sum(x => x.TotalPrice);

            var walkInRevenue = revenueRows
                .Where(x => !x.IsSubscriber)
                .Sum(x => x.TotalPrice);

            // =========================================================
            // 5) SUBSCRIPTIONS SNAPSHOT
            // =========================================================
            var now = DateTime.Now;

            var subscriptionCount = await _context.Set<ParkingMemberships>()
                .AsNoTracking()
                .CountAsync(cancellationToken);

            var activeSubscriptionCount = await _context.Set<ParkingMemberships>()
                .AsNoTracking()
                .Where(m => m.EndDate >= now)
                .CountAsync(cancellationToken);

            var inactiveSubscriptionCount = await _context.Set<ParkingMemberships>()
                .AsNoTracking()
                .Where(m => m.EndDate < now)
                .CountAsync(cancellationToken);

            var membershipsExpiringNext7Days = await _context.Set<ParkingMemberships>()
                .AsNoTracking()
                .CountAsync(m => m.EndDate >= now && m.EndDate <= now.AddDays(7), cancellationToken);

            // =========================================================
            // 6) PARKING DURATION
            // =========================================================
            // حسب البيزنس:
            // Calculate from ticketArchive table
            // الأفضل نحسب من اللي خرجوا في اليوم
            var exitedDurations = await archivedExitedTodayQuery
                .Select(t => EF.Functions.DateDiffMinute(t.StartDate, t.EndDate))
                .ToListAsync(cancellationToken);

            int averageDurationMinutes = 0;
            int maxDurationMinutes = 0;

            if (exitedDurations.Count > 0)
            {
                averageDurationMinutes = (int)Math.Round(exitedDurations.Average());
                maxDurationMinutes = exitedDurations.Max();
            }

            // =========================================================
            // 7) EXCEED METRICS
            // =========================================================
            var exceededRows = revenueRows
                .Where(x => x.ExceedPrice > 0)
                .ToList();

            var exceededTicketsCount = exceededRows.Count;
            var totalExceedRevenue = exceededRows.Sum(x => x.ExceedPrice);
            var averageExceedPerTicket = exceededTicketsCount > 0
                ? totalExceedRevenue / exceededTicketsCount
                : 0m;

            // =========================================================
            // 8) ENTRY GATES STATS
            // =========================================================
            // حسب البيزنس:
            // بوابة الدخول = Ticket table + TicketArchive entered today
            var entryGateLiveRows = await liveTicketsQuery
                .Select(t => new GateRow
                {
                    GateId = t.GateId,
                    TotalPrice = t.TotalPrice,
                    IsCollected = t.IsCollected,
                    CollectorName = t.UserCollector != null ? t.UserCollector.Name : null,
                    CollectedDate = t.CollectedDate
                })
                .ToListAsync(cancellationToken);

            var entryGateArchiveRows = await archivedEnteredTodayQuery
                .Select(t => new GateRow
                {
                    GateId = t.GateId,
                    TotalPrice = t.TotalPrice,
                    IsCollected = t.IsCollected,
                    CollectorName = t.UserCollector != null ? t.UserCollector.Name : null,
                    CollectedDate = t.CollectedDate
                })
                .ToListAsync(cancellationToken);

            var entryGateRows = entryGateLiveRows
                .Concat(entryGateArchiveRows)
                .ToList();

            var entryGateIds = entryGateRows
                .Select(x => x.GateId)
                .Distinct()
                .ToList();

            var entryGatesLookup = await _context.Set<ParkingGate>()
                .AsNoTracking()
                .Where(g => entryGateIds.Contains(g.Id))
                .ToDictionaryAsync(g => g.Id, cancellationToken);

            var entryGates = entryGateRows
                .GroupBy(x => x.GateId)
                .Select(g =>
                {
                    entryGatesLookup.TryGetValue(g.Key, out var gate);

                    var latestCollectorName = g
                        .Where(x => x.IsCollected && !string.IsNullOrWhiteSpace(x.CollectorName))
                        .OrderByDescending(x => x.CollectedDate)
                        .Select(x => x.CollectorName)
                        .FirstOrDefault();

                    return new GateTicketsSummaryDto
                    {
                        GateId = g.Key,
                        GateNumber = gate?.GateNumber ?? 0,
                        TicketsCount = g.Count(),
                        TotalPrice = g.Sum(x => x.TotalPrice),
                        CollectedPrice = g.Where(x => x.IsCollected).Sum(x => x.TotalPrice),
                        SupervisorName = latestCollectorName
                    };
                })
                .OrderBy(x => x.GateNumber)
                .ToList();

            // =========================================================
            // 9) EXIT GATES STATS
            // =========================================================
            // حسب البيزنس:
            // بوابة الخروج = TicketArchive exited today only
            var exitGateRows = await archivedExitedTodayQuery
                .Select(t => new GateRow
                {
                    GateId = t.ExitGateId,
                    TotalPrice = t.TotalPrice,
                    IsCollected = t.IsCollected,
                    CollectorName = t.UserCollector != null ? t.UserCollector.Name : null,
                    CollectedDate = t.CollectedDate
                })
                .ToListAsync(cancellationToken);

            var exitGateIds = exitGateRows
                .Select(x => x.GateId)
                .Distinct()
                .ToList();

            var exitGatesLookup = await _context.Set<ParkingGate>()
                .AsNoTracking()
                .Where(g => exitGateIds.Contains(g.Id))
                .ToDictionaryAsync(g => g.Id, cancellationToken);

            var exitGates = exitGateRows
                .GroupBy(x => x.GateId)
                .Select(g =>
                {
                    exitGatesLookup.TryGetValue(g.Key, out var gate);

                    var latestCollectorName = g
                        .Where(x => x.IsCollected && !string.IsNullOrWhiteSpace(x.CollectorName))
                        .OrderByDescending(x => x.CollectedDate)
                        .Select(x => x.CollectorName)
                        .FirstOrDefault();

                    return new GateTicketsSummaryDto
                    {
                        GateId = g.Key,
                        GateNumber = gate?.GateNumber ?? 0,
                        TicketsCount = g.Count(),
                        TotalPrice = g.Sum(x => x.TotalPrice),
                        CollectedPrice = g.Where(x => x.IsCollected).Sum(x => x.TotalPrice),
                        SupervisorName = latestCollectorName
                    };
                })
                .OrderBy(x => x.GateNumber)
                .ToList();

            // =========================================================
            // 10) BUSIEST GATE
            // =========================================================
            var busiestGate = entryGates
                .Select(x => new { x.GateNumber, x.TicketsCount })
                .Concat(exitGates.Select(x => new { x.GateNumber, x.TicketsCount }))
                .OrderByDescending(x => x.TicketsCount)
                .ThenBy(x => x.GateNumber)
                .FirstOrDefault();

            var busiestGateNumber = busiestGate?.GateNumber ?? 0;
            var busiestGateTicketsCount = busiestGate?.TicketsCount ?? 0;

            // =========================================================
            // 11) TOP COLLECTOR
            // =========================================================
            // حسب البيزنس:
            // from Ticket + TicketArchive
            var topCollector = revenueRows
                .Where(x => x.IsCollected && x.CollectedBy.HasValue && !string.IsNullOrWhiteSpace(x.CollectorName))
                .GroupBy(x => new { x.CollectedBy, x.CollectorName })
                .Select(g => new
                {
                    CollectorName = g.Key.CollectorName!,
                    TicketsCount = g.Count(),
                    TotalAmount = g.Sum(x => x.TotalPrice)
                })
                .OrderByDescending(x => x.TotalAmount)
                .ThenByDescending(x => x.TicketsCount)
                .FirstOrDefault();

            var topCollectorName = topCollector?.CollectorName;
            var topCollectorAmount = topCollector?.TotalAmount ?? 0m;
            var topCollectorTicketsCount = topCollector?.TicketsCount ?? 0;

            // =========================================================
            // 12) BUILD DTO
            // =========================================================
            var dto = new DashboardDto
            {
                Date = targetDate,

                SubscriptionCount = subscriptionCount,
                ActiveSubscriptionCount = activeSubscriptionCount,
                InactiveSubscriptionCount = inactiveSubscriptionCount,

                EnterTicketsCount = enterTicketsCount,
                ExitTicketsCount = exitTicketsCount,

                TotalRevenue = totalRevenue,
                CollectedRevenue = collectedRevenue,
                PendingRevenue = pendingRevenue,

                CurrentVehiclesInside = currentVehiclesInside,
                ActiveTicketsCount = activeTicketsCount,
                OccupancyPercentage = occupancyPercentage,
                EstimatedFreeSlots = estimatedFreeSlots,

                MembershipTicketsCount = membershipTicketsCount,
                WalkInTicketsCount = walkInTicketsCount,
                MembershipRelatedRevenue = membershipRelatedRevenue,
                WalkInRevenue = walkInRevenue,
                MembershipsExpiringNext7Days = membershipsExpiringNext7Days,

                AverageDurationMinutes = averageDurationMinutes,
                MaxDurationMinutes = maxDurationMinutes,

                ExceededTicketsCount = exceededTicketsCount,
                TotalExceedRevenue = totalExceedRevenue,
                AverageExceedPerTicket = averageExceedPerTicket,

                BusiestGateNumber = busiestGateNumber,
                BusiestGateTicketsCount = busiestGateTicketsCount,

                TopCollectorName = topCollectorName,
                TopCollectorAmount = topCollectorAmount,
                TopCollectorTicketsCount = topCollectorTicketsCount,

                EntryGates = entryGates,
                ExitGates = exitGates
            };

            return dto;
        }

        private sealed class RevenueRow
        {
            public bool IsCollected { get; init; }
            public bool IsSubscriber { get; init; }
            public decimal TotalPrice { get; init; }
            public decimal ExceedPrice { get; init; }
            public Guid? CollectedBy { get; init; }
            public string? CollectorName { get; init; }
            public DateTime? CollectedDate { get; init; }
        }

        private sealed class GateRow
        {
            public Guid GateId { get; init; }
            public decimal TotalPrice { get; init; }
            public bool IsCollected { get; init; }
            public string? CollectorName { get; init; }
            public DateTime? CollectedDate { get; init; }
        }
    }
}