using BuildingBlock.Api;
using BuildingBlock.Api.ControllerTemplate;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NPark.Application.Feature.PricingSchemaManagement.Command.Add;
using NPark.Application.Feature.PricingSchemaManagement.Query.GetAll;

namespace NPark.Api.Controllers
{
    [Route("api/[controller]")]
    public class PricingSchemaController : ControllerTemplate
    {
        public PricingSchemaController(ISender sender) : base(sender)
        {
        }

        [HttpPost(nameof(Add))]
        public async Task<IActionResult> Add([FromBody] AddPricingSchemaCommand command, CancellationToken cancellationToken)
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToIActionResult();
        }

        [HttpGet(nameof(GetAll))]
        public async Task<IActionResult> GetAll([FromQuery] GetAllPricingSchemaQuery query, CancellationToken cancellationToken)
        {
            var result = await sender.Send(query, cancellationToken);
            return result.ToIActionResult();
        }
    }
}