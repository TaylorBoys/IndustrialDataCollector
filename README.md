# 工业数据采集系统

一个功能完整的工业数据采集系统，支持Modbus协议，包含数据处理、公式计算、存储和上传功能。

## 功能特性

- **协议支持**：Modbus TCP/RTU
- **公式计算引擎**：支持复杂数学表达式和内置公式
  - 标准状态流量补偿
  - 热量计算
  - 管道压损计算
  - 自定义公式
- **数据缩放**：支持线性缩放因子和偏移量
- **数据存储**：SQLite本地存储
- **数据上传**：支持HTTP API上传
- **多设备管理**：独立线程，互不干扰
- **WPF界面**：现代化桌面应用
- **日志系统**：Serilog结构化日志

## 项目结构

```
IndustrialDataCollector/
├── IndustrialDataCollector.sln    # 解决方案文件
├── Core/                          # 核心库
│   ├── Models/                    # 数据模型
│   ├── Interfaces/                # 接口定义
│   └── Services/
│       ├── FormulaEngine/         # 公式计算引擎
│       ├── DataStorageService.cs  # 数据存储服务
│       ├── DataProcessingService.cs # 数据处理服务
│       ├── DeviceWorker.cs        # 设备工作线程
│       └── DeviceManager.cs       # 设备管理器（单例）
├── Drivers/                       # 协议驱动
│   ├── ModbusDriver/
│   └── OpcUaDriver/
└── WpfApp/                        # WPF桌面应用
    ├── ViewModels/
    └── Converters/
```

## 快速开始

### 环境要求

- .NET 8.0 SDK
- Visual Studio 2022

### 编译运行

1. 在Visual Studio中打开 `IndustrialDataCollector.sln`
2. 还原NuGet包
3. 编译解决方案
4. 运行WpfApp项目

## 使用示例

### 公式计算引擎

```csharp
var formulaService = new FormulaCalculationService();

// 使用内置公式
var parameters = new Dictionary<string, double>
{
    ["Q"] = 100,
    ["P"] = 200,
    ["T"] = 50
};
double result = formulaService.CalculateFormula("standard_flow", parameters);

// 使用自定义公式
var customParams = new Dictionary<string, double> { ["Q"] = 100 };
double customResult = formulaService.CalculateCustomFormula(
    "Q * 0.1 + 2", customParams);
```

### 数据处理

```csharp
var processingService = new DataProcessingService();

// 应用缩放
double rawValue = 255;
double scaledValue = processingService.ApplyScale(rawValue, 0.1, 0);
```

### 数据存储

```csharp
var storage = new DataStorageService();

// 插入数据
await storage.InsertRecordAsync(record);

// 查询数据
var records = await storage.GetRecordsByDeviceIdAsync(deviceId);
```

## 依赖项

- CommunityToolkit.Mvvm 8.2.2
- FluentModbus 5.3.2
- OPCFoundation.NetStandard.Opc.Ua.Client 1.5.375
- Dapper 2.1.35
- Microsoft.Data.Sqlite 8.0.8
- Serilog 3.1.1

## 许可证

MIT License
