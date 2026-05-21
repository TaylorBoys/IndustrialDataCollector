using Core.Interfaces;
using Core.Models;
using Serilog;

namespace Core.Services;

public class ConnectorFactory : IConnectorFactory
{
    public IIndustrialConnector CreateConnector(DeviceConfig config)
    {
        Log.Information("Creating connector for device: {DeviceName}, Protocol: {Protocol}", config.Name, config.Protocol);
        
        return config.Protocol switch
        {
            ProtocolType.ModbusTcp or ProtocolType.ModbusRtu => CreateModbusConnector(config),
            ProtocolType.OpcUa => CreateOpcUaConnector(config),
            _ => throw new ArgumentOutOfRangeException(nameof(config.Protocol))
        };
    }
    
    private IIndustrialConnector CreateModbusConnector(DeviceConfig config)
    {
        var type = Type.GetType("ModbusDriver.ModbusConnector, ModbusDriver");
        if (type == null)
        {
            throw new InvalidOperationException("ModbusDriver assembly not found");
        }
        
        return (IIndustrialConnector)Activator.CreateInstance(type, config)!;
    }
    
    private IIndustrialConnector CreateOpcUaConnector(DeviceConfig config)
    {
        var type = Type.GetType("OpcUaDriver.OpcUaConnector, OpcUaDriver");
        if (type == null)
        {
            throw new InvalidOperationException("OpcUaDriver assembly not found");
        }
        
        return (IIndustrialConnector)Activator.CreateInstance(type, config)!;
    }
}
