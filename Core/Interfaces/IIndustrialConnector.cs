using Core.Models;

namespace Core.Interfaces;

public class DataReceivedEventArgs : EventArgs
{
    public List<DataPointValue> Values { get; set; } = new();
    public string DeviceId { get; set; } = string.Empty;
}

public interface IIndustrialConnector : IDisposable
{
    string DeviceId { get; }
    bool IsConnected { get; }
    
    event EventHandler<bool>? ConnectionStateChanged;
    event EventHandler<DataReceivedEventArgs>? DataReceived;
    
    Task<bool> ConnectAsync(CancellationToken ct = default);
    Task DisconnectAsync();
    Task<object?> ReadAsync(DataPointConfig dataPoint, CancellationToken ct = default);
    Task<List<DataPointValue>> ReadAllAsync(List<DataPointConfig> dataPoints, CancellationToken ct = default);
    Task WriteAsync(DataPointConfig dataPoint, object value, CancellationToken ct = default);
    Task StartScanAsync(List<DataPointConfig> dataPoints, int intervalMs, CancellationToken ct = default);
    Task StopScanAsync();
}
