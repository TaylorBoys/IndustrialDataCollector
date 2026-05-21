namespace Core.Models;

public class FormulaDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Expression { get; set; } = string.Empty;
    public List<FormulaParameter> InputParameters { get; set; } = new();
    public string OutputUnit { get; set; } = string.Empty;
}

public class FormulaParameter
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public ParameterSource Source { get; set; } = ParameterSource.DataPoint;
    public double? ConstantValue { get; set; }
}

public enum ParameterSource
{
    DataPoint,
    Constant,
    Calculation
}
