using BuildingBlock.Application.Abstraction;
using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
using BuildingBlock.Domain.SharedDto;
using NPark.Domain.Entities;
using NPark.Domain.Resource;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NPark.Application.Feature.UserManagement.Query.GetUsersByRoleId
{
    internal sealed class GetUsersByRoleIdQueryHandler
        : IQueryHandler<GetUsersByRoleIdQuery, GetUsersByRoleIdResponse>
    {
        private readonly IGenericRepository<User> _userRepository;
        private readonly IGenericRepository<Role> _roleRepository;

        public GetUsersByRoleIdQueryHandler(
            IGenericRepository<User> userRepository,
            IGenericRepository<Role> roleRepository)
        {
            _userRepository = userRepository
                ?? throw new ArgumentNullException(nameof(userRepository));

            _roleRepository = roleRepository
                ?? throw new ArgumentNullException(nameof(roleRepository));
        }

        public async Task<Result<GetUsersByRoleIdResponse>> Handle(
            GetUsersByRoleIdQuery request,
            CancellationToken cancellationToken)
        {
            var roleSpec = new GetRoleForUsersGroupSpec(request.RoleId);

            var role = await _roleRepository.FirstOrDefaultWithSpecAsync(
                roleSpec,
                cancellationToken);

            if (role is null)
            {
                return Result<GetUsersByRoleIdResponse>.Fail(new Error(
                    Code: "Users.GetByRole.RoleNotFound",
                    Message: ErrorMessage.Role_NotFound,
                    Type: ErrorType.NotFound));
            }

            var usersSpec = new GetUsersByRoleIdSpec(request);

            var (users, totalCount) = _userRepository.GetWithSpec(
                usersSpec
                );

            var pagedUsers = new Pagination<UserByRoleResponse>(
                currentPage: request.PageNumber,
                pageSize: request.PageSize,
                totalItems: totalCount,
                data: users.ToList());

            var response = new GetUsersByRoleIdResponse
            {
                RoleId = role.RoleId,
                RoleNameEn = role.RoleNameEn,
                RoleNameAr = role.RoleNameAr,
                Users = pagedUsers
            };

            return Result<GetUsersByRoleIdResponse>.Ok(response);
        }
    }
}