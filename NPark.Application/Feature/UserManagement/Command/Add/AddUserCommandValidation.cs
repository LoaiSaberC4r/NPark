using BuildingBlock.Application.Abstraction.Encryption;
using BuildingBlock.Application.Repositories;
using FluentValidation;
using NPark.Domain.Entities;
using NPark.Domain.FileNames;
using NPark.Domain.Resource;

namespace NPark.Application.Feature.UserManagement.Command.Add
{
    public sealed class AddUserCommandValidation : AbstractValidator<AddUserCommand>
    {
        private readonly IGenericRepository<Role> _roleRepository;
        private readonly IGenericRepository<User> _userRepository;
        private readonly IPasswordService _passwordService;

        public AddUserCommandValidation(
            IGenericRepository<Role> roleRepository,
            IPasswordService passwordService,
            IGenericRepository<User> userRepository)
        {
            _roleRepository = roleRepository;
            _passwordService = passwordService;
            _userRepository = userRepository;

            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Name is required. / الاسم مطلوب.");

            RuleFor(x => x.Email)
                .NotEmpty()
                .WithMessage(ErrorMessage.Email_Required)
                .EmailAddress()
                .WithMessage(ErrorMessage.Invalid_Email)
                .MustAsync(async (email, ct) =>
                    !await _userRepository.IsExistAsync(u => u.Email == email, ct))
                .WithMessage(ErrorMessage.Email_Exist);

            RuleFor(x => x.UserName)
                .NotEmpty()
                .WithMessage(ErrorMessage.UserName_Required)
                .MaximumLength(100)
                .WithMessage("User name must not exceed 100 characters. / يجب ألا يزيد اسم المستخدم عن 100 حرف.")
                .MustAsync(async (userName, ct) =>
                    !await _userRepository.IsExistAsync(u => u.Username == userName, ct))
                .WithMessage(ErrorMessage.UserName_Exist);

            RuleFor(x => x.Password)
                .NotEmpty()
                .WithMessage(ErrorMessage.PasswordRequired)
                .Must(password => _passwordService.IsStrongPassword(password))
                .WithMessage(ErrorMessage.Invalid_Password);

            RuleFor(x => x.RoleId)
                .NotEmpty()
                .WithMessage(ErrorMessage.Role_Requred)
                .MustAsync(async (roleId, ct) =>
                    await _roleRepository.IsExistAsync(r => r.Id == roleId, ct))
                .WithMessage(ErrorMessage.Role_Exist);

            RuleFor(x => x.NationalId)
                .NotEmpty()
                .WithMessage(ErrorMessage.IsRequired)
                .Length(14)
                .WithMessage(ErrorMessage.Invalid_NationalId)
                .Matches(@"^\d{14}$")
                .WithMessage(ErrorMessage.Invalid_NationalId)
                .MustAsync(async (nationalId, ct) =>
                    !await _userRepository.IsExistAsync(u => u.NationalId == nationalId, ct))
                .WithMessage(ErrorMessage.Unique_NationalId);

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