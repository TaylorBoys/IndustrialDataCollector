using Core.Models;
using Serilog;

namespace Core.Services.FormulaEngine;

public class FormulaCalculationService
{
    private readonly Dictionary<string, FormulaDefinition> _formulas;

    public FormulaCalculationService()
    {
        _formulas = new Dictionary<string, FormulaDefinition>();
        InitializeBuiltInFormulas();
    }

    private void InitializeBuiltInFormulas()
    {
        RegisterFormula(new FormulaDefinition
        {
            Id = "standard_flow",
            Name = "标准状态流量补偿",
            Description = "根据温度和压力进行流量补偿",
            Expression = "Q * P * 293.15 / (101.325 * (T + 273.15))",
            InputParameters = new List<FormulaParameter>
            {
                new() { Name = "Q", Description = "工况流量", Unit = "m³/h" },
                new() { Name = "P", Description = "工况压力", Unit = "kPa" },
                new() { Name = "T", Description = "工况温度", Unit = "°C" }
            },
            OutputUnit = "m³/h (标准状态)"
        });

        RegisterFormula(new FormulaDefinition
        {
            Id = "heat_quantity",
            Name = "热量计算",
            Description = "根据流量和温差计算热量",
            Expression = "M * 4.1868 * (T1 - T2) / 3600",
            InputParameters = new List<FormulaParameter>
            {
                new() { Name = "M", Description = "质量流量", Unit = "kg/h" },
                new() { Name = "T1", Description = "入口温度", Unit = "°C" },
                new() { Name = "T2", Description = "出口温度", Unit = "°C" }
            },
            OutputUnit = "kW"
        });

        RegisterFormula(new FormulaDefinition
        {
            Id = "pressure_loss",
            Name = "管道压损计算",
            Description = "根据流量计算管道压损",
            Expression = "10.67 * POW(Q, 1.852) / (POW(140, 1.852) * POW(100, 4.87))",
            InputParameters = new List<FormulaParameter>
            {
                new() { Name = "Q", Description = "流量", Unit = "m³/h" }
            },
            OutputUnit = "kPa"
        });
    }

    public void RegisterFormula(FormulaDefinition formula)
    {
        _formulas[formula.Id] = formula;
        Log.Information("Registered formula: {FormulaId} - {FormulaName}", formula.Id, formula.Name);
    }

    public List<FormulaDefinition> GetAllFormulas() => _formulas.Values.ToList();

    public FormulaDefinition? GetFormula(string formulaId)
    {
        _formulas.TryGetValue(formulaId, out var formula);
        return formula;
    }

    public double CalculateFormula(string formulaId, Dictionary<string, double> parameters)
    {
        if (!_formulas.TryGetValue(formulaId, out var formula))
            throw new Exception($"公式 '{formulaId}' 未找到");

        var calculator = new FormulaCalculator();
        foreach (var param in formula.InputParameters)
        {
            if (parameters.TryGetValue(param.Name, out var value))
                calculator.SetVariable(param.Name, value);
            else if (param.ConstantValue.HasValue)
                calculator.SetVariable(param.Name, param.ConstantValue.Value);
        }
        return calculator.Calculate(formula.Expression);
    }

    public double CalculateCustomFormula(string expression, Dictionary<string, double> parameters)
    {
        var calculator = new FormulaCalculator();
        calculator.SetVariables(parameters);
        return calculator.Calculate(expression);
    }

    public bool ValidateFormula(string formula, out string? errorMessage)
    {
        var calculator = new FormulaCalculator();
        return calculator.ValidateFormula(formula, out errorMessage);
    }
}
