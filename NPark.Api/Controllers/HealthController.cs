using BuildingBlock.Api.ControllerTemplate;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace NPark.Api.Controllers
{
    [Route("api/[controller]")]
    public class HealthController : ControllerTemplate
    {
        public HealthController(ISender sender) : base(sender)
        {
        }

        [HttpGet]
        public IActionResult GetHealth()
        {
            return Ok();
        }
    }
}