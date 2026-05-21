using Core.Models;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Serilog;

namespace Core.Services;

public class DataUploadService
{
    private readonly HttpClient _httpClient;
    private readonly UploadConfig _config;

    public DataUploadService(UploadConfig config)
    {
        _config = config;
        _httpClient = new HttpClient();
        if (!string.IsNullOrEmpty(config.ApiKey))
        {
            _httpClient.DefaultRequestHeaders.Add("X-API-Key", config.ApiKey);
        }
    }

    public async Task<bool> UploadRecordsAsync(IEnumerable<DataRecord> records)
    {
        if (string.IsNullOrEmpty(_config.ApiEndpoint))
        {
            Log.Warning("Upload endpoint not configured");
            return false;
        }

        try
        {
            var json = JsonSerializer.Serialize(new {
                timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                records = records.Select(r => new {
                    deviceId = r.DeviceId,
                    deviceName = r.DeviceName,
                    dataPointId = r.DataPointId,
                    dataPointName = r.DataPointName,
                    rawValue = r.RawValue,
                    scaledValue = r.ScaledValue,
                    unit = r.Unit,
                    recordTime = r.RecordTime.ToString("yyyy-MM-dd HH:mm:ss.fff")
                }).ToList()
            });

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(_config.ApiEndpoint, content);
            
            if (response.IsSuccessStatusCode)
            {
                Log.Information("Successfully uploaded {Count} records", records.Count());
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Log.Error("Failed to upload records: {StatusCode} - {Content}", 
                    response.StatusCode, errorContent);
                return false;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error uploading records");
            return false;
        }
    }

    public async Task<bool> TestConnectionAsync()
    {
        if (string.IsNullOrEmpty(_config.ApiEndpoint))
            return false;

        try
        {
            var response = await _httpClient.GetAsync(_config.ApiEndpoint);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
