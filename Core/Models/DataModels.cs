namespace Core.Models;

public class DataRecord
{
    public long Id { get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string DataPointId { get; set; } = string.Empty;
    public string DataPointName { get; set; } = string.Empty;
    public double? RawValue { get; set; }
    public double? ScaledValue { get; set; }
    public string? Unit { get; set; }
    public bool IsValid { get; set; }
    public DateTime RecordTime { get; set; } = DateTime.Now;
    public bool IsUploaded { get; set; }
}

public class UploadConfig
{
    public string ApiEndpoint { get; set; } = string.Empty;
    public string? ApiKey { get; set; }
    public int BatchSize { get; set; } = 50;
    public int UploadIntervalMs { get; set; } = 30000;
    public bool AutoUpload { get; set; }
}
