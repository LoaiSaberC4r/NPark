using BuildingBlock.Api;
using BuildingBlock.Api.ControllerTemplate;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NPark.Application.Feature.GateConfig.Command.SetTimeConfig;

namespace NPark.Api.Controllers
{
    [Route("api/[controller]")]
    public class ConfigurationController : ControllerTemplate
    {
        public ConfigurationController(ISender sender) : base(sender)
        {
        }

        [HttpPost(nameof(SetTimeConfig))]
        public async Task<IActionResult> SetTimeConfig([FromBody] SetTimeConfigCommand command, CancellationToken cancellationToken)
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToIActionResult();
        }
    }
}