# 简单易用的轻量化控制台 - EasyConsole

![.NET](https://img.shields.io/badge/.NET-8.0-blue)
![License](https://img.shields.io/badge/license-MIT-green)

## 项目概述

`EasyConsole` 是一个轻量、易用的命令行框架，专为游戏引擎调试和实时交互设计。基于 C# 开发，它支持异步命令执行、带引号参数解析、命令补全和历史记录管理，采用模块化、线程安全设计，完美集成到 Unity 或其他 .NET 游戏引擎，提供高效的调试和控制体验。

### 主要特性

- **命令解析**：支持带引号的参数（如 `command "arg with spaces"`），处理复杂输入。
- **模块化命令**：通过 `ICommand` 接口实现命令，命令按模块分组，易于扩展。
- **历史记录**：支持命令历史导航（上一条/下一条），限制最大历史记录数以优化内存。
- **命令补全**：提供输入补全建议，提升交互体验。
- **异步支持**：命令执行和输出异步化，适合耗时操作。
- **输出灵活性**：通过 `IOutputHandler` 接口支持多种输出目标（如控制台、文件）。
- **线程安全**：使用 `ConcurrentBag` 和 `ConcurrentDictionary`，支持多线程环境。
- **日志分级**：支持 Info、Warning、Error 三级日志，便于调试和用户反馈。

## 安装

### 安装步骤

1. **克隆仓库**：
   ```bash
   git clone https://github.com/your-username/EasyConsole.git
   cd EasyConsole
   ```

2. **添加项目到你的解决方案**：
   将 `EasyConsole.cs` 添加到你的项目中的合适位置。


3. **开始愉快使用吧**

## 使用示例

以下是一个简单的使用示例，展示如何初始化控制台并执行命令：

```csharp
using EasyConsole;
using System;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        // 初始化命令管理器和输出处理器
        var commandManager = new CommandManager();
        var outputHandler = new ConsoleOutputHandler();
        var controller = new ConsoleController(commandManager, outputHandler, maxHistorySize: 100);

        // 执行命令
        await controller.ProcessInputAsync("hello");
        await controller.ProcessInputAsync("help");
        await controller.ProcessInputAsync("command \"arg with spaces\"");

        // 获取命令补全建议
        var suggestions = controller.GetSuggestions("he");
        Console.WriteLine("补全建议: " + string.Join(", ", suggestions));

        // 历史导航
        Console.WriteLine("上一条命令: " + controller.GetPreviousHistory());
        Console.WriteLine("下一条命令: " + controller.GetNextHistory());

        // 清理资源
        await controller.CleanupAsync();
    }
}
```

### 示例输出

```
[Info] [ConsoleController] 控制台初始化启动...
[Info] 控制台已准备好...
[Info] [ConsoleController] 控制台初始化成功
[Info] > hello
[Info] 你好，世界！
[Info] [ConsoleController] 成功执行命令：hello
[Info] > help
[Info] 可用命令：
System 模块：
------------------------------------------------------------------------------------------------------------------------
  clear: 清空控制台内容和历史记录。用法：clear
  hello: 打印问候信息。用法：hello
  help: 列出所有可用命令。用法：help
[Info] [ConsoleController] 成功执行命令：help
补全建议: hello, help
上一条命令: help
下一条命令: hello
[Info] [ConsoleController] 控制台正在销毁...
[Info] [ConsoleController] 控制台数据已清理
```

## 命令说明

- `hello`：打印“你好，世界！”。
- `clear`：清空历史记录和输出。
- `help`：列出所有可用命令，按模块分组。

你可以通过实现 `ICommand` 接口添加自定义命令。例如：

```csharp
public class CustomCommand : ICommand
{
    public string Name => "custom";
    public string Description => "自定义命令示例。用法：custom [arg]";
    public string Module => "User";

    public async Task ExecuteAsync(string[] args, IOutputHandler output)
    {
        string message = args.Length > 0 ? args[0] : "无参数";
        await output.WriteAsync($"自定义命令输出: {message}", LogLevel.Info);
    }
}
```

注册命令：

```csharp
commandManager.RegisterCommand(new CustomCommand());
```

## 贡献

欢迎贡献代码！请遵循以下步骤：

1. Fork 本仓库。
2. 创建你的功能分支（`git checkout -b feature/YourFeature`）。
3. 提交你的更改（`git commit -m 'Add YourFeature'`）。
4. 推送到分支（`git push origin feature/YourFeature`）。
5. 创建 Pull Request。

请确保代码遵循 C# 编码规范，并附带单元测试。

## 许可证

本项目采用 [MIT 许可证](LICENSE)。详情请查看 `LICENSE` 文件。

## 联系

如有问题或建议，请通过 GitHub Issues 联系，或发送邮件至 **yeyixiang_2007@outlook.com**。
