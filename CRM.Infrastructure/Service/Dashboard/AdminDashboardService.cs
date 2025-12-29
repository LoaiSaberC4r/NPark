using CRM.Application.Abstraction.Dashboard;
using CRM.Application.Shared.Dto;
using CRM.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Service.Dashboard
{
    internal class AdminDashboardService : IAdminDashboard
    {
        private CRMDBContext _context;

        public AdminDashboardService(CRMDBContext context)
        {
            _context = context;
        }

        public async Task<AdminDashboardDto> AdminDashboard(CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;
            var end30 = now.AddDays(30);

            var totalClients = await _context.Set<Account>().CountAsync(ct);

            var ticketAgg = await _context.Set<Ticket>()
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    Pending = g.Count(t => t.Status == Domain.Enums.TicketStatus.Pending),
                    Assigned = g.Count(t => t.Status == Domain.Enums.TicketStatus.Assigned),
                    Closed = g.Count(t => t.Status == Domain.Enums.TicketStatus.Closed),
                })
                .FirstOrDefaultAsync(ct)
                ?? new { Pending = 0, Assigned = 0, Closed = 0 };

            var productAgg = await _context.Set<SiteProduct>()
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    EndingWithin30Days = g.Count(sp => sp.SupportEndDate >= now && sp.SupportEndDate <= end30),
                    Ended = g.Count(sp => sp.SupportEndDate < now)
                })
                .FirstOrDefaultAsync(ct)
                ?? new { EndingWithin30Days = 0, Ended = 0 };

            return new AdminDashboardDto
            {
                TotalClients = totalClients,
                TotalPendingTickets = ticketAgg.Pending,
                TotalAssignedTickets = ticketAgg.Assigned,
                TotalClosedTickets = ticketAgg.Closed,
                TotalProductsEndedWithing30Days = productAgg.EndingWithin30Days,
                TtoalProductsEnded = productAgg.Ended
            };
        }
    }
}