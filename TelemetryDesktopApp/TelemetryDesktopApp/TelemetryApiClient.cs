
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace TelemetryDesktopApp;

public class TelemetryApiClient
{
    private readonly HttpClient _httpClient = new();

    public async Task<List<TelemetryData>> GetTelemetryAsync(string url)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<TelemetryData>>(url)
                   ?? new List<TelemetryData>();
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"API connection failed: {ex.Message}", ex);
        }
    }
}