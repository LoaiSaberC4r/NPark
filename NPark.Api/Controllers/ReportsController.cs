using BuildingBlock.Api;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NPark.Application.Feature.Reports.Query.GetEntranceCashierTicketsReport;
using NPark.Application.Feature.Reports.Query.GetSupervisorCollectionReport;
using NPark.Application.Feature.Reports.Query.GetSupervisorCollectionsReport;

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

        [HttpGet("supervisors/{supervisorId:guid}/collections")]
        public async Task<IActionResult> GetSupervisorCollectionsReport(
           [FromRoute] Guid supervisorId,
           [FromQuery] DateTime from,
           [FromQuery] DateTime to,
           CancellationToken cancellationToken)
        {
            var query = new GetSupervisorCollectionsReportQuery
            {
                SupervisorId = supervisorId,
                From = from,
                To = to
            };

            var result = await _sender.Send(query, cancellationToken);

            return result.ToIActionResult(cancellationToken);
        }

        [HttpGet("supervisors/{supervisorId:guid}/collection-report")]
        public async Task<IActionResult> GetSupervisorCollectionReport(
          [FromRoute] Guid supervisorId,
          [FromQuery] DateTime fromDate,
          [FromQuery] DateTime toDate,
          CancellationToken cancellationToken)
        {
            var query = new GetSupervisorCollectionReportQuery
            {
                SupervisorId = supervisorId,
                FromDate = fromDate,
                ToDate = toDate
            };

            var result = await _sender.Send(query, cancellationToken);

            return result.ToIActionResult(cancellationToken);
        }
    }
}