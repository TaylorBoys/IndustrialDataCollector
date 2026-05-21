namespace Core.Models;

public enum ProtocolType
{
    ModbusTcp,
    ModbusRtu,
    OpcUa
}

public enum DataType
{
    Bool,
    Int16,
    UInt16,
    Int32,
    UInt32,
    Float,
    Double,
    String
}

public class DeviceConfig
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public ProtocolType Protocol { get; set; }
    public string IpAddress { get; set; } = "127.0.0.1";
    public int Port { get; set; }
    public int SlaveId { get; set; } = 1;
    public string? OpcUaEndpoint { get; set; }
    public List<DataPointConfig> DataPoints { get; set; } = new();
    public int ScanIntervalMs { get; set; } = 1000;
}

public class DataPointConfig
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public DataType DataType { get; set; }
    public string? Unit { get; set; }
    public double? ScaleFactor { get; set; }
    public double? Offset { get; set; }
    public bool IsWritable { get; set; }
    public bool IsCalculated { get; set; }
    public string? FormulaId { get; set; }
    public string? CustomFormula { get; set; }
}

public class DataPointValue
{
    public string DataPointId { get; set; } = string.Empty;
    public string DataPointName { get; set; } = string.Empty;
    public object? Value { get; set; }
    public DateTime Timestamp { get; set; }
    public string? Unit { get; set; }
    public bool IsValid { get; set; }
}
