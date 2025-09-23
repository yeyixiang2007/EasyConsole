// ------------------------------------------------------------
//  _____                _____                       _
// |  ___|              /  __ \                     | |
// | |__  __ _ ___ _   _| /  \/ ___  _ __  ___  ___ | | ___
// |  __|/ _` / __| | | | |    / _ \| '_ \/ __|/ _ \| |/ _ \
// | |__| (_| \__ \ |_| | \__/\ (_) | | | \__ \ (_) | |  __/
// \____/\__, _|___/\__, |\____/\___/|_| |_|___/\___/|_|\___|
//                  __/ |
//                 |___/
// ------------------------------------------------------------
// <author>YeYixiang</author>
// <date>2025-09-21</date>
// <summary>
// 控制台系统核心实现，提供命令解析、历史记录管理、命令补全和异步输出功能。
// 支持带引号的参数解析、模块化命令管理和线程安全操作，适用于游戏调试、服务器管理或其他命令行场景。
// </summary>
// <remarks>
// 本文件包含命令管理、输出处理和历史记录管理的完整实现，采用依赖注入和异步编程模式。
// </remarks>

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyConsole
{
    /// <summary>
    /// 表示日志级别，用于区分输出的严重性。
    /// </summary>
    public enum LogLevel
    {
        Info,
        Warning,
        Error
    }

    /// <summary>
    /// 定义输出处理接口，允许将输出重定向到不同目标。
    /// </summary>
    public interface IOutputHandler
    {
        /// <summary>
        /// 输出消息。
        /// </summary>
        /// <param name="message">要输出的消息。</param>
        /// <param name="level">日志级别。</param>
        Task WriteAsync(string message, LogLevel level);
    }

    /// <summary>
    /// 控制台输出处理器，将消息输出到控制台。
    /// </summary>
    public class ConsoleOutputHandler : IOutputHandler
    {
        /// <inheritdoc />
        public async Task WriteAsync(string message, LogLevel level)
        {
            await Task.Run(() => Console.WriteLine($"[{level}] {message}"));
        }
    }

    /// <summary>
    /// 定义命令接口，允许实现独立的命令逻辑。
    /// </summary>
    public interface ICommand
    {
        /// <summary>
        /// 命令名称。
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 命令描述。
        /// </summary>
        string Description { get; }

        /// <summary>
        /// 所属模块名称。
        /// </summary>
        string Module { get; }

        /// <summary>
        /// 执行命令。
        /// </summary>
        /// <param name="args">命令参数。</param>
        /// <param name="output">输出处理器。</param>
        /// <returns>异步任务。</returns>
        Task ExecuteAsync(string[] args, IOutputHandler output);
    }

    /// <summary>
    /// 命令管理器，负责命令的注册和查找。
    /// </summary>
    public class CommandManager
    {
        private readonly ConcurrentDictionary<string, ICommand> _commands = new ConcurrentDictionary<string, ICommand>();

        /// <summary>
        /// 注册命令。
        /// </summary>
        /// <param name="command">要注册的命令。</param>
        /// <returns>是否注册成功。</returns>
        public bool RegisterCommand(ICommand command)
        {
            return _commands.TryAdd(command.Name.ToLower(), command);
        }

        /// <summary>
        /// 获取命令。
        /// </summary>
        /// <param name="name">命令名称。</param>
        /// <returns>命令实例，或 null 如果不存在。</returns>
        public ICommand? GetCommand(string name)
        {
            _commands.TryGetValue(name.ToLower(), out var command);
            return command;
        }

        /// <summary>
        /// 获取所有命令，按模块分组。
        /// </summary>
        /// <returns>按模块分组的命令集合。</returns>
        public IEnumerable<IGrouping<string, ICommand>> GetGroupedCommands()
        {
            return _commands.Values.GroupBy(cmd => cmd.Module).OrderBy(g => g.Key);
        }

        /// <summary>
        /// 获取命令补全建议，支持非连续但顺序正确的字符匹配。
        /// </summary>
        /// <param name="input">用户输入的前缀。</param>
        /// <returns>匹配的命令名称列表。</returns>
        public IEnumerable<string> GetSuggestions(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return Enumerable.Empty<string>();

            string inputLower = input.Trim().Split(' ')[0].ToLower();
            return _commands.Keys
                .Where(cmd => IsSubsequenceMatch(inputLower, cmd))
                .OrderBy(cmd => cmd);
        }

        /// <summary>
        /// 检查输入字符串是否为命令名称的子序列（非连续但顺序正确）。
        /// </summary>
        /// <param name="input">输入字符串。</param>
        /// <param name="command">命令名称。</param>
        /// <returns>是否匹配。</returns>
        private bool IsSubsequenceMatch(string input, string command)
        {
            if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(command))
                return false;

            int inputIndex = 0;
            int commandIndex = 0;

            while (inputIndex < input.Length && commandIndex < command.Length)
            {
                if (char.ToLower(input[inputIndex]) == char.ToLower(command[commandIndex]))
                {
                    inputIndex++;
                }
                commandIndex++;
            }

            return inputIndex == input.Length;
        }
    }

    /// <summary>
    /// 示例命令：打印问候信息。
    /// </summary>
    public class HelloCommand : ICommand
    {
        public string Name => "hello";
        public string Description => "打印问候信息。用法：hello";
        public string Module => "System";

        public async Task ExecuteAsync(string[] args, IOutputHandler output)
        {
            await output.WriteAsync("你好，世界！", LogLevel.Info);
        }
    }

    /// <summary>
    /// 示例命令：清空控制台。
    /// </summary>
    public class ClearCommand : ICommand
    {
        private readonly ConsoleController _controller;

        public ClearCommand(ConsoleController controller)
        {
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));
        }

        public string Name => "clear";
        public string Description => "清空控制台内容和历史记录。用法：clear";
        public string Module => "System";

        public async Task ExecuteAsync(string[] args, IOutputHandler output)
        {
            _controller.Clear();
            await output.WriteAsync("控制台已清空", LogLevel.Info);
        }
    }

    /// <summary>
    /// 示例命令：列出所有可用命令。
    /// </summary>
    public class HelpCommand : ICommand
    {
        private readonly CommandManager _commandManager;

        public HelpCommand(CommandManager commandManager)
        {
            _commandManager = commandManager ?? throw new ArgumentNullException(nameof(commandManager));
        }

        public string Name => "help";
        public string Description => "列出所有可用命令。用法：help";
        public string Module => "System";

        public async Task ExecuteAsync(string[] args, IOutputHandler output)
        {
            var builder = new StringBuilder();
            builder.AppendLine("可用命令：");

            foreach (var group in _commandManager.GetGroupedCommands())
            {
                builder.AppendLine($"\n{group.Key} 模块：");
                builder.AppendLine("------------------------------------------------------------------------------------------------------------------------");
                foreach (var cmd in group.OrderBy(c => c.Name))
                {
                    builder.AppendLine($"  {cmd.Name}: {cmd.Description}");
                }
            }

            await output.WriteAsync(builder.ToString(), LogLevel.Info);
        }
    }

    /// <summary>
    /// 控制台核心类，管理命令输入、历史记录和输出。
    /// </summary>
    public class ConsoleController
    {
        private readonly CommandManager _commandManager;
        private readonly IOutputHandler _outputHandler;
        private readonly ConcurrentBag<string> _commandHistory;
        private readonly int _maxHistorySize;
        private int _historyIndex;
        private readonly StringBuilder _outputBuilder;

        /// <summary>
        /// 初始化控制台。
        /// </summary>
        /// <param name="commandManager">命令管理器。</param>
        /// <param name="outputHandler">输出处理器。</param>
        /// <param name="maxHistorySize">最大历史记录数，默认为100。</param>
        public ConsoleController(CommandManager commandManager, IOutputHandler outputHandler, int maxHistorySize = 100)
        {
            _commandManager = commandManager ?? throw new ArgumentNullException(nameof(commandManager));
            _outputHandler = outputHandler ?? throw new ArgumentNullException(nameof(outputHandler));
            _maxHistorySize = maxHistorySize > 0 ? maxHistorySize : throw new ArgumentException("最大历史记录数必须大于0", nameof(maxHistorySize));
            _commandHistory = new ConcurrentBag<string>();
            _historyIndex = -1;
            _outputBuilder = new StringBuilder();

            Initialize();
        }

        /// <summary>
        /// 初始化控制台，注册默认命令。
        /// </summary>
        private async void Initialize()
        {
            await _outputHandler.WriteAsync("[ConsoleController] 控制台初始化启动...", LogLevel.Info);
            RegisterDefaultCommands();
            await _outputHandler.WriteAsync("控制台已准备好...", LogLevel.Info);
            await _outputHandler.WriteAsync("[ConsoleController] 控制台初始化成功", LogLevel.Info);
        }

        /// <summary>
        /// 注册默认命令。
        /// </summary>
        private void RegisterDefaultCommands()
        {
            _commandManager.RegisterCommand(new HelloCommand());
            _commandManager.RegisterCommand(new ClearCommand(this));
            _commandManager.RegisterCommand(new HelpCommand(_commandManager));
        }

        /// <summary>
        /// 处理用户输入的命令。
        /// </summary>
        /// <param name="input">用户输入的命令字符串。</param>
        /// <returns>异步任务。</returns>
        public async Task ProcessInputAsync(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                await _outputHandler.WriteAsync("[ConsoleController] 命令处理失败：输入为空或仅包含空白字符", LogLevel.Warning);
                return;
            }

            await _outputHandler.WriteAsync($"> {input}", LogLevel.Info);
            AddToHistory(input);
            await ProcessCommandAsync(input);
        }

        /// <summary>
        /// 添加命令到历史记录。
        /// </summary>
        /// <param name="input">输入的命令。</param>
        private void AddToHistory(string input)
        {
            _commandHistory.Add(input);
            if (_commandHistory.Count > _maxHistorySize)
            {
                _commandHistory.TryTake(out _);
            }
            _historyIndex = -1;
        }

        /// <summary>
        /// 获取上一条历史命令。
        /// </summary>
        /// <returns>上一条命令，或空字符串如果没有。</returns>
        public string GetPreviousHistory()
        {
            if (_historyIndex >= _commandHistory.Count - 1)
                return string.Empty;

            _historyIndex++;
            var historyArray = _commandHistory.ToArray();
            return historyArray[_commandHistory.Count - 1 - _historyIndex];
        }

        /// <summary>
        /// 获取下一条历史命令。
        /// </summary>
        /// <returns>下一条命令，或空字符串如果没有。</returns>
        public string GetNextHistory()
        {
            if (_historyIndex <= -1)
                return string.Empty;

            _historyIndex--;
            if (_historyIndex == -1)
                return string.Empty;

            var historyArray = _commandHistory.ToArray();
            return historyArray[_commandHistory.Count - 1 - _historyIndex];
        }

        /// <summary>
        /// 获取命令补全建议。
        /// </summary>
        /// <param name="input">当前输入。</param>
        /// <returns>建议的命令列表。</returns>
        public IEnumerable<string> GetSuggestions(string input)
        {
            return _commandManager.GetSuggestions(input);
        }

        /// <summary>
        /// 解析并执行命令。
        /// </summary>
        /// <param name="input">输入的命令字符串。</param>
        /// <returns>异步任务。</returns>
        private async Task ProcessCommandAsync(string input)
        {
            // 简单解析，支持带引号的参数
            var args = ParseCommand(input);
            string command = args.Length > 0 ? args[0].ToLower() : string.Empty;
            string[] commandArgs = args.Length > 1 ? args[1..] : new string[0];

            var cmd = _commandManager.GetCommand(command);
            if (cmd != null)
            {
                try
                {
                    await cmd.ExecuteAsync(commandArgs, _outputHandler);
                    await _outputHandler.WriteAsync($"[ConsoleController] 成功执行命令：{command}", LogLevel.Info);
                }
                catch (Exception ex)
                {
                    await _outputHandler.WriteAsync($"[ConsoleController] 命令执行失败：{ex.Message}", LogLevel.Error);
                }
            }
            else
            {
                await _outputHandler.WriteAsync($"[ConsoleController] 错误：未知命令 '{command}'。使用 'help' 查看可用命令", LogLevel.Error);
            }
        }

        /// <summary>
        /// 解析命令字符串，支持带引号的参数。
        /// </summary>
        /// <param name="input">输入字符串。</param>
        /// <returns>解析后的参数数组。</returns>
        private string[] ParseCommand(string input)
        {
            var args = new List<string>();
            var current = new StringBuilder();
            bool inQuotes = false;
            char quoteChar = '\0';

            foreach (char c in input)
            {
                if ((c == '"' || c == '\'') && !inQuotes)
                {
                    inQuotes = true;
                    quoteChar = c;
                }
                else if (c == quoteChar && inQuotes)
                {
                    inQuotes = false;
                    args.Add(current.ToString());
                    current.Clear();
                }
                else if (char.IsWhiteSpace(c) && !inQuotes)
                {
                    if (current.Length > 0)
                    {
                        args.Add(current.ToString());
                        current.Clear();
                    }
                }
                else
                {
                    current.Append(c);
                }
            }

            if (current.Length > 0)
                args.Add(current.ToString());

            return args.ToArray();
        }

        /// <summary>
        /// 清空控制台数据。
        /// </summary>
        public void Clear()
        {
            _commandHistory.Clear();
            _historyIndex = -1;
            _outputBuilder.Clear();
        }

        /// <summary>
        /// 清理控制台资源。
        /// </summary>
        public async Task CleanupAsync()
        {
            await _outputHandler.WriteAsync("[ConsoleController] 控制台正在销毁...", LogLevel.Info);
            Clear();
            await _outputHandler.WriteAsync("[ConsoleController] 控制台数据已清理", LogLevel.Info);
        }

        /// <summary>
        /// 获取当前输出内容。
        /// </summary>
        /// <returns>输出内容的字符串表示。</returns>
        public string GetOutput()
        {
            return _outputBuilder.ToString();
        }
    }
}
