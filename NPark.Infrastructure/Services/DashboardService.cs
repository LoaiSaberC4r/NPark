using Microsoft.EntityFrameworkCore;
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
            var ticketExceedTransactionsQuery = _context.Set<TicketExceedTransaction>().AsNoTracking();

            var liveEnteredTodayQuery = liveTicketsQuery
                .Where(t => t.StartDate >= start && t.StartDate < end);

            var archivedEnteredTodayQuery = archivedTicketsQuery
                .Where(t => t.StartDate >= start && t.StartDate < end);

            var archivedExitedTodayQuery = archivedTicketsQuery
                .Where(t => t.EndDate >= start && t.EndDate < end);

            var ticketEnterNotTodayButNotExitYetQuery = liveTicketsQuery
                .Where(t => t.StartDate < start);

            var exceedTransactionsTodayQuery = ticketExceedTransactionsQuery
                .Where(t => t.CreatedAt >= start && t.CreatedAt < end);

            // =========================================================
            // 2) TICKETS COUNTS
            // =========================================================
            var liveEnteredTodayCount = await liveEnteredTodayQuery
                .CountAsync(cancellationToken);

            var archivedEnteredTodayCount = await archivedEnteredTodayQuery
                .CountAsync(cancellationToken);

            var ticketEnterToday = liveEnteredTodayCount + archivedEnteredTodayCount;

            var ticketEnterNotTodayButNotExitYet = await ticketEnterNotTodayButNotExitYetQuery
                .CountAsync(cancellationToken);

            var exitTicketsCount = await archivedExitedTodayQuery
                .CountAsync(cancellationToken);

            // =========================================================
            // 3) CURRENT VEHICLES INSIDE + OCCUPANCY
            // =========================================================
            var currentVehiclesInside = await liveTicketsQuery
                .CountAsync(cancellationToken);

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
            // 4) TICKET BASE REVENUE DATASET
            // =========================================================
            var revenueLiveRows = await liveEnteredTodayQuery
                .Select(t => new RevenueRow
                {
                    IsCollected = t.IsCollected,
                    IsSubscriber = t.IsSubscriber,
                    TicketPrice = t.Price,
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
                    TicketPrice = t.Price,
                    CollectedBy = t.CollectedBy,
                    CollectorName = t.UserCollector != null ? t.UserCollector.Name : null,
                    CollectedDate = t.CollectedDate
                })
                .ToListAsync(cancellationToken);

            var revenueRows = revenueLiveRows
                .Concat(revenueArchiveRows)
                .ToList();

            // =========================================================
            // 5) EXCEED TRANSACTIONS DATASET
            // =========================================================
            var exceedRows = await exceedTransactionsTodayQuery
                .Select(t => new ExceedTransactionRow
                {
                    Id = t.Id,
                    GateId = t.GateId,
                    ExceedAmount = t.ExceedAmount,
                    IsSubscriber = t.IsSubscriber,
                    IsSupervisorCollected = t.SuperVisorCollectorId.HasValue,
                    SupervisorCollectorId = t.SuperVisorCollectorId,
                    SupervisorCollectorName = t.SuperVisorCollector != null
                        ? t.SuperVisorCollector.Name
                        : null,
                    CreatedAt = t.CreatedAt,
                    SupervisorCollectedAt = t.SuperVisorCollectedAt
                })
                .ToListAsync(cancellationToken);

            // =========================================================
            // 6) REVENUE CALCULATION
            // =========================================================
            var ticketBaseRevenue = revenueRows
                .Sum(x => x.TicketPrice);

            var collectedTicketBaseRevenue = revenueRows
                .Where(x => x.IsCollected)
                .Sum(x => x.TicketPrice);

            var pendingTicketBaseRevenue = revenueRows
                .Where(x => !x.IsCollected)
                .Sum(x => x.TicketPrice);

            var totalExceedRevenue = exceedRows
                .Sum(x => x.ExceedAmount);

            var collectedExceedRevenue = exceedRows
                .Where(x => x.IsSupervisorCollected)
                .Sum(x => x.ExceedAmount);

            var pendingExceedRevenue = exceedRows
                .Where(x => !x.IsSupervisorCollected)
                .Sum(x => x.ExceedAmount);

            var totalRevenue = ticketBaseRevenue + totalExceedRevenue;

            var collectedRevenue = collectedTicketBaseRevenue + collectedExceedRevenue;

            var pendingRevenue = pendingTicketBaseRevenue + pendingExceedRevenue;

            // =========================================================
            // 7) MEMBERSHIP / WALK-IN
            // =========================================================
            var membershipTicketsCount = revenueRows
                .Count(x => x.IsSubscriber);

            var walkInTicketsCount = revenueRows
                .Count(x => !x.IsSubscriber);

            var membershipTicketBaseRevenue = revenueRows
                .Where(x => x.IsSubscriber)
                .Sum(x => x.TicketPrice);

            var walkInTicketBaseRevenue = revenueRows
                .Where(x => !x.IsSubscriber)
                .Sum(x => x.TicketPrice);

            var membershipExceedRevenue = exceedRows
                .Where(x => x.IsSubscriber)
                .Sum(x => x.ExceedAmount);

            var walkInExceedRevenue = exceedRows
                .Where(x => !x.IsSubscriber)
                .Sum(x => x.ExceedAmount);

            var membershipRelatedRevenue = membershipTicketBaseRevenue + membershipExceedRevenue;

            var walkInRevenue = walkInTicketBaseRevenue + walkInExceedRevenue;

            // =========================================================
            // 8) SUBSCRIPTIONS SNAPSHOT
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
                .CountAsync(
                    m => m.EndDate >= now && m.EndDate <= now.AddDays(7),
                    cancellationToken);

            // =========================================================
            // 9) PARKING DURATION
            // =========================================================
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
            // 10) EXCEED METRICS
            // =========================================================
            var exceedTransactionsCount = exceedRows.Count;

            var collectedExceedTransactionsCount = exceedRows
                .Count(x => x.IsSupervisorCollected);

            var pendingExceedTransactionsCount = exceedRows
                .Count(x => !x.IsSupervisorCollected);

            var exceededTicketsCount = exceedTransactionsCount;

            var averageExceedPerTransaction = exceedTransactionsCount > 0
                ? totalExceedRevenue / exceedTransactionsCount
                : 0m;

            var averageExceedPerTicket = exceededTicketsCount > 0
                ? totalExceedRevenue / exceededTicketsCount
                : 0m;

            // =========================================================
            // 11) ENTRY GATES STATS
            // =========================================================
            var entryGateLiveRows = await liveEnteredTodayQuery
                .Select(t => new EntryGateRow
                {
                    GateId = t.GateId,
                    Price = t.Price,
                    IsCollected = t.IsCollected,
                    CollectedBy = t.CollectedBy,
                    CollectorName = t.UserCollector != null ? t.UserCollector.Name : null
                })
                .ToListAsync(cancellationToken);

            var entryGateArchiveRows = await archivedEnteredTodayQuery
                .Select(t => new EntryGateRow
                {
                    GateId = t.GateId,
                    Price = t.Price,
                    IsCollected = t.IsCollected,
                    CollectedBy = t.CollectedBy,
                    CollectorName = t.UserCollector != null ? t.UserCollector.Name : null
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

                    var collectors = g
                        .Where(x =>
                            x.IsCollected &&
                            x.CollectedBy.HasValue &&
                            !string.IsNullOrWhiteSpace(x.CollectorName))
                        .GroupBy(x => new
                        {
                            CollectorId = x.CollectedBy!.Value,
                            x.CollectorName
                        })
                        .Select(cg => new GateCollectorSummaryDto
                        {
                            CollectorId = cg.Key.CollectorId,
                            CollectorName = cg.Key.CollectorName!,
                            TicketsCount = cg.Count(),
                            CollectedAmount = cg.Sum(x => x.Price)
                        })
                        .OrderByDescending(x => x.CollectedAmount)
                        .ThenByDescending(x => x.TicketsCount)
                        .ToList();

                    return new EntryGateSummaryDto
                    {
                        GateId = g.Key,
                        GateNumber = gate?.GateNumber ?? 0,
                        TicketsCount = g.Count(),
                        TotalPrice = g.Sum(x => x.Price),
                        CollectedPrice = g
                            .Where(x => x.IsCollected)
                            .Sum(x => x.Price),
                        PendingPrice = g
                            .Where(x => !x.IsCollected)
                            .Sum(x => x.Price),
                        Collectors = collectors
                    };
                })
                .OrderBy(x => x.GateNumber)
                .ToList();

            // =========================================================
            // 12) EXIT GATES STATS
            // =========================================================
            var exitTrafficRows = await archivedExitedTodayQuery
                .Where(t => t.ExitGateId != Guid.Empty)
                .Select(t => new ExitTrafficRow
                {
                    GateId = t.ExitGateId
                })
                .ToListAsync(cancellationToken);

            var exitGateIds = exitTrafficRows
                .Select(x => x.GateId)
                .Concat(exceedRows.Select(x => x.GateId))
                .Distinct()
                .ToList();

            var exitGatesLookup = await _context.Set<ParkingGate>()
                .AsNoTracking()
                .Where(g => exitGateIds.Contains(g.Id))
                .ToDictionaryAsync(g => g.Id, cancellationToken);

            var exitGates = exitGateIds
                .Select(gateId =>
                {
                    exitGatesLookup.TryGetValue(gateId, out var gate);

                    var gateTrafficRows = exitTrafficRows
                        .Where(x => x.GateId == gateId)
                        .ToList();

                    var gateExceedRows = exceedRows
                        .Where(x => x.GateId == gateId)
                        .ToList();

                    var supervisors = gateExceedRows
                        .Where(x =>
                            x.IsSupervisorCollected &&
                            x.SupervisorCollectorId.HasValue &&
                            !string.IsNullOrWhiteSpace(x.SupervisorCollectorName))
                        .GroupBy(x => new
                        {
                            SupervisorId = x.SupervisorCollectorId!.Value,
                            x.SupervisorCollectorName
                        })
                        .Select(sg => new GateSupervisorSummaryDto
                        {
                            SupervisorId = sg.Key.SupervisorId,
                            SupervisorName = sg.Key.SupervisorCollectorName!,
                            TransactionsCount = sg.Count(),
                            CollectedAmount = sg.Sum(x => x.ExceedAmount)
                        })
                        .OrderByDescending(x => x.CollectedAmount)
                        .ThenByDescending(x => x.TransactionsCount)
                        .ToList();

                    return new ExitGateSummaryDto
                    {
                        GateId = gateId,
                        GateNumber = gate?.GateNumber ?? 0,

                        ExitedTicketsCount = gateTrafficRows.Count,

                        ExceedTransactionsCount = gateExceedRows.Count,
                        CollectedExceedTransactionsCount = gateExceedRows
                            .Count(x => x.IsSupervisorCollected),
                        PendingExceedTransactionsCount = gateExceedRows
                            .Count(x => !x.IsSupervisorCollected),

                        TotalExceedAmount = gateExceedRows
                            .Sum(x => x.ExceedAmount),
                        CollectedExceedAmount = gateExceedRows
                            .Where(x => x.IsSupervisorCollected)
                            .Sum(x => x.ExceedAmount),
                        PendingExceedAmount = gateExceedRows
                            .Where(x => !x.IsSupervisorCollected)
                            .Sum(x => x.ExceedAmount),

                        Supervisors = supervisors
                    };
                })
                .OrderBy(x => x.GateNumber)
                .ToList();

            // =========================================================
            // 13) BUSIEST GATE
            // =========================================================
            var busiestGate = entryGates
                .Select(x => new
                {
                    x.GateNumber,
                    TrafficCount = x.TicketsCount
                })
                .Concat(exitGates.Select(x => new
                {
                    x.GateNumber,
                    TrafficCount = x.ExitedTicketsCount
                }))
                .OrderByDescending(x => x.TrafficCount)
                .ThenBy(x => x.GateNumber)
                .FirstOrDefault();

            var busiestGateNumber = busiestGate?.GateNumber ?? 0;
            var busiestGateTicketsCount = busiestGate?.TrafficCount ?? 0;

            // =========================================================
            // 14) TOP TICKET COLLECTOR
            // =========================================================
            var topCollector = revenueRows
                .Where(x =>
                    x.IsCollected &&
                    x.CollectedBy.HasValue &&
                    !string.IsNullOrWhiteSpace(x.CollectorName))
                .GroupBy(x => new
                {
                    x.CollectedBy,
                    x.CollectorName
                })
                .Select(g => new
                {
                    CollectorName = g.Key.CollectorName!,
                    TicketsCount = g.Count(),
                    TotalAmount = g.Sum(x => x.TicketPrice)
                })
                .OrderByDescending(x => x.TotalAmount)
                .ThenByDescending(x => x.TicketsCount)
                .FirstOrDefault();

            var topCollectorName = topCollector?.CollectorName;
            var topCollectorAmount = topCollector?.TotalAmount ?? 0m;
            var topCollectorTicketsCount = topCollector?.TicketsCount ?? 0;

            // =========================================================
            // 15) TOP EXCEED COLLECTOR
            // =========================================================
            var topExceedCollector = exceedRows
                .Where(x =>
                    x.IsSupervisorCollected &&
                    x.SupervisorCollectorId.HasValue &&
                    !string.IsNullOrWhiteSpace(x.SupervisorCollectorName))
                .GroupBy(x => new
                {
                    x.SupervisorCollectorId,
                    x.SupervisorCollectorName
                })
                .Select(g => new
                {
                    CollectorName = g.Key.SupervisorCollectorName!,
                    TransactionsCount = g.Count(),
                    TotalAmount = g.Sum(x => x.ExceedAmount)
                })
                .OrderByDescending(x => x.TotalAmount)
                .ThenByDescending(x => x.TransactionsCount)
                .FirstOrDefault();

            var topExceedCollectorName = topExceedCollector?.CollectorName;
            var topExceedCollectorAmount = topExceedCollector?.TotalAmount ?? 0m;
            var topExceedCollectorTransactionsCount = topExceedCollector?.TransactionsCount ?? 0;

            // =========================================================
            // 16) BUILD DTO
            // =========================================================
            return new DashboardDto
            {
                Date = targetDate,

                // Subscriptions
                SubscriptionCount = subscriptionCount,
                ActiveSubscriptionCount = activeSubscriptionCount,
                InactiveSubscriptionCount = inactiveSubscriptionCount,
                MembershipsExpiringNext7Days = membershipsExpiringNext7Days,

                // Tickets
                TicketEnterToday = ticketEnterToday,
                TicketEnterNotTodayButNotExitYet = ticketEnterNotTodayButNotExitYet,
                ExitTicketsCount = exitTicketsCount,

                CurrentVehiclesInside = currentVehiclesInside,
                ActiveTicketsCount = activeTicketsCount,
                OccupancyPercentage = occupancyPercentage,
                EstimatedFreeSlots = estimatedFreeSlots,

                // Revenue
                TotalRevenue = totalRevenue,
                CollectedRevenue = collectedRevenue,
                PendingRevenue = pendingRevenue,

                TicketBaseRevenue = ticketBaseRevenue,
                CollectedTicketBaseRevenue = collectedTicketBaseRevenue,
                PendingTicketBaseRevenue = pendingTicketBaseRevenue,

                // Membership / Walk-in
                MembershipTicketsCount = membershipTicketsCount,
                WalkInTicketsCount = walkInTicketsCount,
                MembershipRelatedRevenue = membershipRelatedRevenue,
                WalkInRevenue = walkInRevenue,

                // Duration
                AverageDurationMinutes = averageDurationMinutes,
                MaxDurationMinutes = maxDurationMinutes,

                // Exceed
                ExceededTicketsCount = exceededTicketsCount,
                ExceedTransactionsCount = exceedTransactionsCount,
                CollectedExceedTransactionsCount = collectedExceedTransactionsCount,
                PendingExceedTransactionsCount = pendingExceedTransactionsCount,

                TotalExceedRevenue = totalExceedRevenue,
                CollectedExceedRevenue = collectedExceedRevenue,
                PendingExceedRevenue = pendingExceedRevenue,

                AverageExceedPerTicket = averageExceedPerTicket,
                AverageExceedPerTransaction = averageExceedPerTransaction,

                // Gates
                BusiestGateNumber = busiestGateNumber,
                BusiestGateTicketsCount = busiestGateTicketsCount,

                EntryGates = entryGates,
                ExitGates = exitGates,

                // Collectors
                TopCollectorName = topCollectorName,
                TopCollectorAmount = topCollectorAmount,
                TopCollectorTicketsCount = topCollectorTicketsCount,

                TopExceedCollectorName = topExceedCollectorName,
                TopExceedCollectorAmount = topExceedCollectorAmount,
                TopExceedCollectorTransactionsCount = topExceedCollectorTransactionsCount
            };
        }

        private sealed class RevenueRow
        {
            public bool IsCollected { get; init; }

            public bool IsSubscriber { get; init; }

            public decimal TicketPrice { get; init; }

            public Guid? CollectedBy { get; init; }

            public string? CollectorName { get; init; }

            public DateTime? CollectedDate { get; init; }
        }

        private sealed class EntryGateRow
        {
            public Guid GateId { get; init; }

            public decimal Price { get; init; }

            public bool IsCollected { get; init; }

            public Guid? CollectedBy { get; init; }

            public string? CollectorName { get; init; }
        }

        private sealed class ExitTrafficRow
        {
            public Guid GateId { get; init; }
        }

        private sealed class ExceedTransactionRow
        {
            public Guid Id { get; init; }

            public Guid GateId { get; init; }

            public decimal ExceedAmount { get; init; }

            public bool IsSubscriber { get; init; }

            public bool IsSupervisorCollected { get; init; }

            public Guid? SupervisorCollectorId { get; init; }

            public string? SupervisorCollectorName { get; init; }

            public DateTime CreatedAt { get; init; }

            public DateTime? SupervisorCollectedAt { get; init; }
        }
    }
}

