
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
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
        "http://10.50.247.132:8000/api/devices";

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
            var sensors = await _client.GetTelemetryAsync(ApiUrl);

            ConnectionStatusText.Text = "API anslutet";

            // Översikt: senaste temperatur och luftfuktighet.
            var temperature = sensors
                .Where(s => s.Unit == "°C")
                .OrderByDescending(s => s.MeasuredAtUTC)
                .FirstOrDefault();

            var humidity = sensors
                .Where(s => s.Unit == "%")
                .OrderByDescending(s => s.MeasuredAtUTC)
                .FirstOrDefault();

            TemperatureValueText.Text =
                FormatValue(temperature, "°C");

            HumidityValueText.Text =
                FormatValue(humidity, "%");

            // ESP32 #1
            UpdateDevice(
                sensors,
                "qurat-esp32-01",
                NanoStatusText,
                NanoTemperatureText,
                NanoHumidityText,
                NanoUpdatedText);

            // ESP32 #2
            UpdateDevice(
                sensors,
                "qurat-esp32-s3-02",
                S3StatusText,
                S3TemperatureText,
                S3HumidityText,
                S3UpdatedText);

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

    private static void UpdateDevice(
        IEnumerable<TelemetryData> sensors,
        string deviceId,
        TextBlock statusText,
        TextBlock temperatureText,
        TextBlock humidityText,
        TextBlock updatedText)
    {
        var deviceData = sensors
            .Where(s => s.DeviceId == deviceId)
            .ToList();

        if (deviceData.Count == 0)
        {
            statusText.Text = "Ingen data";
            temperatureText.Text = "-- °C";
            humidityText.Text = "-- %";
            updatedText.Text = "Ingen uppdatering ännu";
            return;
        }

        var temperature = deviceData
            .Where(s => s.Unit == "°C")
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

        updatedText.Text = latest.MeasuredAtUTC.HasValue
            ? "Senast uppdaterad: " +
              latest.MeasuredAtUTC.Value
                  .ToLocalTime()
                  .ToString("HH:mm:ss")
            : "Tid saknas";
    }
}
