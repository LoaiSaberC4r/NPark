using BuildingBlock.Application.Abstraction;
using BuildingBlock.Application.Abstraction.QrCode;
using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using NPark.Application.Options;
using NPark.Domain.Entities;

namespace NPark.Application.Feature.TicketsManagement.Command.Add
{
    public sealed class AddTicketCommandHandler : ICommandHandler<AddTicketCommand, byte[]>
    {
        private readonly IGenericRepository<Ticket> _ticketRepository;
        private readonly IQRCodeService _qrCodeService;
        private readonly SalaryConfig _option;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AddTicketCommandHandler(IGenericRepository<Ticket> ticketRepository,
            IQRCodeService qrCodeService, IOptionsMonitor<SalaryConfig> option,
            IHttpContextAccessor httpContextAccessor)
        {
            _ticketRepository = ticketRepository ?? throw new ArgumentNullException(nameof(ticketRepository));
            _qrCodeService = qrCodeService ?? throw new ArgumentNullException(nameof(qrCodeService));
            _option = option.CurrentValue ?? throw new ArgumentNullException(nameof(option));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        public async Task<Result<byte[]>> Handle(AddTicketCommand request, CancellationToken cancellationToken)
        {
            var endTime = DateTime.UtcNow.Add(_option.AllowedTime);
            var entity = Ticket.Create(DateTime.UtcNow, endTime, _option.Salary);
            await _ticketRepository.AddAsync(entity, cancellationToken);
            await _ticketRepository.SaveChangesAsync(cancellationToken);
            var uri = GetUri(entity.Id.ToString());
            var qrCode = _qrCodeService.GenerateQRCode(uri);
            return Result<byte[]>.Ok(qrCode);
        }

        private string GetUri(string id)
        {
            var request = _httpContextAccessor.HttpContext.Request;
            var ipHost = $"{request.Scheme}://{request.Host.Value}";

            var fullUrl = $"{ipHost}?id={id}";

            return fullUrl;
        }
    }
}