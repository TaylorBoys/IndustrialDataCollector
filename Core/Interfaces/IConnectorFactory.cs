using Core.Models;

namespace Core.Interfaces;

public interface IConnectorFactory
{
    IIndustrialConnector CreateConnector(DeviceConfig config);
}
