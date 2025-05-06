namespace Zeti.Fleet.Billing.Model;

public class OdometerResponse
{
    public string Vin { get; set; }
    public string LicensePlate { get; set; }
    public string Make { get; set; }
    public string Model { get; set; }
    public VehicleState State { get; set; }
}

public class VehicleState
{
    public decimal OdometerInMeters { get; set; }
    public decimal SpeedInMph { get; set; }
    public DateTime AsAt { get; set; }
}