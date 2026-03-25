using BuildingBlock.Application.Abstraction;
using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
using NPark.Domain.Entities;
using NPark.Domain.Resource;

namespace NPark.Application.Feature.UserManagement.Command.Delete
{
    internal sealed class DeleteSystemUserCommandHandler : ICommandHandler<DeleteSystemUserCommand>
    {
        private readonly IGenericRepository<User> _userRepository;
        private static readonly Guid AdminRoleId = new Guid("f47ac10b-58cc-4372-a567-0e02b2c3d479");

        public DeleteSystemUserCommandHandler(IGenericRepository<User> repository)
        {
            _userRepository = repository;
        }

        public async Task<Result> Handle(DeleteSystemUserCommand request, CancellationToken cancellationToken)
        {
            var entity = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (entity is null)
            {
                return Result.Fail(new Error(
                    Code: "User.NotFound",
                    Message: ErrorMessage.UserManagement_Id_NotFound,
                    Type: ErrorType.NotFound));
            }

            if (entity.RoleId == AdminRoleId)
            {
                return Result.Fail(new Error(
                    Code: "User.UpdateForbidden",
                    Message: ErrorMessage.UserManagement_RoleId_AdminImmutable,
                    Type: ErrorType.Security));
            }
            _userRepository.Delete(entity);
            await _userRepository.SaveChangesAsync(cancellationToken);
            return Result.Ok();
        }
    }
}