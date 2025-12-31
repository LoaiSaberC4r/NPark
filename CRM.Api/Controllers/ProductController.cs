using BuildingBlock.Api;
using BuildingBlock.Api.ControllerTemplate;
using CRM.Api.Attribute;
using CRM.Application.Feature.ProductManagement.Command.Add;
using CRM.Application.Feature.ProductManagement.Command.RenewProductSite;
using CRM.Application.Feature.ProductManagement.Query.GetAll;
using CRM.Application.Feature.ProductManagement.Query.GetClosedProducts;
using CRM.Application.Feature.ProductManagement.Query.GetProductEndedWithin30Days;
using CRM.Application.Feature.ProductManagement.Query.GetProducts;
using CRM.Application.Feature.ProductManagement.Query.GetRenewProductMonthly;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CRM.Api.Controllers
{
    [Route("api/[controller]")]
    public class ProductController : ControllerTemplate
    {
        public ProductController(ISender sender) : base(sender)
        {
        }

        [Permission("Platform:Products:Create")]
        [HttpPost(nameof(Add))]
        public async Task<IActionResult> Add([FromForm] AddProductCommand command, CancellationToken cancellationToken)
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToIActionResult();
        }

        [Permission("Platform:Products:Read")]
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] GetAllProductQuery query, CancellationToken cancellationToken)
        {
            var result = await sender.Send(query, cancellationToken);
            return result.ToIActionResult();
        }

        [Permission("Platform:Products:Read")]
        [HttpGet(nameof(GetProducts))]
        public async Task<IActionResult> GetProducts([FromQuery] GetProductsQuery query, CancellationToken cancellationToken)
        {
            var result = await sender.Send(query, cancellationToken);
            return result.ToIActionResult();
        }

        [HttpGet(nameof(GetProductEndedWithin30Days))]
        public async Task<IActionResult> GetProductEndedWithin30Days([FromQuery] GetProductEndedWithin30DaysQuery query, CancellationToken cancellationToken)
        {
            var result = await sender.Send(query, cancellationToken);

            return result.ToIActionResult();
        }

        [HttpGet(nameof(GetClosedProducts))]
        public async Task<IActionResult> GetClosedProducts([FromQuery] GetClosedProductsQuery query, CancellationToken cancellationToken)
        {
            var result = await sender.Send(query, cancellationToken);
            return result.ToIActionResult();
        }

        [HttpPost(nameof(RenewProduct))]
        public async Task<IActionResult> RenewProduct([FromBody] RenewProductSiteCommand command, CancellationToken cancellationToken)
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToIActionResult();
        }

        [HttpGet(nameof(GetRenewProductMonthly))]
        public async Task<IActionResult> GetRenewProductMonthly([FromQuery] GetRenewProductMonthlyQuery query, CancellationToken cancellationToken)
        {
            var result = await sender.Send(query, cancellationToken);
            return result.ToIActionResult();
        }
    }
}