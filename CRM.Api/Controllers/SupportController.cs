using BuildingBlock.Api;
using BuildingBlock.Api.ControllerTemplate;
using CRM.Api.Attribute;
using CRM.Application.Feature.SupportManagement.Command.Add;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CRM.Api.Controllers
{
    [Route("api/[controller]")]
    public class SupportController : ControllerTemplate
    {
        public SupportController(ISender sender) : base(sender)
        {
        }

        [Permission("Platform:Support:Create")]
        [HttpPost]
        public async Task<IActionResult> AddSupport([FromBody] AddSupportCommand command, CancellationToken cancellationToken)
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToIActionResult();
        }
    }
}