using System.Text.Json;

namespace Zeti.Fleet.Billing.Services;

public class JsonBillFormatter : IBillFormatter
{
    public string ContentType => "application/json";

    public string Format(string customer, decimal amount)
    {
        var result = new { Customer = customer, Amount = amount };
        return JsonSerializer.Serialize(result);
    }
}