using Core.Interfaces;
using Core.Models;
using Serilog;

namespace Core.Services;

public class DeviceManager : IDisposable
{
    private static readonly Lazy<DeviceManager> _instance = new(() => new DeviceManager());
    public static DeviceManager Instance => _instance.Value;

    private readonly Dictionary<string, DeviceWorker> _workers;
    private readonly IConnectorFactory _factory;
    private readonly DataProcessingService _dataService;
    private readonly DataStorageService? _storageService;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public event EventHandler<DeviceWorker>? DeviceAdded;
    public event EventHandler<string>? DeviceRemoved;
    public event EventHandler<(string DeviceId, List<DataPointValue> Values)>? DeviceDataReceived;

    private DeviceManager()
    {
        _workers = new Dictionary<string, DeviceWorker>();
        _factory = new ConnectorFactory();
        _dataService = new DataProcessingService();
        _storageService = new DataStorageService();
    }

    public async Task AddDeviceAsync(DeviceConfig config)
    {
        await _lock.WaitAsync();
        try
        {
            if (_workers.ContainsKey(config.Id))
            {
                Log.Warning("Device already exists: {DeviceId}", config.Id);
                return;
            }

            var connector = _factory.CreateConnector(config);
            var worker = new DeviceWorker(config, connector, _dataService, _storageService);
            
            worker.DataReceived += (s, e) => OnDeviceDataReceived(config.Id, e);
            worker.ConnectionStateChanged += (s, e) => Log.Information(
                "Device {DeviceName} state: {State}", config.Name, e ? "Connected" : "Disconnected");

            _workers[config.Id] = worker;
            DeviceAdded?.Invoke(this, worker);
            
            Log.Information("Device added: {DeviceId} - {DeviceName}", config.Id, config.Name);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task RemoveDeviceAsync(string deviceId)
    {
        await _lock.WaitAsync();
        try
        {
            if (_workers.TryGetValue(deviceId, out var worker))
            {
                await worker.StopScanAsync();
                await worker.DisconnectAsync();
                worker.Dispose();
                _workers.Remove(deviceId);
                DeviceRemoved?.Invoke(this, deviceId);
                Log.Information("Device removed: {DeviceId}", deviceId);
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    public DeviceWorker? GetDevice(string deviceId)
    {
        _workers.TryGetValue(deviceId, out var worker);
        return worker;
    }

    public List<DeviceWorker> GetAllDevices()
    {
        return _workers.Values.ToList();
    }

    public async Task StartAllDevicesAsync()
    {
        foreach (var worker in _workers.Values)
        {
            if (!worker.IsConnected)
            {
                await worker.ConnectAsync();
            }
            await worker.StartScanAsync();
        }
    }

    public async Task StopAllDevicesAsync()
    {
        foreach (var worker in _workers.Values)
        {
            await worker.StopScanAsync();
            await worker.DisconnectAsync();
        }
    }

    public DataStorageService? GetStorageService() => _storageService;
    public DataProcessingService GetDataService() => _dataService;

    private void OnDeviceDataReceived(string deviceId, List<DataPointValue> values)
    {
        DeviceDataReceived?.Invoke(this, (deviceId, values));
    }

    public void Dispose()
    {
        StopAllDevicesAsync().Wait();
        foreach (var worker in _workers.Values)
        {
            worker.Dispose();
        }
        _lock.Dispose();
    }
}
