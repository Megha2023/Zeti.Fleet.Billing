using Microsoft.AspNetCore.SignalR;

namespace Zeti.Fleet.Billing;

public class BillingOptions
{
    public decimal CostPerMile { get; set; }
    public string readingUrl { get; set; }
}