public sealed class DashboardDto
{
    public DateOnly Date { get; init; }

    // -----------------------------
    // Subscriptions
    // -----------------------------
    public int SubscriptionCount { get; init; }

    public int ActiveSubscriptionCount { get; init; }
    public int InactiveSubscriptionCount { get; init; }
    public int MembershipsExpiringNext7Days { get; init; }

    // -----------------------------
    // Tickets
    // -----------------------------
    public int TicketEnterToday { get; init; }

    public int TicketEnterNotTodayButNotExitYet { get; init; }

    public int ExitTicketsCount { get; init; }

    public int CurrentVehiclesInside { get; init; }

    public int ActiveTicketsCount { get; init; }

    public decimal OccupancyPercentage { get; init; }

    public int EstimatedFreeSlots { get; init; }

    // -----------------------------
    // Revenue
    // -----------------------------
    public decimal TotalRevenue { get; init; }

    public decimal CollectedRevenue { get; init; }

    public decimal PendingRevenue { get; init; }

    public decimal TicketBaseRevenue { get; init; }

    public decimal CollectedTicketBaseRevenue { get; init; }

    public decimal PendingTicketBaseRevenue { get; init; }

    // -----------------------------
    // Membership / Walk-in
    // -----------------------------
    public int MembershipTicketsCount { get; init; }

