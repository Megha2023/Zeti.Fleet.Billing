using FluentValidation;
using Zeti.Fleet.Billing.Model;

namespace Zeti.Fleet.Billing.Validator;

public class BillingValidator : AbstractValidator<BillingRequest>
{
    public BillingValidator()
    {
        RuleFor(x => x.Customer)
            .NotEmpty().WithMessage("Customer name is required.");

        RuleFor(x => x.Vehicles)
            .NotEmpty().WithMessage("Vehicles list is required.");

        RuleFor(x => x.StartDate)
            .LessThan(x => x.EndDate)
            .WithMessage("Start date must be earlier than end date.");
    }
}