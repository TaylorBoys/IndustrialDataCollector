using Core.Interfaces;
using Core.Models;
using Opc.Ua;
using Opc.Ua.Client;
using Serilog;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace OpcUaDriver;

public class OpcUaConnector : IIndustrialConnector
{
    private readonly DeviceConfig _config;
    private Session? _session;
    private Task? _scanTask;
    private CancellationTokenSource? _scanCts;
    private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

    public string DeviceId => _config.Id;
    public bool IsConnected => _session?.Connected ?? false;

    public event EventHandler<bool>? ConnectionStateChanged;
    public event EventHandler<DataReceivedEventArgs>? DataReceived;

    public OpcUaConnector(DeviceConfig config)
    {
        _config = config;
    }

    public async Task<bool> ConnectAsync(CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            Log.Information("Connecting to OPC UA endpoint: {Endpoint}", _config.OpcUaEndpoint);

            var config = new ApplicationConfiguration
            {
                ApplicationName = "IndustrialDataCollector",
                ApplicationType = ApplicationType.Client,
                ApplicationUri = "urn:IndustrialDataCollector",
                ProductUri = "http://IndustrialDataCollector/OpcUaDriver",
                SecurityConfiguration = new SecurityConfiguration
                {
                    ApplicationCertificate = new CertificateIdentifier
                    {
                        StoreType = CertificateStoreType.Directory,
                        StorePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "IndustrialDataCollector", "pki", "own"),
                        SubjectName = "CN=IndustrialDataCollector"
                    },
                    TrustedIssuerCertificates = new CertificateTrustList
                    {
                        StoreType = CertificateStoreType.Directory,
                        StorePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "IndustrialDataCollector", "pki", "issuers")
                    },
                    TrustedPeerCertificates = new CertificateTrustList
                    {
                        StoreType = CertificateStoreType.Directory,
                        StorePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "IndustrialDataCollector", "pki", "trusted")
                    },
                    RejectedCertificateStore = new CertificateStoreIdentifier
                    {
                        StoreType = CertificateStoreType.Directory,
                        StorePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "IndustrialDataCollector", "pki", "rejected")
                    },
                    AutoAcceptUntrustedCertificates = true,
                    RejectSHA1SignedCertificates = false,
                    MinimumCertificateKeySize = 1024
                },
                TransportConfigurations = new TransportConfigurationCollection(),
                TransportQuotas = new TransportQuotas
                {
                    OperationTimeout = 15000,
                    MaxStringLength = 1048576,
                    MaxByteStringLength = 1048576,
                    MaxArrayLength = 65535,
                    MaxMessageSize = 4194304,
                    MaxBufferSize = 65535,
                    ChannelLifetime = 300000,
                    SecurityTokenLifetime = 3600000
                },
                ClientConfiguration = new ClientConfiguration
                {
                    DefaultSessionTimeout = 60000,
                    MinSubscriptionLifetime = 10000
                }
            };

            config.Validate(ApplicationType.Client);

            var endpointConfiguration = EndpointConfiguration.Create(config);
            var endpoint = new ConfiguredEndpoint(null, new EndpointDescription(_config.OpcUaEndpoint), endpointConfiguration);

            _session = await Session.Create(
                config,
                endpoint,
                false,
                "IndustrialDataCollector Session",
                60000,
                new UserIdentity(new AnonymousIdentityToken()),
                null
            );

            if (_session.Connected)
            {
                ConnectionStateChanged?.Invoke(this, true);
                Log.Information("Connected to OPC UA server successfully");
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to connect to OPC UA server");
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
            if (_session != null)
            {
                _session.Close();
                _session.Dispose();
                _session = null;
                Log.Information("Disconnected from OPC UA server");
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
        if (_session == null || !_session.Connected)
            return null;

        try
        {
            var nodeId = new NodeId(dataPoint.Address);
            var value = _session.ReadValue(nodeId);
            return ConvertValue(value.Value, dataPoint.DataType);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error reading OPC UA node {Node}", dataPoint.Address);
            return null;
        }
    }

    public async Task<List<DataPointValue>> ReadAllAsync(List<DataPointConfig> dataPoints, CancellationToken ct = default)
    {
        var values = new List<DataPointValue>();

        if (_session == null || !_session.Connected)
            return values;

        try
        {
            var nodesToRead = new ReadValueIdCollection();
            foreach (var dp in dataPoints)
            {
                nodesToRead.Add(new ReadValueId
                {
                    NodeId = new NodeId(dp.Address),
                    AttributeId = Attributes.Value
                });
            }

            var results = _session.Read(
                null,
                0,
                TimestampsToReturn.Neither,
                nodesToRead,
                out var resultsOut,
                out var diagnosticInfosOut
            );

            for (int i = 0; i < dataPoints.Count; i++)
            {
                var dataPoint = dataPoints[i];
                var result = resultsOut[i];
                
                values.Add(new DataPointValue
                {
                    DataPointId = dataPoint.Id,
                    DataPointName = dataPoint.Name,
                    Value = result.StatusCode == StatusCodes.Good ? ConvertValue(result.Value, dataPoint.DataType) : null,
                    Unit = dataPoint.Unit,
                    IsValid = result.StatusCode == StatusCodes.Good,
                    Timestamp = DateTime.Now
                });
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error reading multiple OPC UA nodes");
        }

        return values;
    }

    public async Task WriteAsync(DataPointConfig dataPoint, object value, CancellationToken ct = default)
    {
        if (_session == null || !_session.Connected)
            return;

        await _lock.WaitAsync(ct);
        try
        {
            var nodeId = new NodeId(dataPoint.Address);
            var valueToWrite = ConvertToVariant(value, dataPoint.DataType);
            
            var writeValue = new WriteValue
            {
                NodeId = nodeId,
                AttributeId = Attributes.Value,
                Value = new DataValue(valueToWrite)
            };

            var writeValues = new WriteValueCollection { writeValue };
            
            _session.Write(
                null,
                writeValues,
                out var resultsOut,
                out var diagnosticInfosOut
            );

            if (resultsOut[0] == StatusCodes.Good)
            {
                Log.Information("Wrote to OPC UA node {Node}: {Value}", dataPoint.Address, value);
            }
            else
            {
                Log.Warning("Failed to write to OPC UA node {Node}: {Status}", dataPoint.Address, resultsOut[0]);
            }
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
                    Log.Error(ex, "Error during OPC UA scan");
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

    private static object? ConvertValue(object? value, DataType dataType)
    {
        if (value == null) return null;

        try
        {
            return dataType switch
            {
                DataType.Bool => Convert.ToBoolean(value),
                DataType.Int16 => Convert.ToInt16(value),
                DataType.UInt16 => Convert.ToUInt16(value),
                DataType.Int32 => Convert.ToInt32(value),
                DataType.UInt32 => Convert.ToUInt32(value),
                DataType.Float => Convert.ToSingle(value),
                DataType.Double => Convert.ToDouble(value),
                DataType.String => value.ToString(),
                _ => value
            };
        }
        catch
        {
            return value;
        }
    }

    private static Variant ConvertToVariant(object value, DataType dataType)
    {
        return dataType switch
        {
            DataType.Bool => new Variant(Convert.ToBoolean(value)),
            DataType.Int16 => new Variant(Convert.ToInt16(value)),
            DataType.UInt16 => new Variant(Convert.ToUInt16(value)),
            DataType.Int32 => new Variant(Convert.ToInt32(value)),
            DataType.UInt32 => new Variant(Convert.ToUInt32(value)),
            DataType.Float => new Variant(Convert.ToSingle(value)),
            DataType.Double => new Variant(Convert.ToDouble(value)),
            DataType.String => new Variant(value.ToString()),
            _ => new Variant(value)
        };
    }

    public void Dispose()
    {
        StopScanAsync().Wait();
        DisconnectAsync().Wait();
        _lock.Dispose();
    }
}