    public int WalkInTicketsCount { get; init; }

    public decimal MembershipRelatedRevenue { get; init; }

    public decimal WalkInRevenue { get; init; }

    // -----------------------------
    // Duration
    // -----------------------------
    public int AverageDurationMinutes { get; init; }

    public int MaxDurationMinutes { get; init; }

    // -----------------------------
    // Exceed
    // -----------------------------
    public int ExceededTicketsCount { get; init; }

    public int ExceedTransactionsCount { get; init; }

    public int CollectedExceedTransactionsCount { get; init; }

    public int PendingExceedTransactionsCount { get; init; }

    public decimal TotalExceedRevenue { get; init; }

    public decimal CollectedExceedRevenue { get; init; }

    public decimal PendingExceedRevenue { get; init; }

    public decimal AverageExceedPerTicket { get; init; }

    public decimal AverageExceedPerTransaction { get; init; }

    // -----------------------------
    // Gates
    // -----------------------------
    public int BusiestGateNumber { get; init; }

    public int BusiestGateTicketsCount { get; init; }

    public List<EntryGateSummaryDto> EntryGates { get; init; } = new();

    public List<ExitGateSummaryDto> ExitGates { get; init; } = new();

    // -----------------------------
    // Collectors
    // -----------------------------
    public string? TopCollectorName { get; init; }

