using Microsoft.AspNetCore.Mvc;
using NPark.Infrastructure.Services;

namespace NPark.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet]
        public async Task<IActionResult> Get(
            [FromQuery] DateOnly? from,
            [FromQuery] DateOnly? to,
            CancellationToken cancellationToken)
        {
            var today = DateOnly.FromDateTime(DateTime.Now);

            var fromDate = from ?? today;
            var toDate = to ?? fromDate;

            if (toDate < fromDate)
            {
                return BadRequest(new
                {
                    Message = "To date must be greater than or equal to From date.",
                    From = fromDate,
                    To = toDate
                });
            }

            var result = await _dashboardService.GetDashboardAsync(
                fromDate,
                toDate,
                cancellationToken);

            return Ok(result);
        }
    }
}