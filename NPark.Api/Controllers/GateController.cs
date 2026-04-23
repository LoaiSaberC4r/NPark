using BuildingBlock.Api;
using BuildingBlock.Api.ControllerTemplate;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NPark.Api.Contracts;
using NPark.Application.Feature.GateManagement.Command.OpenGate;
using NPark.Application.Feature.GateManagement.Query.GetAll;

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
        [ProducesResponseType(typeof(OpenGateCommandResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> Open(
           [FromBody] OpenGateRequest request,
           CancellationToken cancellationToken)
        {
            var command = new OpenGateCommand
            {
                ControllerId = request.ControllerId
            };

            var result = await sender.Send(command, cancellationToken);
            return result.ToIActionResult(cancellationToken);
        }
    }
}