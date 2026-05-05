using BuildingBlock.Application.Repositories;
using FluentValidation;
using NPark.Domain.Entities;
using NPark.Domain.FileNames;
using NPark.Domain.Resource;

namespace NPark.Application.Feature.UserManagement.Command.Update
{
    public sealed class UpdateUserCommandValidation : AbstractValidator<UpdateUserCommand>
    {
        private readonly IGenericRepository<Role> _roleRepository;

        private const string PasswordRegex =
            @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).{8,128}$";

        public UpdateUserCommandValidation(
            IGenericRepository<User> userRepository,
            IGenericRepository<Role> roleRepository)
        {
            _roleRepository = roleRepository ?? throw new ArgumentNullException(nameof(roleRepository));

            RuleFor(x => x.Id)
                .NotEmpty()
                .WithMessage(ErrorMessage.IsRequired)
                .MustAsync(async (id, ct) =>
                    await userRepository.IsExistAsync(u => u.Id == id, ct))
                .WithMessage(ErrorMessage.NotFound);

            RuleFor(x => x.Email)
                .NotEmpty()
                .WithMessage(ErrorMessage.Email_Required)
                .EmailAddress()
                .WithMessage(ErrorMessage.Invalid_Email)
                .MustAsync(async (command, email, ct) =>
                    !await userRepository.IsExistAsync(
                        u => u.Email == email && u.Id != command.Id,
                        ct))
                .WithMessage(ErrorMessage.Email_Exist);

            RuleFor(x => x.UserName)
                .NotEmpty()
                .WithMessage(ErrorMessage.UserName_Required)
                .MaximumLength(100)
                .WithMessage("User name must not exceed 100 characters. / يجب ألا يزيد اسم المستخدم عن 100 حرف.")
                .MustAsync(async (command, userName, ct) =>
                    !await userRepository.IsExistAsync(
                        u => u.Username == userName && u.Id != command.Id,
                        ct))
                .WithMessage(ErrorMessage.UserName_Exist);

            RuleFor(x => x.RoleId)
                .NotEmpty()
                .WithMessage(ErrorMessage.Role_Requred)
                .Must(roleId => roleId != Guid.Empty)
                .WithMessage(ErrorMessage.Role_Requred)
                .MustAsync(async (roleId, ct) =>
                    await _roleRepository.IsExistAsync(r => r.Id == roleId, ct))
                .WithMessage(ErrorMessage.Role_Exist);

            RuleFor(x => x.Password)
                .Matches(PasswordRegex)
                .WithMessage(ErrorMessage.UserManagement_Password_Invalid)
                .When(x => !string.IsNullOrWhiteSpace(x.Password));

            RuleFor(x => x)
                .Must(x => x.ShiftStartTime.HasValue == x.ShiftEndTime.HasValue)
                .WithMessage("Shift start time and shift end time must be provided together. / يجب إدخال وقت بداية ونهاية الشيفت معًا.");

            RuleFor(x => x.ShiftStartTime)
                .NotNull()
                .WithMessage("Shift start time is required for cashier users. / وقت بداية الشيفت مطلوب للكاشير.")
                .When(x => AppRoleIds.IsCashier(x.RoleId));

            RuleFor(x => x.ShiftEndTime)
                .NotNull()
                .WithMessage("Shift end time is required for cashier users. / وقت نهاية الشيفت مطلوب للكاشير.")
                .When(x => AppRoleIds.IsCashier(x.RoleId));

            RuleFor(x => x)
                .Must(command =>
                {
                    if (AppRoleIds.IsCashier(command.RoleId))
                    {
                        return true;
                    }

                    return command.ShiftStartTime is null &&
                           command.ShiftEndTime is null;
                })
                .WithMessage("Shift time is allowed only for EntranceCashier and ExitCashier roles. / الشيفت مسموح فقط لكاشير الدخول وكاشير الخروج.");
        }
    }
}