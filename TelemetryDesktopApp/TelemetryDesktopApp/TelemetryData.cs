using System;

namespace TelemetryDesktopApp;

public class TelemetryData
{
    public double? Value { get; set; }
    public string DeviceId { get; set; } = "";
    public DateTime? MeasuredAtUTC { get; set; }
    public string Unit { get; set; } = "";
    public string Status { get; set; } = "";
}
