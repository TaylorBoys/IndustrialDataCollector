using Core.Interfaces;
using Core.Models;
using Serilog;

namespace Core.Services;

public class DeviceWorker : IDisposable
{
    private readonly DeviceConfig _config;
    private readonly IIndustrialConnector _connector;
    private readonly DataProcessingService _dataService;
    private readonly DataStorageService? _storageService;
    
    private CancellationTokenSource? _scanCts;
    private readonly SemaphoreSlim _lock = new(1, 1);
    
    public event EventHandler<List<DataPointValue>>? DataReceived;
    public event EventHandler<bool>? ConnectionStateChanged;
    
    public bool IsConnected => _connector.IsConnected;
    public string DeviceId => _config.Id;
    public string DeviceName => _config.Name;

    public DeviceWorker(
        DeviceConfig config,
        IIndustrialConnector connector,
        DataProcessingService dataService,
        DataStorageService? storageService = null)
    {
        _config = config;
        _connector = connector;
        _dataService = dataService;
        _storageService = storageService;
        
        _connector.DataReceived += OnConnectorDataReceived;
        _connector.ConnectionStateChanged += OnConnectorConnectionStateChanged;
    }

    public async Task<bool> ConnectAsync()
    {
        await _lock.WaitAsync();
        try
        {
            Log.Information("Connecting to device: {DeviceName}", _config.Name);
            return await _connector.ConnectAsync();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task DisconnectAsync()
    {
        await _lock.WaitAsync();
        try
        {
            await StopScanAsync();
            Log.Information("Disconnecting from device: {DeviceName}", _config.Name);
            await _connector.DisconnectAsync();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task StartScanAsync()
    {
        if (_scanCts != null) return;
        
        await _lock.WaitAsync();
        try
        {
            _scanCts = new CancellationTokenSource();
            Log.Information("Starting scan for device: {DeviceName}, interval: {Interval}ms", 
                _config.Name, _config.ScanIntervalMs);
            
            await _connector.StartScanAsync(
                _config.DataPoints, 
                _config.ScanIntervalMs, 
                _scanCts.Token);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task StopScanAsync()
    {
        if (_scanCts == null) return;
        
        await _lock.WaitAsync();
        try
        {
            Log.Information("Stopping scan for device: {DeviceName}", _config.Name);
            _scanCts.Cancel();
            await _connector.StopScanAsync();
            _scanCts.Dispose();
            _scanCts = null;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<object?> ReadAsync(DataPointConfig dataPoint)
    {
        return await _connector.ReadAsync(dataPoint);
    }

    public async Task WriteAsync(DataPointConfig dataPoint, object value)
    {
        await _lock.WaitAsync();
        try
        {
            Log.Information("Writing to {DataPoint} on {DeviceName}: {Value}", 
                dataPoint.Name, _config.Name, value);
            await _connector.WriteAsync(dataPoint, value);
        }
        finally
        {
            _lock.Release();
        }
    }

    private void OnConnectorDataReceived(object? sender, DataReceivedEventArgs e)
    {
        var processedValues = e.Values
            .Select(v => _dataService.ProcessDataPoint(
                _config.DataPoints.First(dp => dp.Id == v.DataPointId),
                v.Value))
            .ToList();

        if (_storageService != null)
        {
            foreach (var value in processedValues)
            {
                var dataPoint = _config.DataPoints.First(dp => dp.Id == value.DataPointId);
                var record = _dataService.CreateDataRecord(_config, dataPoint, value);
                _ = _storageService.InsertRecordAsync(record);
            }
        }

        DataReceived?.Invoke(this, processedValues);
    }

    private void OnConnectorConnectionStateChanged(object? sender, bool isConnected)
    {
        Log.Information("Device {DeviceName} connection state: {State}", 
            _config.Name, isConnected ? "Connected" : "Disconnected");
        ConnectionStateChanged?.Invoke(this, isConnected);
    }

    public void Dispose()
    {
        StopScanAsync().Wait();
        DisconnectAsync().Wait();
        _connector.Dispose();
        _scanCts?.Dispose();
        _lock.Dispose();
    }
}
