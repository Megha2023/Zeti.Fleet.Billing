namespace Zeti.Fleet.Billing.Services;

public class BillFormatterFactory(IEnumerable<IBillFormatter> formatters)
{
    public IBillFormatter GetFormatter(string acceptHeader)
    {
        return formatters.FirstOrDefault(f => f.ContentType.Equals(acceptHeader, StringComparison.OrdinalIgnoreCase))
               ?? formatters.First(f => f.ContentType == "application/json");
    }
}