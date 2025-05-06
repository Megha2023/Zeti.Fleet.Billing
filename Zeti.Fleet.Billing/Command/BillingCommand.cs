using Zeti.Fleet.Billing.Model;
using Zeti.Fleet.Billing.Services;

namespace Zeti.Fleet.Billing.Command;

public class BillingCommand(IBillingService billService, BillingRequest? request)
{
    public async Task<decimal> ExecuteAsync()
    {
        return await billService.CalculateBillAsync(request);
    }
}