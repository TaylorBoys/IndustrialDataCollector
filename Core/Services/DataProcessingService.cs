using Core.Models;
using Core.Services.FormulaEngine;
using Serilog;

namespace Core.Services;

public class DataProcessingService
{
    private readonly FormulaCalculationService _formulaService;

    public DataProcessingService()
    {
        _formulaService = new FormulaCalculationService();
    }

    public double ApplyScale(double rawValue, double? scaleFactor, double? offset)
    {
        double result = rawValue;
        if (scaleFactor.HasValue) result *= scaleFactor.Value;
        if (offset.HasValue) result += offset.Value;
        return result;
    }

    public DataPointValue ProcessDataPoint(DataPointConfig config, object? rawValue)
    {
        var value = new DataPointValue
        {
            DataPointId = config.Id,
            DataPointName = config.Name,
            Unit = config.Unit,
            Timestamp = DateTime.Now,
            IsValid = rawValue != null
        };

        if (rawValue != null)
        {
            if (double.TryParse(rawValue.ToString(), out var numValue))
            {
                value.Value = ApplyScale(numValue, config.ScaleFactor, config.Offset);
            }
            else
            {
                value.Value = rawValue;
            }
        }

        return value;
    }

    public DataRecord CreateDataRecord(DeviceConfig device, DataPointConfig dataPoint, DataPointValue value)
    {
        double? rawValue = null;
        double? scaledValue = null;

        if (value.Value != null && double.TryParse(value.Value.ToString(), out var numValue))
        {
            if (dataPoint.ScaleFactor.HasValue || dataPoint.Offset.HasValue)
            {
                scaledValue = numValue;
                if (dataPoint.ScaleFactor.HasValue && dataPoint.Offset.HasValue)
                {
                    rawValue = (numValue - dataPoint.Offset.Value) / dataPoint.ScaleFactor.Value;
                }
                else if (dataPoint.ScaleFactor.HasValue)
                {
                    rawValue = numValue / dataPoint.ScaleFactor.Value;
                }
                else if (dataPoint.Offset.HasValue)
                {
                    rawValue = numValue - dataPoint.Offset.Value;
                }
            }
            else
            {
                rawValue = numValue;
                scaledValue = numValue;
            }
        }

        return new DataRecord
        {
            DeviceId = device.Id,
            DeviceName = device.Name,
            DataPointId = dataPoint.Id,
            DataPointName = dataPoint.Name,
            RawValue = rawValue,
            ScaledValue = scaledValue,
            Unit = value.Unit,
            IsValid = value.IsValid,
            RecordTime = value.Timestamp
        };
    }

    public double CalculateFormulaValue(string formulaId, Dictionary<string, double> parameters)
    {
        return _formulaService.CalculateFormula(formulaId, parameters);
    }

    public double CalculateCustomFormula(string expression, Dictionary<string, double> parameters)
    {
        return _formulaService.CalculateCustomFormula(expression, parameters);
    }

    public FormulaCalculationService GetFormulaService() => _formulaService;
}
