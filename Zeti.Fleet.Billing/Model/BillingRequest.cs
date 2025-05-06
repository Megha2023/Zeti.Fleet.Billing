namespace Zeti.Fleet.Billing.Model;

public class BillingRequest
{
    public string Customer { get; set; }
    public List<string> Vehicles { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}