namespace Zeti.Fleet.Billing.Services;

public interface IBillFormatter
{
    string Format(string customer, decimal amount);
    string ContentType { get; }
}