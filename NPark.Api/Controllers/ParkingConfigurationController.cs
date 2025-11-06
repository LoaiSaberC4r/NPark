using BuildingBlock.Api;
using BuildingBlock.Api.ControllerTemplate;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NPark.Application.Feature.ParkingSystemConfigurationManagement.Command.Update;
using NPark.Application.Feature.ParkingSystemConfigurationManagement.Query.GetSystemConfiguration;

namespace NPark.Api.Controllers
{
    [Route("api/[controller]")]
    public class ParkingConfigurationController : ControllerTemplate
    {
        public ParkingConfigurationController(ISender sender) : base(sender)
        {
        }

        [HttpPost(nameof(Update))]
        public async Task<IActionResult> Update([FromBody] UpdateParkingConfigurationCommand command, CancellationToken cancellationToken)
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToIActionResult();
        }

        [HttpGet(nameof(Get))]
        public async Task<IActionResult> Get([FromQuery] GetSystemConfigurationQuery query, CancellationToken cancellationToken)
        {
            var result = await sender.Send(query, cancellationToken);
            return result.ToIActionResult();
        }
    }
}