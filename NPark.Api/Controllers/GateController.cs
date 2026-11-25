using BuildingBlock.Domain.Results;
using NPark.Application.Abstraction.Security;
using NPark.Application.Services;
using NPark.Domain.Enums;
using NPark.Infrastructure.Services.Mqtt;
using BuildingBlock.Api;
using BuildingBlock.Api.ControllerTemplate;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NPark.Api.Contracts;
using NPark.Application.Feature.GateManagement.Command.OpenGate;
using NPark.Application.Feature.GateManagement.Query.GetAll;
using NPark.Application.Feature.GateManagement.Query.GetGateConnectionInfo;

namespace NPark.Api.Controllers
{
    [Route("api/[controller]")]
    public class GateController : ControllerTemplate
    {
        public GateController(ISender sender) : base(sender)
        {
        }

        [HttpGet(nameof(GetAll))]
        public async Task<IActionResult> GetAll([FromQuery] GetAllGateQuery query, CancellationToken cancellationToken)
        {
            var result = await sender.Send(query, cancellationToken);
            return result.ToIActionResult();
        }

        [HttpPost("open")]
        [Authorize]
        [ProducesResponseType(typeof(GateOperationResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(GateOperationResponse), StatusCodes.Status202Accepted)]
        public async Task<IActionResult> Open(
           [FromBody] OpenGateRequest request,
           CancellationToken cancellationToken)
        {
            var command = new OpenGateCommand
            {
                ControllerId = request.ControllerId,
                OperationType = request.OperationType,
                TicketId = request.TicketId,
                RequestId = request.RequestId
            };

            var result = await sender.Send(command, cancellationToken);
            return result.ToOperationResponse();
        }

        [Authorize]
        [HttpGet("operations/{operationId:guid}")]
        public async Task<IActionResult> GetOperation(Guid operationId,
            [FromServices] GateOperationService operations, [FromServices] ITokenReader tokens, CancellationToken ct)
        {
            if (User.GetGateId(tokens) is not Guid gateId)
                return Forbid();
            return (await operations.GetAsync(operationId, gateId, ct)).ToOperationResponse();
        }

        [Authorize]
        [HttpGet("operations")]
        public async Task<IActionResult> ListOperations([FromQuery] Guid? ticketId, [FromQuery] int? controllerId,
            [FromQuery] GateOperationStatus? status, [FromServices] GateOperationService operations,
            [FromServices] ITokenReader tokens, CancellationToken ct)
        {
            if (User.GetGateId(tokens) is not Guid gateId)
                return Forbid();
            return Ok(await operations.ListAsync(gateId, ticketId, controllerId, status, ct));
        }

        [Authorize]
        [HttpPost("operations/{operationId:guid}/retry")]
        public async Task<IActionResult> Retry(Guid operationId, [FromServices] GateOperationService operations,
            [FromServices] IGateOperationDispatcher dispatcher, [FromServices] ITokenReader tokens, CancellationToken ct)
        {
            if (User.GetGateId(tokens) is not Guid gateId)
                return Forbid();
            if (!await dispatcher.RetryAsync(operationId, gateId, ct))
            {
                var existing = await operations.GetAsync(operationId, gateId, ct);
                if (existing.IsFailure)
                    return existing.ToOperationResponse();
                if (existing.Value.Status is "Completed" or "Failed" or "TimedOut" or "Unknown")
                    return Result<GateOperationResponse>.Fail(TicketExitEligibilityService.AsError("GateFreshRequestRequired"))
                        .ToOperationResponse();
                return Conflict(existing.Value);
            }
            return (await operations.GetAsync(operationId, gateId, ct)).ToOperationResponse();
        }

        [Authorize(Roles = "Supervisor")]
        [HttpPost("operations/{operationId:guid}/reconcile-non-execution")]
        public async Task<IActionResult> Reconcile(Guid operationId, [FromBody] ReconciliationRequest request,
            [FromServices] ITokenReader tokens, [FromServices] IGateOperationDispatcher dispatcher, CancellationToken ct)
        {
            if (User.GetGateId(tokens) is not Guid gateId || User.GetUserId(tokens) is not Guid userId)
                return Forbid();
            if (string.IsNullOrWhiteSpace(request.Evidence) || request.Evidence.Length > 500)
                return BadRequest();
            var result = await sender.Send(new AdvanceGateOperation(operationId, GateTransition.ReconcileNonExecution,
                gateId, userId, request.Evidence.Trim()), ct);
            if (result.IsFailure)
                return result.ToIActionResult();
            if (result.Value.Operation is null)
                return Conflict();
            await dispatcher.PublishStatusAsync(result.Value);
            return Ok(result.Value.Operation);
        }

        public sealed record ReconciliationRequest(string Evidence);

        [HttpGet("{gateId:guid}/connection-info")]
        public async Task<IActionResult> GetConnectionInfo(
           [FromRoute] Guid gateId,
           CancellationToken cancellationToken)
        {
            var result = await sender.Send(
                new GetGateConnectionInfoQuery(gateId),
                cancellationToken);

            return result.ToIActionResult(cancellationToken);
        }
    }
}
