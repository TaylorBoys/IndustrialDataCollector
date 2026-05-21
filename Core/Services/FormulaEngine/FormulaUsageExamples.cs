using Core.Models;

namespace Core.Services.FormulaEngine;

public static class FormulaUsageExamples
{
    public static void RunExamples()
    {
        var service = new FormulaCalculationService();
        
        Console.WriteLine("=== 工业数据采集系统 - 公式计算引擎 ===\n");
        
        foreach (var (name, expression, parameters, expected) in FormulaExamples.GetExamples())
        {
            Console.WriteLine($"示例: {name}");
            Console.WriteLine($"表达式: {expression}");
            
            try
            {
                var result = service.CalculateCustomFormula(expression, parameters);
                Console.WriteLine($"参数: {string.Join(", ", parameters.Select(kvp => $"{kvp.Key}={kvp.Value}"))}");
                Console.WriteLine($"结果: {result:F4} (预期: {expected:F4})");
                Console.WriteLine($"状态: {(Math.Abs(result - expected) < 0.01 ? "✅ 通过" : "❌ 失败")}\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 错误: {ex.Message}\n");
            }
        }
        
        Console.WriteLine("\n=== 内建公式库 ===");
        foreach (var formula in service.GetAllFormulas())
        {
            Console.WriteLine($"\n- {formula.Id}: {formula.Name}");
            Console.WriteLine($"  描述: {formula.Description}");
            Console.WriteLine($"  表达式: {formula.Expression}");
            Console.WriteLine($"  输入参数: {string.Join(", ", formula.InputParameters.Select(p => $"{p.Name} ({p.Unit})"))}");
            Console.WriteLine($"  输出单位: {formula.OutputUnit}");
        }
    }
}
