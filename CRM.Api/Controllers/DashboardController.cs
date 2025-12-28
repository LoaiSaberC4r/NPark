using BuildingBlock.Api;
using BuildingBlock.Api.ControllerTemplate;
using CRM.Application.Feature.DashboardManagement.Query.AdminDashboard;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CRM.Api.Controllers
{
    [Route("api/[controller]")]
    public class DashboardController : ControllerTemplate
    {
        public DashboardController(ISender sender) : base(sender)
        {
        }

        [HttpGet]
        public async Task<IActionResult> GetAdminDashboard([FromQuery] AdminDashboardQuery query, CancellationToken cancellationToken)
        {
            var result = await sender.Send(query, cancellationToken);
            return result.ToIActionResult();
        }
    }
}