using BuildingBlock.Api;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NPark.Application.Feature.Reports.Query.GetEntranceCashierTicketsReport;

namespace NPark.Api.Controllers
{
    [ApiController]
    [Route("api/reports")]
    [Authorize]
    public sealed class ReportsController : ControllerBase
    {
        private readonly ISender _sender;

        public ReportsController(ISender sender)
        {
            _sender = sender ?? throw new ArgumentNullException(nameof(sender));
        }

        [HttpGet("entrance-cashiers/{userId:guid}/tickets")]
        public async Task<IActionResult> GetEntranceCashierTicketsReport(
            [FromRoute] Guid userId,
            [FromQuery] DateTime from,
            [FromQuery] DateTime to,
            CancellationToken cancellationToken)
        {
            var query = new GetEntranceCashierTicketsReportQuery
            {
                UserId = userId,
                From = from,
                To = to
            };

            var result = await _sender.Send(query, cancellationToken);

            return result.ToIActionResult(cancellationToken);
        }
    }
}