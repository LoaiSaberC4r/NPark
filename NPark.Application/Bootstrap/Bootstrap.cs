using NPark.Application.Services;
using BuildingBlock.Application.Behaviors;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace NPark.Application.Bootstrap
{
    public static class Bootstrap
    {
        //Mediator Injection
        private static IServiceCollection AddMediatorInjection(this IServiceCollection services)
        {
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(AssemblyReference.Assembly);
                cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
                cfg.AddOpenBehavior(typeof(TracingBehavior<,>));
                cfg.AddOpenBehavior(typeof(ExceptionMappingBehavior<,>));
                cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
                cfg.AddOpenBehavior(typeof(CacheBehavior<,>));
                cfg.AddOpenBehavior(typeof(ResilienceBehavior<,>));
                cfg.AddOpenBehavior(typeof(TransactionBehavior<,>));
            });
            return services;
        }

        private static IServiceCollection AddFluentValidation(this IServiceCollection services)
        {
            services.AddValidatorsFromAssembly(
                   AssemblyReference.Assembly,
                   includeInternalTypes: true);
            return services;
        }

        public static IServiceCollection AddApplicationBootstrap(this IServiceCollection services)
        {
            services.AddScoped<ParkingBusinessClock>();
            services.AddScoped<OvernightPenaltyPolicy>();
            services.AddScoped<ITicketExitEligibilityService, TicketExitEligibilityService>();
            services.AddScoped<ITicketArchiveService, TicketArchiveService>();
            services.AddScoped<GateOperationService>();
            services.AddScoped<IFinancialSettlementService, FinancialSettlementService>();
            services.AddScoped<SettlementAudit>();
            services.AddFluentValidation();
            services.AddMediatorInjection();
            return services;
        }
    }
}