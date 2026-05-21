using System.Text.RegularExpressions;

namespace Core.Services.FormulaEngine;

public class FormulaCalculator
{
    private readonly Dictionary<string, double> _variables;
    private readonly Dictionary<string, Func<double[], double>> _functions;

    public FormulaCalculator()
    {
        _variables = new Dictionary<string, double>();
        _functions = new Dictionary<string, Func<double[], double>>();
        InitializeFunctions();
    }

    private void InitializeFunctions()
    {
        _functions["SIN"] = args => Math.Sin(args[0] * Math.PI / 180);
        _functions["COS"] = args => Math.Cos(args[0] * Math.PI / 180);
        _functions["TAN"] = args => Math.Tan(args[0] * Math.PI / 180);
        _functions["ASIN"] = args => Math.Asin(args[0]) * 180 / Math.PI;
        _functions["ACOS"] = args => Math.Acos(args[0]) * 180 / Math.PI;
        _functions["ATAN"] = args => Math.Atan(args[0]) * 180 / Math.PI;
        _functions["SQRT"] = args => Math.Sqrt(args[0]);
        _functions["ABS"] = args => Math.Abs(args[0]);
        _functions["LOG"] = args => Math.Log(args[0]);
        _functions["LOG10"] = args => Math.Log10(args[0]);
        _functions["EXP"] = args => Math.Exp(args[0]);
        _functions["POW"] = args => Math.Pow(args[0], args[1]);
        _functions["MIN"] = args => Math.Min(args[0], args[1]);
        _functions["MAX"] = args => Math.Max(args[0], args[1]);
        _functions["ROUND"] = args => Math.Round(args[0], (int)args[1]);
        _functions["FLOOR"] = args => Math.Floor(args[0]);
        _functions["CEILING"] = args => Math.Ceiling(args[0]);
        _functions["PI"] = args => Math.PI;
        _functions["E"] = args => Math.E;
        _functions["IF"] = args => args[0] != 0 ? args[1] : args[2];
        _functions["AVG"] = args => args.Average();
        _functions["SUM"] = args => args.Sum();
    }

    public void SetVariable(string name, double value) => _variables[name] = value;
    public void SetVariables(Dictionary<string, double> variables)
    {
        foreach (var kvp in variables) _variables[kvp.Key] = kvp.Value;
    }

    public double Calculate(string formula)
    {
        try
        {
            var expression = PreprocessFormula(formula);
            return EvaluateExpression(expression);
        }
        catch (Exception ex)
        {
            throw new Exception($"公式计算失败: {ex.Message}", ex);
        }
    }

    private string PreprocessFormula(string formula)
    {
        var result = formula.Trim();
        foreach (var kvp in _variables)
        {
            result = Regex.Replace(result, $@"\b{kvp.Key}\b", kvp.Value.ToString("G17"), RegexOptions.IgnoreCase);
        }
        return result;
    }

    private double EvaluateExpression(string expression)
    {
        expression = expression.Trim();
        if (string.IsNullOrEmpty(expression)) return 0;
        
        expression = HandleIfFunction(expression);
        expression = HandleFunctions(expression);
        return ParseAddSubtract(expression);
    }

    private string HandleIfFunction(string expression)
    {
        var ifPattern = new Regex(@"IF\s*\(([^,]+),\s*([^,]+),\s*([^)]+)\)", RegexOptions.IgnoreCase);
        while (ifPattern.IsMatch(expression))
        {
            expression = ifPattern.Replace(expression, match =>
            {
                var condition = EvaluateExpression(match.Groups[1].Value);
                var trueValue = EvaluateExpression(match.Groups[2].Value);
                var falseValue = EvaluateExpression(match.Groups[3].Value);
                return (condition != 0 ? trueValue : falseValue).ToString("G17");
            });
        }
        return expression;
    }

