using BuildingBlock.Application.Abstraction;
using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NPark.Domain.Entities;
using NPark.Domain.Resource;

namespace NPark.Application.Feature.PricingSchemaManagement.Command.Delete
{
    public sealed class DeletePricingSchemaCommandHandler : ICommandHandler<DeletePricingSchemaCommand>
    {
        private readonly IGenericRepository<PricingScheme> _pricingRepo;
        private readonly IGenericRepository<OrderPricingSchema> _orderSchemaRepo;
        private readonly IGenericRepository<ParkingMemberships> _membershipRepo;

        public DeletePricingSchemaCommandHandler(
            IGenericRepository<PricingScheme> pricingRepo,
            IGenericRepository<OrderPricingSchema> orderSchemaRepo,
            IGenericRepository<ParkingMemberships> membershipRepo)
        {
            _pricingRepo = pricingRepo ?? throw new ArgumentNullException(nameof(pricingRepo));
            _orderSchemaRepo = orderSchemaRepo ?? throw new ArgumentNullException(nameof(orderSchemaRepo));
            _membershipRepo = membershipRepo ?? throw new ArgumentNullException(nameof(membershipRepo));
        }

        public async Task<Result> Handle(DeletePricingSchemaCommand request, CancellationToken cancellationToken)
        {
            // 1) Load entity (guard in handler)
            var entity = await _pricingRepo.GetByIdAsync(request.Id, cancellationToken);
            if (entity is null)
            {
                return Result.Fail(new Error(
                    Code: "PricingScheme.NotFound",
                    Message: ErrorMessage.PricingSchema_NotFound,
                    Type: ErrorType.NotFound));
            }

            // 2) Prevent delete if referenced (separate messages per source)
            var referencedByOrdersTask = await _orderSchemaRepo.IsExistAsync(
                x => x.PricingSchemaId == request.Id,
                cancellationToken);

            var referencedByMembershipsTask = await _membershipRepo.IsExistAsync(
                x => x.PricingSchemeId == request.Id,
                cancellationToken);

            if (referencedByOrdersTask)
            {
                return Result.Fail(new Error(
                    Code: "PricingScheme.Delete.ReferencedByOrders",
                    Message: ErrorMessage.PricingScheme_Delete_ReferencedByOrders,
                    Type: ErrorType.Conflict));
            }

            if (referencedByMembershipsTask)
            {
                return Result.Fail(new Error(
                    Code: "PricingScheme.Delete.ReferencedByMemberships",
                    Message: ErrorMessage.PricingScheme_Delete_ReferencedByMemberships,
                    Type: ErrorType.Conflict));
            }

            // 3) Delete
            _pricingRepo.Delete(entity);

            // 4) Safety net (race condition / FK constraint)
            try
            {
                await _pricingRepo.SaveChangesAsync(cancellationToken);
                return Result.Ok();
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 547 })
            {
                return Result.Fail(new Error(
                    Code: "PricingScheme.Delete.ForeignKeyConflict",
                    Message: ErrorMessage.PricingScheme_Delete_ForeignKeyConflict,
                    Type: ErrorType.Conflict));
            }
        }
    }
}