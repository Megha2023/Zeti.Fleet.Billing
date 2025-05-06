using Zeti.Fleet.Billing.Model;

namespace Zeti.Fleet.Billing.Services;

public interface IBillingService
{
    Task<decimal> CalculateBillAsync(BillingRequest? billingReq);
}