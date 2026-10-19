
using Microsoft.UI.Xaml;
using System;
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

    private bool _isLoading = false;

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
        if (_isLoading) return;

        _isLoading = true;

        try
        {
            var sensors = await _client.GetTelemetryAsync(
                "http://127.0.0.1:8000/api/devices");

            var rows = sensors.Select(s =>
                $"{s.DeviceId} | {s.Value:0.0} {s.Unit} | {s.Status}"
            ).ToList();

            while (_sensors.Count > rows.Count)
                _sensors.RemoveAt(_sensors.Count - 1);

            for (int i = 0; i < rows.Count; i++)
            {
                if (i >= _sensors.Count)
                {
                    _sensors.Add(rows[i]);
                }
                else if (_sensors[i] != rows[i])
                {
                    _sensors[i] = rows[i];
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex.Message);
        }
        finally
        {
            _isLoading = false;
        }
    }
}
