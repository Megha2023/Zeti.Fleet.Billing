using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using Zeti.Fleet.Billing.Model;

namespace Zeti.Fleet.Billing.Services;

public class BillingService(HttpClient httpClient, ILogger<BillingService> logger, IOptions<BillingOptions> options) : IBillingService
{
    private readonly decimal _costPerMile = options.Value.CostPerMile;

    public async Task<decimal> CalculateBillAsync(BillingRequest? billingReq)
    {
        var vehicles = new List<string> { "CBDH 789", "86532 AZE" };
       
        decimal totalMiles = 0;

        foreach (var vehicleId in vehicles)
        {
            var startOdometer = await GetOdometerReadingAsync(vehicleId, billingReq.StartDate);
            var endOdometer = await GetOdometerReadingAsync(vehicleId, billingReq.EndDate);

            var milesTravelled = endOdometer - startOdometer;
            if (milesTravelled < 0)
            {
                milesTravelled = 0;
            }

            totalMiles += milesTravelled;
        }

        var totalBill = totalMiles * _costPerMile;

        return Math.Round(totalBill, 2);
    }

    private async Task<decimal> GetOdometerReadingAsync(string vehicleId, DateTime timestamp)
    {
        var url = $"{options.Value.readingUrl}{timestamp:o}";

        var response = await httpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("Failed to fetch odometer reading for vehicle {VehicleId} at {Timestamp}", vehicleId, timestamp);
            throw new ApplicationException($"Failed to get odometer reading for {vehicleId}");
        }

        var odometerResponses = await response.Content.ReadFromJsonAsync<List<OdometerResponse>>();
        var odometerResponse = odometerResponses?.FirstOrDefault(o => o.LicensePlate == vehicleId);
        if (odometerResponse == null)
        {
            logger.LogWarning("No odometer data found for vehicle {VehicleId} at {Timestamp}", vehicleId, timestamp);
            return 0;
        }
        var odometerInMiles = odometerResponse.State.OdometerInMeters / 1609.34m;

        return odometerInMiles;
    }
}
