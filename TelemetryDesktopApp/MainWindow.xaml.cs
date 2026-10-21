
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace TelemetryDesktopApp;

public sealed partial class MainWindow : Window
{
    private readonly TelemetryApiClient _client = new();
    private readonly ObservableCollection<string> _sensors = new();

    private readonly DispatcherTimer _timer = new()
    {
        Interval = TimeSpan.FromSeconds(5)
    };

    private bool _isLoading;

    private const string ApiUrl =
        "http://127.0.0.1:8000/api/devices";

    private static readonly SolidColorBrush OnlineBrush =
        new(Colors.Green);

    private static readonly SolidColorBrush OfflineBrush =
        new(Colors.Red);

    private static readonly SolidColorBrush NeutralBrush =
        new(Colors.Gray);

    // Senaste mätning per enhet och mätenhet.
    private readonly Dictionary<string, DateTime> _lastUpdated = new();

    public MainWindow()
    {
        InitializeComponent();

        SensorList.ItemsSource = _sensors;

        _timer.Tick += async (_, _) => await ApiAsync();
        _timer.Start();

        _ = ApiAsync();

        Closed += (_, _) => _timer.Stop();
    }

    private async Task ApiAsync()
    {
        if (_isLoading)
            return;

        _isLoading = true;

        try
        {
            var sensors = (await _client.GetTelemetryAsync(ApiUrl)).ToList();

            ConnectionStatusText.Text = "API anslutet";

            // Beräkna genomsnittet från senaste giltiga värdet
            // för varje enhet och mätenhet.
            var latestTemperatures = sensors
                .Where(s => (s.Unit == "°C" || s.Unit == "C") && s.Value.HasValue)
                .GroupBy(s => s.DeviceId)
                .Select(g => g
                    .OrderByDescending(s => s.MeasuredAtUTC)
                    .First());

            var latestHumidities = sensors
                .Where(s => s.Unit == "%" && s.Value.HasValue)
                .GroupBy(s => s.DeviceId)
                .Select(g => g
                    .OrderByDescending(s => s.MeasuredAtUTC)
                    .First());

            var temperatures = latestTemperatures
                .Select(s => s.Value!.Value)
                .ToList();

            var humidities = latestHumidities
                .Select(s => s.Value!.Value)
                .ToList();

            TemperatureValueText.Text = temperatures.Count > 0
                ? $"{temperatures.Average():0.0} °C"
                : "-- °C";

            HumidityValueText.Text = humidities.Count > 0
                ? $"{humidities.Average():0.0} %"
                : "-- %";

            var deviceIds = sensors
                .Where(s => !string.IsNullOrWhiteSpace(s.DeviceId))
                .Select(s => s.DeviceId)
                .Distinct()
                .OrderBy(id => id)
                .Take(4)
                .ToArray();

            Esp1DeviceIdText.Text = deviceIds.ElementAtOrDefault(0) ?? null;
            Esp2DeviceIdText.Text = deviceIds.ElementAtOrDefault(1) ?? null;
            Esp3DeviceIdText.Text = deviceIds.ElementAtOrDefault(2) ?? null;
            Esp4DeviceIdText.Text = deviceIds.ElementAtOrDefault(3) ?? null;

            UpdateDevice(
                sensors,
                deviceIds.ElementAtOrDefault(0),
                NanoStatusText,
                NanoTemperatureText,
                NanoHumidityText,
                NanoUpdatedText);

            UpdateDevice(
                sensors,
                deviceIds.ElementAtOrDefault(1),
                S3StatusText,
                S3TemperatureText,
                S3HumidityText,
                S3UpdatedText);

            UpdateDevice(
                sensors,
                deviceIds.ElementAtOrDefault(2),
                Esp3StatusText,
                Esp3TemperatureText,
                Esp3HumidityText,
                Esp3UpdatedText);

            UpdateDevice(
                sensors,
                deviceIds.ElementAtOrDefault(3),
                Esp4StatusText,
                Esp4TemperatureText,
                Esp4HumidityText,
                Esp4UpdatedText);

            // Sensorlista
            var rows = sensors
                .OrderBy(s => s.DeviceId)
                .ThenBy(s => s.Unit)
                .Select(s =>
                    $"{s.DeviceId} | {FormatValue(s, s.Unit)} | {s.Status}")
                .ToList();

            while (_sensors.Count > rows.Count)
                _sensors.RemoveAt(_sensors.Count - 1);

            for (int i = 0; i < rows.Count; i++)
            {
                if (i >= _sensors.Count)
                    _sensors.Add(rows[i]);
                else if (_sensors[i] != rows[i])
                    _sensors[i] = rows[i];
            }
        }
        catch (Exception ex)
        {
            ConnectionStatusText.Text = "API-fel";

            System.Diagnostics.Debug.WriteLine(ex.ToString());
        }
        finally
        {
            _isLoading = false;
        }
    }

