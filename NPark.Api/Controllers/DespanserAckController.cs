using BuildingBlock.Api;
using BuildingBlock.Api.ControllerTemplate;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NPark.Application.Feature.DespanserManagement.Command.ExitAck;

namespace NPark.Api.Controllers
{
    [Route("api/[controller]")]
    public class DespanserAckController : ControllerTemplate
    {
        public DespanserAckController(ISender sender) : base(sender)
        {
        }

        [HttpPost(nameof(ExitAck))]
        public async Task<IActionResult> ExitAck([FromBody] ExitAckCommand command, CancellationToken cancellationToken)
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToIActionResult();
        }
    }
}