﻿using BuildingBlock.Application.Repositories;
using FluentValidation;
using NPark.Domain.Entities;
using NPark.Domain.Enums;
using NPark.Domain.Resource;

namespace NPark.Application.Feature.PricingSchemaManagement.Command.Add
{
    public class AddPricingSchemaCommandValidation : AbstractValidator<AddPricingSchemaCommand>
    {
        private IGenericRepository<PricingScheme> _repository;

        public AddPricingSchemaCommandValidation(IGenericRepository<PricingScheme> repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));

            RuleFor(x => x.Name).NotEmpty().WithMessage(ErrorMessage.IsRequired);
            RuleFor(x => x.DurationType).IsInEnum().WithMessage(ErrorMessage.IsRequired);
            RuleFor(x => x.StartTime).NotEmpty().WithMessage(ErrorMessage.IsRequired).When(x => !x.IsRepeated);
            RuleFor(x => x.EndTime).NotEmpty().WithMessage(ErrorMessage.IsRequired).When(x => !x.IsRepeated);
            RuleFor(x => x.Price).GreaterThanOrEqualTo(0).WithMessage(ErrorMessage.InvalidPrice);
            RuleFor(x => x.IsRepeated).NotNull().WithMessage(ErrorMessage.IsRequired);

            // OrderPriority is required only if IsRepeated is true
            RuleFor(x => x.OrderPriority)
                .NotEmpty().When(x => x.IsRepeated)
                .WithMessage(ErrorMessage.OrderPiorityIsRequired);

            // Ensure OrderPriority is unique when IsRepeated is true
            RuleFor(x => x.OrderPriority)
                .MustAsync(async (orderPriority, cancellationToken) =>
                !await _repository.IsExistAsync(x => x.OrderPriority == orderPriority, cancellationToken))
                .When(x => x.IsRepeated)
                .WithMessage(ErrorMessage.OrderPiority_Unique);

            // Ensure EndTime is not before StartTime
            RuleFor(x => x.EndTime)
                .GreaterThan(x => x.StartTime)
                .WithMessage(ErrorMessage.Invalid_EndTime)
                .When(x => !x.IsRepeated);

            // Ensure IsRepeated is not true when DurationType is Days
            RuleFor(x => x.IsRepeated)
                .Must((command, isRepeated) => !(isRepeated && command.DurationType == DurationType.Days))
                .WithMessage(ErrorMessage.Conflict_IsRepeated);

            // Ensure TotalDays is required and greater than zero when DurationType is Days
            RuleFor(x => x.TotalDays)
                .NotEmpty().When(x => x.DurationType == DurationType.Days)
                .WithMessage(ErrorMessage.TotalDay_Require)
                .GreaterThan(0).When(x => x.DurationType == DurationType.Days)
                .WithMessage(ErrorMessage.Invalid_TotalDays);
            // Ensure TotalHours is required and greater than zero when DurationType is Hours
            RuleFor(x => x.TotalHours)
    .NotEmpty().When(x => x.DurationType == DurationType.Hours)
    .WithMessage(ErrorMessage.TotalHour_Require)
    .GreaterThan(0).When(x => x.DurationType == DurationType.Hours)
    .WithMessage(ErrorMessage.Invalid_TotalHour);
        }
    }
}