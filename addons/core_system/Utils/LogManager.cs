using System;
using EasyInject.Attributes;
using Godot;
using static EventBus;


/// <summary>
/// 日志服务 - 统一管理框架中的日志输出
/// </summary>
[Component]
public class Logger : IDisposable
{
	/// <summary>日志级别枚举</summary>
	public enum LogLevel
	{
		/// <summary>调试级别，最详细的日志</summary>
		Debug,

		/// <summary>信息级别，常规操作信息</summary>
		Info,

		/// <summary>警告级别，潜在问题提示</summary>
		Warning,

		/// <summary>错误级别，运行时错误</summary>
		Error
	}

	/// <summary>
	/// 日志事件结构 - 用于事件总线传递
	/// </summary>
	public struct LogEvent : EventBus.IEvent
	{
		/// <summary>
		/// 日志消息内容
		/// </summary>
		public string Message;

		/// <summary>
		/// 日志级别
		/// </summary>
		public LogLevel Level;

		/// <summary>
		/// 时间戳
		/// </summary>
		public DateTime Timestamp;
	}

	/// <summary>是否启用调试日志</summary>
	public bool EnableDebugLogs { get; set; } = true;

	/// <summary>是否启用文件日志</summary>
	public bool EnableFileLogging { get; set; } = false;

	/// <summary>日志文件路径</summary>
	public string LogFilePath { get; set; } = "user://logs/game.log";
	/// <summary>
	/// 事件总线
	/// </summary>
	/// <value></value>
	[Autowired]
	public EventBus _eventBus { get; set; }

	/// <summary>
	/// 初始化日志服务
	/// </summary>
	public void Initialize()
	{
		Info("日志服务已初始化");

		if (EnableFileLogging)
		{
			try
			{
				// 确保日志目录存在
				string directory = System.IO.Path.GetDirectoryName(LogFilePath);
				if (!string.IsNullOrEmpty(directory) && !System.IO.Directory.Exists(directory))
				{
					System.IO.Directory.CreateDirectory(directory);
				}

				// 清空或创建日志文件
				System.IO.File.WriteAllText(LogFilePath, $"==== 日志会话开始：{DateTime.Now} ====\n");

				Info($"日志文件已创建：{LogFilePath}");
			}
			catch (Exception ex)
			{
				EnableFileLogging = false;
				Error($"无法创建日志文件：{ex.Message}");
			}
		}
	}

	/// <summary>
	/// 记录调试信息
	/// </summary>
	/// <param name="message">日志消息</param>
	public void Debug(string message)
	{
		if (!EnableDebugLogs)
			return;

		GD.Print($"[调试] {message}");

		LogToFile($"[调试] {message}");
		PublishLogEvent(message, LogLevel.Debug);
	}

	/// <summary>
	/// 记录普通信息
	/// </summary>
	/// <param name="message">日志消息</param>
	public void Info(string message)
	{
		GD.Print($"[信息] {message}");

		LogToFile($"[信息] {message}");
		PublishLogEvent(message, LogLevel.Info);
	}

	/// <summary>
	/// 记录警告信息
	/// </summary>
	/// <param name="message">日志消息</param>
	public void Warning(string message)
	{
		GD.PushWarning($"[警告] {message}");

		LogToFile($"[警告] {message}");
		PublishLogEvent(message, LogLevel.Warning);
	}

	/// <summary>
	/// 记录错误信息
	/// </summary>
	/// <param name="message">日志消息</param>
	public void Error(string message)
	{
		GD.PrintErr($"[错误] {message}");

		LogToFile($"[错误] {message}");
		PublishLogEvent(message, LogLevel.Error);
	}

	/// <summary>
	/// 记录带上下文的错误信息
	/// </summary>
	/// <param name="message">日志消息</param>
	/// <param name="context">错误上下文</param>
	public void Error(string message, object context)
	{
		string contextInfo = context != null ? $" [上下文: {context}]" : "";
		Error($"{message}{contextInfo}");
	}

	/// <summary>
	/// 记录异常信息
	/// </summary>
	/// <param name="ex">异常对象</param>
	/// <param name="context">错误上下文</param>
	public void Exception(Exception ex, string context = null)
	{
		string contextInfo = !string.IsNullOrEmpty(context) ? $" [上下文: {context}]" : "";
		Error($"异常: {ex.Message}{contextInfo}\n{ex.StackTrace}");
	}

	/// <summary>
	/// 写入日志到文件
	/// </summary>
	/// <param name="message">日志消息</param>
	private void LogToFile(string message)
	{
		if (!EnableFileLogging)
			return;

		try
		{
			string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
			string formattedMessage = $"[{timestamp}] {message}";

			System.IO.File.AppendAllText(LogFilePath, formattedMessage + "\n");
		}
		catch (Exception ex)
		{
			// 防止日志记录导致的异常再次触发日志
			EnableFileLogging = false;
			GD.PrintErr($"写入日志文件失败：{ex.Message}");
		}
	}

	/// <summary>
	/// 发布日志事件到事件总线
	/// </summary>
	/// <param name="message">日志消息</param>
	/// <param name="level">日志级别</param>
	private void PublishLogEvent(string message, LogLevel level)
	{
		_eventBus?.Publish(new LogEvent
		{
			Message = message,
			Level = level,
			Timestamp = DateTime.Now
		});
	}

	/// <summary>
	/// 释放资源
	/// </summary>
	public void Dispose()
	{
		if (EnableFileLogging)
		{
			try
			{
				LogToFile($"==== 日志会话结束：{DateTime.Now} ====");
			}
			catch
			{
				// 忽略关闭时的异常
			}
		}

		GD.Print("[日志服务] 已销毁");
	}
}