namespace Core.Services.FormulaEngine;

public static class FormulaExamples
{
    public static List<(string Name, string Expression, Dictionary<string, double> Parameters, double ExpectedResult)> GetExamples()
    {
        return new List<(string Name, string Expression, Dictionary<string, double> Parameters, double ExpectedResult)>
        {
            (
                "基本算术运算",
                "A + B * C",
                new Dictionary<string, double> { ["A"] = 5, ["B"] = 3, ["C"] = 2 },
                11
            ),
            (
                "标准状态流量补偿",
                "Q * P * 293.15 / (101.325 * (T + 273.15))",
                new Dictionary<string, double> { ["Q"] = 100, ["P"] = 200, ["T"] = 50 },
                160.8
            ),
            (
                "热量计算",
                "M * 4.1868 * (T1 - T2) / 3600",
                new Dictionary<string, double> { ["M"] = 10000, ["T1"] = 85, ["T2"] = 65 },
                232.6
            ),
            (
                "三角函数",
                "SIN(30) + COS(60)",
                new Dictionary<string, double>(),
                1.0
            ),
            (
                "IF条件判断",
                "IF(T > 100, 100, T)",
                new Dictionary<string, double> { ["T"] = 120 },
                100
            ),
            (
                "平方根与幂运算",
                "SQRT(A) + POW(B, 2)",
                new Dictionary<string, double> { ["A"] = 16, ["B"] = 3 },
                13
            )
        };
    }
}
