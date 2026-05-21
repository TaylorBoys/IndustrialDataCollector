using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Core.Models;

namespace WpfApp.Converters;

public class BooleanToStatusConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue ? "运行中" : "已停止";
        }
        return "未知";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class BooleanToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue ? new SolidColorBrush(Color.FromRgb(76, 175, 80)) : new SolidColorBrush(Color.FromRgb(244, 67, 54));
        }
        return new SolidColorBrush(Color.FromRgb(158, 158, 158));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class ProtocolTypeToChineseConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ProtocolType protocol)
        {
            return protocol switch
            {
                ProtocolType.ModbusTcp => "Modbus TCP",
                ProtocolType.ModbusRtu => "Modbus RTU",
                ProtocolType.OpcUa => "OPC UA",
                _ => "未知"
            };
        }
        return "未知";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string str)
        {
            return str switch
            {
                "Modbus TCP" => ProtocolType.ModbusTcp,
                "Modbus RTU" => ProtocolType.ModbusRtu,
                "OPC UA" => ProtocolType.OpcUa,
                _ => ProtocolType.ModbusTcp
            };
        }
        return ProtocolType.ModbusTcp;
    }
}

public class DataTypeToChineseConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is DataType dataType)
        {
            return dataType switch
            {
                DataType.Bool => "布尔",
                DataType.Int16 => "短整数",
                DataType.UInt16 => "无符号短整数",
                DataType.Int32 => "整数",
                DataType.UInt32 => "无符号整数",
                DataType.Float => "浮点数",
                DataType.Double => "双精度",
                DataType.String => "字符串",
                _ => "未知"
            };
        }
        return "未知";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string str)
        {
            return str switch
            {
                "布尔" => DataType.Bool,
                "短整数" => DataType.Int16,
                "无符号短整数" => DataType.UInt16,
                "整数" => DataType.Int32,
                "无符号整数" => DataType.UInt32,
                "浮点数" => DataType.Float,
                "双精度" => DataType.Double,
                "字符串" => DataType.String,
                _ => DataType.Int16
            };
        }
        return DataType.Int16;
    }
}