    private string HandleFunctions(string expression)
    {
        var functionPattern = new Regex(@"([A-Z_]+)\s*\(([^)]+)\)", RegexOptions.IgnoreCase);
        while (functionPattern.IsMatch(expression))
        {
            expression = functionPattern.Replace(expression, match =>
            {
                var funcName = match.Groups[1].Value.ToUpper();
                var argsStr = match.Groups[2].Value;
                if (_functions.TryGetValue(funcName, out var func))
                {
                    var args = ParseArguments(argsStr);
                    return func(args).ToString("G17");
                }
                return match.Value;
            });
        }
        return expression;
    }

    private double[] ParseArguments(string argsStr)
    {
        var args = new List<double>();
        var depth = 0;
        var current = "";
        foreach (var c in argsStr)
        {
            if (c == '(') depth++;
            else if (c == ')') depth--;
            else if (c == ',' && depth == 0)
            {
                args.Add(string.IsNullOrWhiteSpace(current) ? 0 : EvaluateExpression(current));
                current = "";
                continue;
            }
            current += c;
        }
        if (!string.IsNullOrWhiteSpace(current))
            args.Add(EvaluateExpression(current));
        return args.ToArray();
    }

    private double ParseAddSubtract(string expression)
    {
        var tokens = Tokenize(expression, '+', '-');
        if (tokens.Count == 1) return ParseMultiplyDivide(tokens[0]);
        double result = ParseMultiplyDivide(tokens[0]);
        for (int i = 1; i < tokens.Count; i += 2)
        {
            var op = tokens[i];
            var value = ParseMultiplyDivide(tokens[i + 1]);
            result = op == "+" ? result + value : result - value;
        }
        return result;
    }

    private double ParseMultiplyDivide(string expression)
    {
        var tokens = Tokenize(expression, '*', '/', '%');
        if (tokens.Count == 1) return ParsePower(tokens[0]);
        double result = ParsePower(tokens[0]);
        for (int i = 1; i < tokens.Count; i += 2)
        {
            var op = tokens[i];
            var value = ParsePower(tokens[i + 1]);
            result = op switch
            {
                "*" => result * value,
                "/" => value != 0 ? result / value : 0,
                "%" => value != 0 ? result % value : 0,
                _ => result
            };
        }
        return result;
    }

    private double ParsePower(string expression)
    {
        var tokens = Tokenize(expression, '^');
        if (tokens.Count == 1) return ParseUnary(tokens[0]);
        double result = ParseUnary(tokens[^1]);
        for (int i = tokens.Count - 2; i >= 0; i--)
            result = Math.Pow(ParseUnary(tokens[i]), result);
        return result;
    }

    private double ParseUnary(string expression)
    {
        expression = expression.Trim();
        if (expression.StartsWith('-')) return -ParseUnary(expression.Substring(1));
        if (expression.StartsWith('+')) return ParseUnary(expression.Substring(1));
        return ParsePrimary(expression);
    }

    private double ParsePrimary(string expression)
    {
        expression = expression.Trim();
        if (expression.StartsWith('(') && expression.EndsWith(')'))
            return EvaluateExpression(expression.Substring(1, expression.Length - 2));
        if (double.TryParse(expression, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var value))
            return value;
        throw new FormatException($"无法解析值: {expression}");
    }

    private List<string> Tokenize(string expression, params char[] operators)
    {
        var tokens = new List<string>();
        var depth = 0;
        var current = "";
        foreach (var c in expression)
        {
            if (c == '(') depth++;
            else if (c == ')') depth--;
            if (depth == 0 && operators.Contains(c))
            {
                if (!string.IsNullOrWhiteSpace(current)) tokens.Add(current);
                tokens.Add(c.ToString());
                current = "";
            }
            else current += c;
        }
        if (!string.IsNullOrWhiteSpace(current)) tokens.Add(current);
        return tokens;
    }

    public bool ValidateFormula(string formula, out string? errorMessage)
    {
        try
        {
            var calc = new FormulaCalculator();
            calc.SetVariables(new Dictionary<string, double> { ["A"] = 10, ["B"] = 5 });
            calc.Calculate(formula);
            errorMessage = null;
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            return false;
        }
    }
}
