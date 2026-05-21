using Core.Interfaces;
using Core.Models;
using FluentModbus;
using Serilog;

namespace ModbusDriver;

public class ModbusConnector : IIndustrialConnector
{
    private readonly DeviceConfig _config;
    private ModbusTcpClient? _client;
    private Task? _scanTask;
    private CancellationTokenSource? _scanCts;
    private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

    public string DeviceId => _config.Id;
    public bool IsConnected => _client?.IsConnected ?? false;

    public event EventHandler<bool>? ConnectionStateChanged;
    public event EventHandler<DataReceivedEventArgs>? DataReceived;

    public ModbusConnector(DeviceConfig config)
    {
        _config = config;
    }

    public async Task<bool> ConnectAsync(CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            Log.Information("Connecting to Modbus device at {Ip}:{Port}", _config.IpAddress, _config.Port);
            
            _client = new ModbusTcpClient();
            _client.Connect(_config.IpAddress, ModbusEndianness.BigEndian);
            
            if (_client.IsConnected)
            {
                ConnectionStateChanged?.Invoke(this, true);
                Log.Information("Connected to Modbus device successfully");
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to connect to Modbus device");
            return false;
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
            if (_client != null)
            {
                _client.Disconnect();
                Log.Information("Disconnected from Modbus device");
            }
            ConnectionStateChanged?.Invoke(this, false);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<object?> ReadAsync(DataPointConfig dataPoint, CancellationToken ct = default)
    {
        if (_client == null || !_client.IsConnected)
            return null;

        try
        {
            var address = int.Parse(dataPoint.Address);
            object result = dataPoint.DataType switch
            {
                DataType.Int16 => ReadInt16(address),
                DataType.UInt16 => ReadUInt16(address),
                DataType.Int32 => ReadInt32(address),
                DataType.UInt32 => ReadUInt32(address),
                DataType.Float => ReadFloat(address),
                DataType.Bool => ReadBool(address),
                _ => null
            };
            return result;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error reading from Modbus address {Address}", dataPoint.Address);
            return null;
        }
    }

    public async Task<List<DataPointValue>> ReadAllAsync(List<DataPointConfig> dataPoints, CancellationToken ct = default)
    {
        var values = new List<DataPointValue>();
        
        foreach (var dp in dataPoints)
        {
            var value = await ReadAsync(dp, ct);
            values.Add(new DataPointValue
            {
                DataPointId = dp.Id,
                DataPointName = dp.Name,
                Value = value,
                Unit = dp.Unit,
                IsValid = value != null,
                Timestamp = DateTime.Now
            });
        }

        return values;
    }

    public async Task WriteAsync(DataPointConfig dataPoint, object value, CancellationToken ct = default)
    {
        if (_client == null || !_client.IsConnected)
            return;

        await _lock.WaitAsync(ct);
        try
        {
            var address = int.Parse(dataPoint.Address);
            
            switch (dataPoint.DataType)
            {
                case DataType.Int16:
                    WriteInt16(address, Convert.ToInt16(value));
                    break;
                case DataType.UInt16:
                    WriteUInt16(address, Convert.ToUInt16(value));
                    break;
                case DataType.Int32:
                    WriteInt32(address, Convert.ToInt32(value));
                    break;
                case DataType.Float:
                    WriteFloat(address, Convert.ToSingle(value));
                    break;
                case DataType.Bool:
                    WriteBool(address, Convert.ToBoolean(value));
                    break;
            }
            
            Log.Information("Wrote to Modbus address {Address}: {Value}", dataPoint.Address, value);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task StartScanAsync(List<DataPointConfig> dataPoints, int intervalMs, CancellationToken ct = default)
    {
        if (_scanTask != null) return;

        _scanCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _scanTask = Task.Run(async () =>
        {
            while (!_scanCts.Token.IsCancellationRequested)
            {
                try
                {
                    var values = await ReadAllAsync(dataPoints, _scanCts.Token);
                    DataReceived?.Invoke(this, new DataReceivedEventArgs { Values = values, DeviceId = _config.Id });
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error during scan");
                }
                
                await Task.Delay(intervalMs, _scanCts.Token);
            }
        }, _scanCts.Token);
    }

    public async Task StopScanAsync()
    {
        if (_scanCts != null)
        {
            _scanCts.Cancel();
            if (_scanTask != null)
                await _scanTask;
            _scanCts.Dispose();
            _scanCts = null;
            _scanTask = null;
        }
    }

    private short ReadInt16(int address)
    {
        if (_client == null) return 0;
        var result = _client.ReadHoldingRegisters<short>(_config.SlaveId, address, 1);
        return result.Length > 0 ? result[0] : (short)0;
    }

    private ushort ReadUInt16(int address)
    {
        if (_client == null) return 0;
        var result = _client.ReadHoldingRegisters<ushort>(_config.SlaveId, address, 1);
        return result.Length > 0 ? result[0] : (ushort)0;
    }

    private int ReadInt32(int address)
    {
        if (_client == null) return 0;
        var result = _client.ReadHoldingRegisters<int>(_config.SlaveId, address, 1);
        return result.Length > 0 ? result[0] : 0;
    }

    private uint ReadUInt32(int address)
    {
        if (_client == null) return 0;
        var result = _client.ReadHoldingRegisters<uint>(_config.SlaveId, address, 1);
        return result.Length > 0 ? result[0] : 0;
    }

    private float ReadFloat(int address)
    {
        if (_client == null) return 0;
        var result = _client.ReadHoldingRegisters<float>(_config.SlaveId, address, 1);
        return result.Length > 0 ? result[0] : 0;
    }

    private bool ReadBool(int address)
    {
        if (_client == null) return false;
        var result = _client.ReadCoils(_config.SlaveId, address, 1);
        return result.Length > 0 && result[0] != 0;
    }

    private void WriteInt16(int address, short value)
    {
        _client?.WriteSingleRegister(_config.SlaveId, address, value);
    }

    private void WriteUInt16(int address, ushort value)
    {
        _client?.WriteSingleRegister(_config.SlaveId, address, value);
    }

    private void WriteInt32(int address, int value)
    {
        _client?.WriteMultipleRegisters(_config.SlaveId, address, BitConverter.GetBytes(value));
    }

    private void WriteFloat(int address, float value)
    {
        _client?.WriteMultipleRegisters(_config.SlaveId, address, BitConverter.GetBytes(value));
    }

    private void WriteBool(int address, bool value)
    {
        _client?.WriteSingleCoil(_config.SlaveId, address, value);
    }

    public void Dispose()
    {
        StopScanAsync().Wait();
        DisconnectAsync().Wait();
        _lock.Dispose();
    }
}