    public decimal TopCollectorAmount { get; init; }

    public int TopCollectorTicketsCount { get; init; }

    public string? TopExceedCollectorName { get; init; }

    public decimal TopExceedCollectorAmount { get; init; }

    public int TopExceedCollectorTransactionsCount { get; init; }
}

public sealed class EntryGateSummaryDto
{
    public Guid GateId { get; init; }

    public int GateNumber { get; init; }

    public int TicketsCount { get; init; }

    public decimal TotalPrice { get; init; }

    public decimal CollectedPrice { get; init; }

    public decimal PendingPrice { get; init; }

    public List<GateCollectorSummaryDto> Collectors { get; init; } = new();
}

public sealed class GateCollectorSummaryDto
{
    public Guid CollectorId { get; init; }

    public string CollectorName { get; init; } = string.Empty;

    public int TicketsCount { get; init; }

    public decimal CollectedAmount { get; init; }
}

public sealed class ExitGateSummaryDto
{
    public Guid GateId { get; init; }

    public int GateNumber { get; init; }

    // From TicketArchive
    public int ExitedTicketsCount { get; init; }

    // From TicketExceedTransaction
    public int ExceedTransactionsCount { get; init; }

    public int CollectedExceedTransactionsCount { get; init; }

    public int PendingExceedTransactionsCount { get; init; }

    public decimal TotalExceedAmount { get; init; }

    public decimal CollectedExceedAmount { get; init; }

    public decimal PendingExceedAmount { get; init; }

    public List<GateSupervisorSummaryDto> Supervisors { get; init; } = new();
}

public sealed class GateSupervisorSummaryDto
{
    public Guid SupervisorId { get; init; }

    public string SupervisorName { get; init; } = string.Empty;

    public int TransactionsCount { get; init; }

    public decimal CollectedAmount { get; init; }
}