    private static string FormatValue(
        TelemetryData? data,
        string unit)
    {
        return data?.Value is double value
            ? $"{value:0.0} {unit}"
            : $"-- {unit}";
    }

    private void UpdateDevice(
        IEnumerable<TelemetryData> sensors,
        string? deviceId,
        TextBlock statusText,
        TextBlock temperatureText,
        TextBlock humidityText,
        TextBlock updatedText)
    {
        var deviceData = string.IsNullOrWhiteSpace(deviceId)
            ? new List<TelemetryData>()
            : sensors
                .Where(s => s.DeviceId == deviceId)
                .ToList();

        if (deviceData.Count == 0)
        {
            statusText.Text = "Ingen data";
            statusText.Foreground = NeutralBrush;

            temperatureText.Text = "-- °C";
            humidityText.Text = "-- %";
            updatedText.Text = "Updated --";

            return;
        }

        var temperature = deviceData
            .Where(s => s.Unit == "°C" || s.Unit == "C")
            .OrderByDescending(s => s.MeasuredAtUTC)
            .FirstOrDefault();

        var humidity = deviceData
            .Where(s => s.Unit == "%")
            .OrderByDescending(s => s.MeasuredAtUTC)
            .FirstOrDefault();

        temperatureText.Text = FormatValue(temperature, "°C");
        humidityText.Text = FormatValue(humidity, "%");

        var latest = deviceData
            .OrderByDescending(s => s.MeasuredAtUTC)
            .First();

        statusText.Text = latest.Status;

        // Statusfärg baserad på API-statusen.
        string status = latest.Status ?? "";

        if (status.Contains("offline", StringComparison.OrdinalIgnoreCase)
            || status.Contains("disconnected", StringComparison.OrdinalIgnoreCase)
            || status.Contains("error", StringComparison.OrdinalIgnoreCase))
        {
            statusText.Foreground = OfflineBrush;
        }
        else if (status.Contains("online", StringComparison.OrdinalIgnoreCase)
                 || status.Contains("connected", StringComparison.OrdinalIgnoreCase)
                 || status.Equals("OK", StringComparison.OrdinalIgnoreCase))
        {
            statusText.Foreground = OnlineBrush;
        }
        else
        {
            statusText.Foreground = NeutralBrush;
        }

        // Visa relativ tid baserad på senaste mätningen.
        if (latest.MeasuredAtUTC.HasValue)
        {
            DateTime measuredAt = latest.MeasuredAtUTC.Value;

            if (measuredAt.Kind == DateTimeKind.Unspecified)
                measuredAt = DateTime.SpecifyKind(
                    measuredAt, DateTimeKind.Utc);

            measuredAt = measuredAt.ToUniversalTime();

            _lastUpdated[deviceId!] = measuredAt;

            TimeSpan elapsed = DateTime.UtcNow - measuredAt;

            if (elapsed < TimeSpan.Zero)
                elapsed = TimeSpan.Zero;

            updatedText.Text = FormatElapsed(elapsed);
        }
        else
        {
            updatedText.Text = "Updated: time unavailable";
        }
    }

    private static string FormatElapsed(TimeSpan elapsed)
    {
        if (elapsed.TotalSeconds < 60)
        {
            int seconds = Math.Max(0, (int)elapsed.TotalSeconds);

            return $"Updated {seconds} seconds ago";
        }

        if (elapsed.TotalMinutes < 60)
        {
            int minutes = (int)elapsed.TotalMinutes;

            return $"Updated {minutes} minutes ago";
        }

        int hours = (int)elapsed.TotalHours;

        if (hours < 24)
            return $"Updated {hours} hours ago";

        int days = (int)elapsed.TotalDays;

        return $"Updated {days} days ago";
    }
}
