using BuildingBlock.Application.Abstraction;
using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
using NPark.Domain.Entities;

namespace NPark.Application.Feature.PricingSchemaManagement.Command.AddOrderSchema
{
    public sealed class AddOrderSchemaCommandHandler : ICommandHandler<AddOrderSchemaCommand>
    {
        private readonly IGenericRepository<OrderPricingSchema> _orderPricingSchemaRepository;

        public AddOrderSchemaCommandHandler(IGenericRepository<OrderPricingSchema> orderPricingSchemaRepository)
        {
            _orderPricingSchemaRepository = orderPricingSchemaRepository ?? throw new ArgumentNullException(nameof(orderPricingSchemaRepository));
        }

        public async Task<Result> Handle(AddOrderSchemaCommand request, CancellationToken cancellationToken)
        {
            var entities = _orderPricingSchemaRepository.GetAll().ToList();
            if (entities is not null || entities?.Count > 0)
            {
                _orderPricingSchemaRepository.DeleteRange(entities);
                await _orderPricingSchemaRepository.SaveChangesAsync(cancellationToken);
            }

            var orderList = request.OrderSchema.OrderBy(x => x.Count).ToList();
            var list = new List<OrderPricingSchema>();

            for (int i = 1; i <= orderList.Count; i++)
            {
                list.Add(OrderPricingSchema.Create(orderList[i - 1].PricingSchemaId, i));
            }

            await _orderPricingSchemaRepository.AddRangeAsync(list, cancellationToken);
            await _orderPricingSchemaRepository.SaveChangesAsync(cancellationToken);
            return Result.Ok();
        }
    }
}