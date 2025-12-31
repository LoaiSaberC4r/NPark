using BuildingBlock.Application.Abstraction;
using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
using CRM.Application.Abstraction;
using CRM.Application.Shared.Dto;
using CRM.Domain.Entities;

namespace CRM.Application.Feature.SupportManagement.Command.Add
{
    internal sealed record AddSupportCommandHandler : ICommandHandler<AddSupportCommand>
    {
        private readonly IGenericRepository<SuperAdmin> _superAdminRepository;
        private readonly IEmailSender _emailSender;
        public AddSupportCommandHandler(IGenericRepository<SuperAdmin> superAdminRepository, IEmailSender emailSender)
        {
            _superAdminRepository = superAdminRepository ?? throw new ArgumentNullException(nameof(superAdminRepository));
            _emailSender = emailSender ?? throw new ArgumentNullException(nameof(emailSender));
        }
        public async Task<Result> Handle(AddSupportCommand request, CancellationToken cancellationToken)
        {
            var superAdmin = SuperAdmin.Create(
                  email: request.Email,
                  password: request.Password,
                  phoneNumber: request.PhoneNumber ?? string.Empty
              );

            await _superAdminRepository.AddAsync(superAdmin, cancellationToken);
            await _superAdminRepository.SaveChangesAsync(cancellationToken);
            _emailSender.SendAsync(
                               new EmailMessage(
                                   To: request.Email,
                                   Subject: "Account created successfully | تم إنشاء الحساب بنجاح",
                                   HtmlBody: $@"
                            <div style='font-family: Arial, sans-serif; line-height:1.7'>

                              <!-- Arabic -->
                              <div dir='rtl' lang='ar' style='text-align:right; border:1px solid #eee; padding:16px; border-radius:8px;'>
                                <h3 style='margin:0 0 10px;'>تم إنشاء حسابك بنجاح ✅</h3>

                                <p style='margin:0 0 10px;'>
                                  نود إعلامك بأنه تم <b>إنشاء حسابك بنجاح</b>.
                                </p>

                                <p style='margin:0 0 10px;'>
                                  <b>بيانات الدخول:</b><br/>
                                  📧 البريد الإلكتروني: <b>{request.Email}</b><br/>
                                  🔑 كلمة المرور: <b>{request.Password}</b>
                                </p>

                                <p style='margin:0; color:#b00020;'>
                                  ⚠️ يرجى تغيير كلمة المرور عند تسجيل الدخول لأول مرة حفاظًا على أمان حسابك.
                                </p>
                              </div>

                              <div style='height:14px;'></div>

                              <!-- English -->
                              <div dir='ltr' lang='en' style='text-align:left; border:1px solid #eee; padding:16px; border-radius:8px;'>
                                <h3 style='margin:0 0 10px;'>Your account has been created successfully ✅</h3>

                                <p style='margin:0 0 10px;'>
                                  We’re pleased to inform you that your account has been <b>successfully created</b>.
                                </p>

                                <p style='margin:0 0 10px;'>
                                  <b>Login details:</b><br/>
                                  📧 Email: <b>{request.Email}</b><br/>
                                  🔑 Password: <b>{request.Password}</b>
                                </p>

                                <p style='margin:0; color:#b00020;'>
                                  ⚠️ For security reasons, please change your password after your first login.
                                </p>
                              </div>

                            </div>"
                               ),
                               cancellationToken);
            return Result.Ok();
        }
    }
}