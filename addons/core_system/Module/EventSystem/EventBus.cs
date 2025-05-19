using EasyInject.Attributes;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

/// <summary>
/// 泛型事件总线 - 提供基于发布-订阅模式的高性能类型安全事件系统
/// </summary>
/// <remarks>
/// 实现了一个完整的泛型事件总线系统，允许游戏不同组件之间通过强类型事件进行解耦通信。
/// 支持事件过滤、优先级排序、一次性订阅、延迟调用和历史记录等高级功能。
/// </remarks>
[CreateNode]
public partial class EventBus : Node
{
	#region 内部类和接口定义

	/// <summary>
	/// 所有事件的基本接口
	/// </summary>
	/// <remarks>
	/// 作为事件类型的标记接口，所有事件必须实现此接口。
	/// 建议使用值类型（struct）实现此接口以获得更好的性能。
	/// </remarks>
	public interface IEvent { }

	/// <summary>
	/// 事件元数据类 - 存储事件订阅的附加信息
	/// </summary>
	/// <remarks>
	/// 包含事件处理的优先级和一次性订阅标记。
	/// 使用只读结构体以提高性能并减少内存分配。
	/// </remarks>
	[Serializable]
	public readonly struct EventMetadata : IEquatable<EventMetadata>
	{
		/// <summary>
		/// 事件优先级 - 数值越小优先级越高
		/// </summary>
		/// <remarks>
		/// 决定事件处理器的执行顺序，从小到大排序。
		/// </remarks>
		public int Priority { get; }

		/// <summary>
		/// 是否为一次性订阅 - 触发一次后自动取消订阅
		/// </summary>
		/// <remarks>
		/// 如果为true，处理器在执行一次后会自动被移除。
		/// 适用于只需响应一次的事件，如完成成就、引导提示等。
		/// </remarks>
		public bool OneTime { get; }

		/// <summary>
		/// 创建默认的事件元数据
		/// </summary>
		/// <remarks>
		/// 默认优先级为0，非一次性订阅。
		/// </remarks>
		public EventMetadata()
		{
			Priority = 0;
			OneTime = false;
		}

		/// <summary>
		/// 创建包含指定优先级和一次性标记的事件元数据
		/// </summary>
		/// <param name="priority">事件优先级（越小优先级越高）</param>
		/// <param name="oneTime">是否为一次性订阅</param>
		/// <remarks>
		/// 允许手动指定优先级和一次性标志。
		/// </remarks>
		public EventMetadata(int priority = 0, bool oneTime = false)
		{
			Priority = priority;
			OneTime = oneTime;
		}

		/// <summary>
		/// 比较两个元数据对象是否相等
		/// </summary>
		/// <param name="other">要比较的另一个元数据对象</param>
		/// <returns>如果相等则为true，否则为false</returns>
		public bool Equals(EventMetadata other)
		{
			return Priority == other.Priority && OneTime == other.OneTime;
		}

		/// <summary>
		/// 重写默认的Equals方法
		/// </summary>
		/// <param name="obj">要比较的对象</param>
		/// <returns>如果相等则为true，否则为false</returns>
		public override bool Equals(object obj)
		{
			return obj is EventMetadata metadata && Equals(metadata);
		}

		/// <summary>
		/// 提供与Equals一致的哈希码
		/// </summary>
		/// <returns>唯一标识此元数据的哈希码</returns>
		public override int GetHashCode()
		{
			return HashCode.Combine(Priority, OneTime);
		}

		/// <summary>
		/// 相等运算符重载
		/// </summary>
		/// <param name="left">左侧操作数</param>
		/// <param name="right">右侧操作数</param>
		/// <returns>如果两个对象相等则为true，否则为false</returns>
		public static bool operator ==(EventMetadata left, EventMetadata right)
		{
			return left.Equals(right);
		}

		/// <summary>
		/// 不等运算符重载
		/// </summary>
		/// <param name="left">左侧操作数</param>
		/// <param name="right">右侧操作数</param>
		/// <returns>如果两个对象不相等则为true，否则为false</returns>
		public static bool operator !=(EventMetadata left, EventMetadata right)
		{
			return !left.Equals(right);
		}
	}

	/// <summary>
	/// 优先级队列项 - 用于存储带优先级的处理器
	/// </summary>
	/// <typeparam name="TEvent">事件类型</typeparam>
	/// <remarks>
	/// 封装事件处理器及其元数据，支持优先级排序和状态标记。
	/// </remarks>
	private class HandlerItem<TEvent> where TEvent : IEvent
	{
		/// <summary>
		/// 事件处理委托 - 当事件发布时将被调用
		/// </summary>
		/// <remarks>
		/// 这是实际处理事件的函数引用，具有类型安全性。
		/// </remarks>
		public Action<TEvent> Handler { get; }

		/// <summary>
		/// 事件处理的元数据 - 包含优先级和一次性标记
		/// </summary>
		/// <remarks>
		/// 决定处理器的执行顺序和是否在处理一次后自动移除。
		/// </remarks>
		public EventMetadata Metadata { get; }

		/// <summary>
		/// 指示处理器是否处于活动状态
		/// </summary>
		/// <remarks>
		/// 用于标记已失效但尚未物理移除的处理器，避免频繁修改集合。
		/// </remarks>
		public bool IsActive { get; set; } = true;

		/// <summary>
		/// 拥有此处理器的对象类型
		/// </summary>
		/// <remarks>
		/// 用于调试和历史记录，帮助识别处理器所属的组件。
		/// </remarks>
		public Type OwnerType { get; }

		/// <summary>
		/// 处理器方法的名称
		/// </summary>
		/// <remarks>
		/// 用于调试和历史记录，提供更详细的处理器信息。
		/// </remarks>
		public string HandlerName { get; }

		/// <summary>
		/// 创建新的处理器项
		/// </summary>
		/// <param name="handler">事件处理委托</param>
		/// <param name="metadata">处理器元数据</param>
		/// <remarks>
		/// 初始化处理器项并提取处理器的目标类型和方法名。
		/// </remarks>
		public HandlerItem(Action<TEvent> handler, EventMetadata metadata)
		{
			Handler = handler;
			Metadata = metadata;

			// 保存处理器信息，用于调试和历史记录
			OwnerType = handler.Target?.GetType();
			HandlerName = handler.Method.Name;
		}
	}

	/// <summary>
	/// 事件过滤器委托 - 决定事件是否应被特定处理器处理
	/// </summary>
	/// <typeparam name="TEvent">事件类型</typeparam>
	/// <param name="eventData">事件数据</param>
	/// <returns>如果事件应被处理，则为true</returns>
	/// <remarks>
	/// 允许基于事件数据内容选择性地处理事件。
	/// 例如，只处理特定玩家ID的事件或特定区域内的事件。
	/// </remarks>
	public delegate bool EventFilter<TEvent>(TEvent eventData) where TEvent : IEvent;

	/// <summary>
	/// 事件历史记录条目 - 记录单个事件的触发信息
	/// </summary>
	/// <remarks>
	/// 用于调试和事件追踪，记录事件的名称、数据、时间以及哪些处理器被调用。
	/// 帮助开发者了解事件流和性能瓶颈。
	/// </remarks>
	public class EventHistoryEntry
	{
		/// <summary>
		/// 事件类型的名称
		/// </summary>
		/// <remarks>
		/// 事件类型的简短名称，不含命名空间。
		/// </remarks>
		public string EventName { get; }

		/// <summary>
		/// 事件类型
		/// </summary>
		/// <remarks>
		/// 事件的完整类型引用。
		/// </remarks>
		public Type EventType { get; }

		/// <summary>
		/// 事件数据（对值类型事件进行了装箱）
		/// </summary>
		/// <remarks>
		/// 存储事件的实际数据，用于历史分析。
		/// 注意：会导致值类型的装箱，但仅在历史记录启用时发生。
		/// </remarks>
		public IEvent EventData { get; }

		/// <summary>
		/// 事件触发时间戳
		/// </summary>
		/// <remarks>
		/// 记录事件发布的精确时间，用于时序分析。
		/// </remarks>
		public DateTime Timestamp { get; }

		/// <summary>
		/// 处理此事件的处理器信息列表
		/// </summary>
		/// <remarks>
		/// 记录所有处理了此事件的处理器，包括它们的所有者类型和方法名。
		/// 用于跟踪事件的处理流程和诊断问题。
		/// </remarks>
		public List<string> HandlerInfo { get; } = new List<string>();

		/// <summary>
		/// 事件处理的总时间（毫秒）
		/// </summary>
		/// <remarks>
		/// 从事件发布到所有处理器执行完成的总时间。
		/// 用于性能分析和识别处理缓慢的事件。
		/// </remarks>
		public double ProcessingTimeMs { get; set; }

		/// <summary>
		/// 创建新的事件历史记录条目
		/// </summary>
		/// <param name="eventType">事件类型</param>
		/// <param name="eventData">事件数据</param>
		/// <remarks>
		/// 初始化新的历史记录条目，记录事件的基本信息。
		/// </remarks>
		public EventHistoryEntry(Type eventType, IEvent eventData)
		{
			EventType = eventType;
			EventName = eventType.Name;
			EventData = eventData;
			Timestamp = DateTime.Now;
		}

		/// <summary>
		/// 返回事件的可读字符串表示
		/// </summary>
		/// <returns>格式化的事件信息字符串</returns>
		/// <remarks>
		/// 提供事件的简要摘要，包括时间戳、名称、处理时间和处理器数量。
		/// </remarks>
		public override string ToString()
		{
			return $"[{Timestamp:HH:mm:ss.fff}] {EventName} - {ProcessingTimeMs:F3}ms - {HandlerInfo.Count} 个处理器";
		}
	}

	/// <summary>
	/// 事件优先级枚举 - 提供语义化的优先级值
	/// </summary>
	/// <remarks>
	/// 使用预定义的优先级级别，使代码更具可读性。
	/// 避免使用魔术数字作为优先级值。
	/// </remarks>
	public enum Priority
	{
		/// <summary>最高优先级 - 首先处理</summary>
		Highest = -100,
		/// <summary>高优先级</summary>
		High = -50,
		/// <summary>普通优先级 - 默认值</summary>
		Normal = 0,
		/// <summary>低优先级</summary>
		Low = 50,
		/// <summary>最低优先级 - 最后处理</summary>
		Lowest = 100
	}

	#endregion

	#region 委托和事件定义

	/// <summary>
	/// 事件已经发布的委托
	/// </summary>
	/// <param name="eventType">已发布事件的类型</param>
	/// <param name="eventData">事件数据</param>
	/// <remarks>
	/// 当事件被发布时触发，在任何处理器执行之前。
	/// </remarks>
	public delegate void EventPublishedDelegate(Type eventType, IEvent eventData);

	/// <summary>
	/// 事件处理完成的委托
	/// </summary>
	/// <param name="eventType">已处理事件的类型</param>
	/// <param name="eventData">事件数据</param>
	/// <param name="processingTimeMs">处理时间（毫秒）</param>
	/// <remarks>
	/// 当事件被所有处理器处理完成后触发。
	/// 包括事件处理的总时间，用于性能监控。
	/// </remarks>
	public delegate void EventHandledDelegate(Type eventType, IEvent eventData, double processingTimeMs);

	/// <summary>
	/// 当事件发布时触发的事件
	/// </summary>
	/// <remarks>
	/// 在事件被发布时立即触发，在处理器执行前。
	/// 可用于全局事件监控、日志记录和调试。
	/// </remarks>
	public event EventPublishedDelegate EventPublished;

	/// <summary>
	/// 当事件处理完成时触发的事件
	/// </summary>
	/// <remarks>
	/// 在事件的所有处理器执行完毕后触发。
	/// 可用于统计处理时间、性能监控和链式事件处理。
	/// </remarks>
	public event EventHandledDelegate EventHandled;

	#endregion

	#region 字段和属性

	/// <summary>
	/// 泛型事件处理器字典 - 存储类型安全的事件订阅
	/// </summary>
	/// <remarks>
	/// 使用Type作为键，存储泛型处理器列表。值类型是object，
	/// 但实际上是List<HandlerItem<TEvent>>，需要在使用时进行转换。
	/// 这种设计允许在编译时不知道具体事件类型的情况下存储任何事件类型的处理器。
	/// </remarks>
	private readonly Dictionary<Type, object> _subscriptions = new Dictionary<Type, object>();

	/// <summary>
	/// 事件过滤器字典 - 存储每个处理器的过滤条件
	/// </summary>
	/// <remarks>
	/// 使用Type作为键，存储处理器到过滤器的映射。
	/// 值类型是object，但实际上是Dictionary<Action<TEvent>, EventFilter<TEvent>>。
	/// 过滤器允许根据事件数据选择性地执行处理器。
	/// </remarks>
	private readonly Dictionary<Type, object> _filters = new Dictionary<Type, object>();

	/// <summary>
	/// 事件历史记录 - 存储最近触发的事件信息
	/// </summary>
	/// <remarks>
	/// 按时间倒序存储事件记录，新事件插入到列表开头。
	/// 用于调试、性能分析和事件流可视化。
	/// </remarks>
	private readonly List<EventHistoryEntry> _eventHistory = new();

	/// <summary>
	/// 是否启用调试模式
	/// </summary>
	/// <remarks>
	/// 启用后会输出详细的日志信息，有助于跟踪事件流，
	/// 但会降低性能，建议仅在开发阶段启用。
	/// </remarks>
	private bool _debugMode = false;

	/// <summary>
	/// 是否启用事件历史记录
	/// </summary>
	/// <remarks>
	/// 启用后会记录所有事件的触发和处理情况，
	/// 可用于回溯分析，但会占用额外内存。
	/// </remarks>
	private bool _enableHistory = false;

	/// <summary>
	/// 历史记录最大条目数
	/// </summary>
	/// <remarks>
	/// 限制历史记录长度以避免内存占用过多。
	/// 默认值为3000，适合大多数游戏场景。
	/// </remarks>
	private int _maxHistorySize = 3000;

	/// <summary>
	/// 记录已经添加了TreeExiting监听的节点集合
	/// </summary>
	/// <remarks>
	/// 避免重复添加节点销毁监听器，确保每个节点只被注册一次。
	/// 当节点销毁时，自动清理与该节点相关的所有事件订阅。
	/// </remarks>
	private readonly HashSet<Node> _registeredNodes = new HashSet<Node>();

	/// <summary>
	/// 用于跟踪已发布事件的集合，防止循环发布
	/// </summary>
	/// <remarks>
	/// 临时存储正在处理的事件类型，以检测和防止事件循环引用，
	/// 避免无限递归和堆栈溢出。
	/// </remarks>
	private readonly HashSet<Type> _publishingEvents = new HashSet<Type>();

	/// <summary>
	/// 最大同时处理的事件嵌套深度
	/// </summary>
	/// <remarks>
	/// 限制事件嵌套调用的最大深度，防止潜在的无限递归。
	/// 如果超过此深度，将输出错误并中止事件处理。
	/// </remarks>
	private const int MaxEventNestingDepth = 10;

	/// <summary>
	/// 是否启用处理器异常保护
	/// </summary>
	/// <remarks>
	/// 启用时，单个处理器的异常不会中断整个事件处理流程。
	/// 禁用时，异常会向上传播，可能导致应用崩溃，但便于调试。
	/// </remarks>
	private bool _exceptionProtection = true;

	/// <summary>
	/// 去抖动计时器字典
	/// </summary>
	/// <remarks>
	/// 存储每种事件类型的去抖动计时器。
	/// 用于减少高频事件的处理次数，优化性能并降低资源消耗。
	/// </remarks>
	private readonly Dictionary<Type, SceneTreeTimer> _debounceTimers = new Dictionary<Type, SceneTreeTimer>();

	/// <summary>
	/// 去抖动处理器字典
	/// </summary>
	/// <remarks>
	/// 存储事件类型到对应超时处理器的映射。
	/// 确保能够正确地移除先前的计时器回调，避免信号连接错误。
	/// </remarks>
	private readonly Dictionary<Type, Action> _debounceHandlers = new Dictionary<Type, Action>();

	/// <summary>
	/// 调试模式开关
	/// </summary>
	/// <remarks>
	/// 控制是否输出调试日志信息。
	/// 启用后，事件总线会记录详细的操作日志，便于跟踪事件流程。
	/// 在生产环境中建议禁用以提高性能。
	/// </remarks>
	public bool DebugMode
	{
		get => _debugMode;
		set => _debugMode = value;
	}

	/// <summary>
	/// 历史记录开关
	/// </summary>
	/// <remarks>
	/// 控制是否记录事件历史。
	/// 启用后，事件总线会保存所有事件的处理记录，
	/// 便于回溯分析，但会消耗额外内存。
	/// </remarks>
	public bool EnableHistory
	{
		get => _enableHistory;
		set => _enableHistory = value;
	}

	/// <summary>
	/// 历史记录最大长度
	/// </summary>
	/// <remarks>
	/// 控制历史记录的最大条目数。
	/// 设置较高的值可记录更多历史，但会占用更多内存。
	/// 设置后会自动裁剪过长的历史记录。
	/// </remarks>
	public int MaxHistorySize
	{
		get => _maxHistorySize;
		set
		{
			_maxHistorySize = Math.Max(100, value); // 确保至少保留100条记录
			TrimHistoryIfNeeded();
		}
	}

	/// <summary>
	/// 是否启用处理器异常保护
	/// </summary>
	/// <remarks>
	/// 控制事件处理器异常的行为。
	/// 启用时，单个处理器的异常不会中断整体事件处理流程。
	/// 禁用时，任何处理器的异常都会向上传播，可能导致应用崩溃。
	/// 开发时禁用有助于发现和修复错误，生产环境建议启用以提高稳定性。
	/// </remarks>
	public bool ExceptionProtection
	{
		get => _exceptionProtection;
		set => _exceptionProtection = value;
	}

    #endregion

    #region 事件订阅方法

    /// <summary>
    /// 订阅泛型事件
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="handler">事件处理委托</param>
    /// <param name="metadata">事件元数据</param>
    /// <param name="filter">事件过滤器（可选）</param>
    /// <remarks>
    /// 注册一个处理器来响应特定类型的事件。
    /// 可以通过元数据设置处理器的优先级和一次性标志。
    /// 可以通过过滤器选择性地处理事件。
    /// </remarks>
    public void Subscribe<TEvent>(Action<TEvent> handler, EventMetadata metadata = default, EventFilter<TEvent> filter = null) where TEvent : IEvent
	{
		Type eventType = typeof(TEvent);

		// 获取或创建处理器列表
		var handlers = GetOrCreateHandlerList<TEvent>();

		// 创建处理器项
		var handlerItem = new HandlerItem<TEvent>(handler, metadata);

		// 添加处理器到列表
		handlers.Add(handlerItem);

		// 按优先级排序
		handlers.Sort((a, b) => a.Metadata.Priority.CompareTo(b.Metadata.Priority));

		// 如果提供了过滤器，则存储它
		if (filter != null)
		{
			var filterDict = GetOrCreateFilterDictionary<TEvent>();
			filterDict[handler] = filter;
		}

		// 自动注册节点清理
		if (handler.Target is Node node)
		{
			AutoRegisterNodeCleanup(node);
		}

		DebugLog($"已订阅事件: {eventType.Name}, 优先级: {metadata.Priority}, 一次性: {metadata.OneTime}");
	}

	/// <summary>
	/// 使用枚举优先级订阅泛型事件
	/// </summary>
	/// <typeparam name="TEvent">事件类型</typeparam>
	/// <param name="handler">事件处理委托</param>
	/// <param name="priority">优先级枚举</param>
	/// <param name="oneTime">是否为一次性订阅</param>
	/// <param name="filter">事件过滤器（可选）</param>
	/// <remarks>
	/// 使用预定义的优先级枚举值订阅事件，提高代码可读性。
	/// 提供了更语义化的优先级设置方式。
	/// </remarks>
	public void Subscribe<TEvent>(Action<TEvent> handler, Priority priority, bool oneTime = false, EventFilter<TEvent> filter = null) where TEvent : IEvent
	{
		var metadata = new EventMetadata((int)priority, oneTime);
		Subscribe(handler, metadata, filter);
	}

	/// <summary>
	/// 订阅一次性泛型事件
	/// </summary>
	/// <typeparam name="TEvent">事件类型</typeparam>
	/// <param name="handler">事件处理委托</param>
	/// <param name="priority">优先级</param>
	/// <param name="filter">事件过滤器（可选）</param>
	/// <remarks>
	/// 注册一个只执行一次的事件处理器。
	/// 处理器在首次执行后会自动被移除。
	/// 适用于一次性通知、引导提示等场景。
	/// </remarks>
	public void SubscribeOnce<TEvent>(Action<TEvent> handler, int priority = 0, EventFilter<TEvent> filter = null) where TEvent : IEvent
	{
		var metadata = new EventMetadata(priority, true);
		Subscribe(handler, metadata, filter);
	}

	/// <summary>
	/// 取消订阅泛型事件
	/// </summary>
	/// <typeparam name="TEvent">事件类型</typeparam>
	/// <param name="handler">要移除的处理委托</param>
	/// <returns>是否成功取消订阅</returns>
	/// <remarks>
	/// 移除先前注册的事件处理器。
	/// 如果处理器不存在，返回false。
	/// 当组件不再需要响应某类事件时调用。
	/// </remarks>
	public bool Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : IEvent
	{
		Type eventType = typeof(TEvent);

		if (!_subscriptions.TryGetValue(eventType, out var existingList))
		{
			DebugLog($"无法取消订阅: 事件 {eventType.Name} 未找到");
			return false;
		}

		var handlers = (List<HandlerItem<TEvent>>)existingList;
		bool removed = false;

		// 找到并移除匹配的处理器
		for (int i = handlers.Count - 1; i >= 0; i--)
		{
			if (handlers[i].Handler.Equals(handler))
			{
				handlers.RemoveAt(i);
				removed = true;
				break;
			}
		}

		// 如果有过滤器，也移除过滤器
		if (_filters.TryGetValue(eventType, out var filtersObj))
		{
			var filterDict = (Dictionary<Action<TEvent>, EventFilter<TEvent>>)filtersObj;
			filterDict.Remove(handler);
		}

		if (removed)
		{
			DebugLog($"已取消订阅事件: {eventType.Name}");

			// 如果没有处理器了，清理列表
			if (handlers.Count == 0)
			{
				_subscriptions.Remove(eventType);
				_filters.Remove(eventType);
			}

			return true;
		}

		DebugLog($"无法取消订阅: 未找到事件 {eventType.Name} 的处理器");
		return false;
	}

	#endregion

	#region 事件发布方法

	/// <summary>
	/// 发布泛型事件
	/// </summary>
	/// <typeparam name="TEvent">事件类型</typeparam>
	/// <param name="eventData">事件数据</param>
	/// <remarks>
	/// 发布一个事件，通知所有订阅者。
	/// 会检测并防止事件循环引用。
	/// 所有处理器按优先级顺序执行。
	/// </remarks>
	public void Publish<TEvent>(TEvent eventData) where TEvent : IEvent
	{
		Type eventType = typeof(TEvent);

		// 防止循环发布
		if (_publishingEvents.Count >= MaxEventNestingDepth)
		{
			GD.PushError($"[事件总线] 发布事件 {eventType.Name} 时达到最大嵌套深度。" +
						 $"这可能表示存在无限递归。");
			return;
		}

		if (_publishingEvents.Contains(eventType))
		{
			DebugLog($"事件 {eventType.Name} 正在发布中 - 跳过以防止递归");
			return;
		}

		_publishingEvents.Add(eventType);

		try
		{
			PublishInternal(eventData);
		}
		finally
		{
			_publishingEvents.Remove(eventType);
		}
	}

	/// <summary>
	/// 内部事件发布实现
	/// </summary>
	/// <typeparam name="TEvent">事件类型</typeparam>
	/// <param name="eventData">事件数据</param>
	/// <remarks>
	/// 执行实际的事件发布逻辑。
	/// 通知所有订阅者并记录处理信息。
	/// 由公共Publish方法调用，受循环检测保护。
	/// </remarks>
	private void PublishInternal<TEvent>(TEvent eventData) where TEvent : IEvent
	{
		Type eventType = typeof(TEvent);

#if DEBUG
		var startTime = DateTime.Now;
#endif

		// 记录事件历史
		var historyEntry = RecordEvent(eventType, eventData);

		// 触发事件发布事件
		EventPublished?.Invoke(eventType, eventData);

		// 获取处理器列表
		if (!_subscriptions.TryGetValue(eventType, out var existingList))
		{
			// 没有处理器，记录历史并完成
#if DEBUG
			var endTime = DateTime.Now;
			double processingTimeMs = (endTime - startTime).TotalMilliseconds;

			if (historyEntry != null)
			{
				historyEntry.ProcessingTimeMs = processingTimeMs;
			}

			EventHandled?.Invoke(eventType, eventData, processingTimeMs);
#endif

			DebugLog($"事件 {eventType.Name} 没有订阅者");
			return;
		}

		var handlers = (List<HandlerItem<TEvent>>)existingList;

		// 获取过滤器字典
		Dictionary<Action<TEvent>, EventFilter<TEvent>> filterDict = null;
		if (_filters.TryGetValue(eventType, out var filtersObj))
		{
			filterDict = (Dictionary<Action<TEvent>, EventFilter<TEvent>>)filtersObj;
		}

		// 过滤无效处理程序
		FilterInvalidHandlers(eventType, handlers);

		// 创建要移除的一次性订阅列表
		var oneTimeHandlers = new List<Action<TEvent>>();

		// 调用所有处理器
		foreach (var item in handlers)
		{
			if (!item.IsActive) continue;

			// 应用过滤器
			bool filtered = false;
			if (filterDict != null && filterDict.TryGetValue(item.Handler, out var filter))
			{
				try
				{
					if (!filter(eventData))
					{
						filtered = true;
						RecordHandlerExecution(historyEntry, item, true);
						continue; // 过滤掉此处理器
					}
				}
				catch (Exception ex)
				{
					GD.PushError($"[事件总线] 事件 {eventType.Name} 的过滤器中发生异常: {ex.Message}");
					if (_exceptionProtection)
					{
						continue; // 如果启用了异常保护，跳过此处理器
					}
					throw; // 否则重新抛出异常
				}
			}

			// 执行处理器
			try
			{
				item.Handler(eventData);
				RecordHandlerExecution(historyEntry, item, false);
			}
			catch (Exception ex)
			{
				// 记录异常
				GD.PushError($"[事件总线] 事件 {eventType.Name} 的处理器中发生异常: {ex.Message}");
				if (historyEntry != null)
				{
					historyEntry.HandlerInfo.Add($"{item.OwnerType?.Name}.{item.HandlerName} - 错误: {ex.Message}");
				}

				if (!_exceptionProtection)
				{
					throw; // 如果禁用了异常保护，重新抛出异常
				}
			}

			// 如果是一次性订阅，添加到移除列表
			if (item.Metadata.OneTime)
			{
				oneTimeHandlers.Add(item.Handler);
			}
		}

		// 移除所有一次性订阅
		foreach (var handler in oneTimeHandlers)
		{
			Unsubscribe<TEvent>(handler);
		}

		// 计算处理时间
#if DEBUG
		var endProcessTime = DateTime.Now;
		double processingTime = (endProcessTime - startTime).TotalMilliseconds;

		if (historyEntry != null)
		{
			historyEntry.ProcessingTimeMs = processingTime;
		}

		// 触发事件处理完成事件
		EventHandled?.Invoke(eventType, eventData, processingTime);

		DebugLog($"事件 {eventType.Name} 处理完成，耗时 {processingTime:F3}ms，" +
				 $"执行了 {handlers.Count - oneTimeHandlers.Count} 个处理器");
#endif
	}

	/// <summary>
	/// 使用去抖动机制发布事件
	/// </summary>
	/// <typeparam name="TEvent">事件类型</typeparam>
	/// <param name="eventData">事件数据</param>
	/// <param name="debounceSeconds">去抖动时间（秒）</param>
	/// <remarks>
	/// 实现事件发布的去抖动(防抖)功能，在高频触发场景中特别有用。
	/// 在指定的时间窗口内，如果同一类型的事件被多次发布，只有最后一次会被实际处理。
	/// 典型应用场景包括UI元素拖拽、输入处理、窗口调整大小等。
	/// 去抖动可以显著减少不必要的处理，提高系统响应性并降低资源消耗。
	/// </remarks>
	public void PublishDebounced<TEvent>(TEvent eventData, float debounceSeconds = 0.1f) where TEvent : IEvent
	{
		// 获取事件类型
		Type eventType = typeof(TEvent);

		DebugLog($"事件去抖动: {eventType.Name}，延迟 {debounceSeconds}秒");

		// 如果已存在此类型事件的计时器，取消之前的计时
		if (_debounceTimers.TryGetValue(eventType, out var existingTimer))
		{
			// 移除之前计时器的超时回调
			if (_debounceHandlers.TryGetValue(eventType, out var handler))
			{
				existingTimer.Timeout -= handler;
				_debounceHandlers.Remove(eventType);
			}
		}

		// 创建新的超时处理函数
		Action timeoutHandler = () =>
		{
			// 实际发布事件
			Publish(eventData);
			// 移除计时器引用和处理器
			_debounceTimers.Remove(eventType);
			_debounceHandlers.Remove(eventType);
		};

		// 保存处理函数引用
		_debounceHandlers[eventType] = timeoutHandler;

		// 创建新的计时器，延迟指定的时间
		SceneTreeTimer timer = GetTree().CreateTimer(debounceSeconds);
		_debounceTimers[eventType] = timer;

		// 在计时器超时后发布事件
		timer.Timeout += timeoutHandler;
	}

	#endregion

	#region 延迟事件处理

	/// <summary>
	/// 延迟发布泛型事件
	/// </summary>
	/// <typeparam name="TEvent">事件类型</typeparam>
	/// <param name="eventData">事件数据</param>
	/// <remarks>
	/// 使用Godot的延迟调用机制发布事件。
	/// 在当前帧的脚本处理完成后、绘制前执行。
	/// 等效于GDScript的call_deferred。
	/// </remarks>
	public void PublishDeferred<TEvent>(TEvent eventData) where TEvent : IEvent
	{
		DebugLog($"已安排延迟事件: {typeof(TEvent).Name}");
		CallDeferred(nameof(Publish), Variant.From(eventData));
	}

	/// <summary>
	/// 在下一帧处理时发布泛型事件
	/// </summary>
	/// <typeparam name="TEvent">事件类型</typeparam>
	/// <param name="eventData">事件数据</param>
	/// <remarks>
	/// 在下一个游戏逻辑帧开始时发布事件。
	/// 使用Godot的ProcessFrame信号实现。
	/// 适用于需要在下一帧处理的事件。
	/// </remarks>
	public void PublishOnNextProcess<TEvent>(TEvent eventData) where TEvent : IEvent
	{
		DebugLog($"已安排下一逻辑帧事件: {typeof(TEvent).Name}");

		void OnNextProcess()
		{
			Publish(eventData);
			GetTree().ProcessFrame -= OnNextProcess;
		}

		GetTree().ProcessFrame += OnNextProcess;
	}

	/// <summary>
	/// 在下一帧物理处理时发布泛型事件
	/// </summary>
	/// <typeparam name="TEvent">事件类型</typeparam>
	/// <param name="eventData">事件数据</param>
	/// <remarks>
	/// 在下一个物理帧开始时发布事件。
	/// 使用Godot的PhysicsFrame信号实现。
	/// 适用于需要在物理处理前发布的事件。
	/// </remarks>
	public void PublishOnNextPhysicsProcess<TEvent>(TEvent eventData) where TEvent : IEvent
	{
		DebugLog($"已安排下一物理帧事件: {typeof(TEvent).Name}");

		void OnNextPhysicsProcess()
		{
			Publish(eventData);
			GetTree().PhysicsFrame -= OnNextPhysicsProcess;
		}

		GetTree().PhysicsFrame += OnNextPhysicsProcess;
	}

	/// <summary>
	/// 在指定延迟后发布泛型事件
	/// </summary>
	/// <typeparam name="TEvent">事件类型</typeparam>
	/// <param name="eventData">事件数据</param>
	/// <param name="delaySeconds">延迟秒数</param>
	/// <remarks>
	/// 在指定的延迟时间后发布事件。
	/// 使用Godot的计时器实现，比C#的Timer更适合游戏上下文。
	/// 适用于需要延时触发的事件，如技能冷却、倒计时等。
	/// </remarks>
	public void PublishAfterDelay<TEvent>(TEvent eventData, float delaySeconds) where TEvent : IEvent
	{
		DebugLog($"已安排延时事件: {typeof(TEvent).Name}, 延迟: {delaySeconds}秒");

		SceneTreeTimer timer = GetTree().CreateTimer(delaySeconds);
		timer.Timeout += () => Publish(eventData);
	}

	/// <summary>
	/// 创建一个可以调度多个事件的事件序列
	/// </summary>
	/// <returns>事件序列构建器</returns>
	/// <remarks>
	/// 用于创建复杂的事件序列，如对话、过场动画等。
	/// 提供流畅的API进行序列构建，支持事件间的延迟。
	/// </remarks>
	public EventSequenceBuilder CreateEventSequence()
	{
		return new EventSequenceBuilder(this);
	}

	/// <summary>
	/// 事件序列构建器 - 用于按顺序调度多个事件
	/// </summary>
	/// <remarks>
	/// 提供流畅的API用于构建和执行事件序列。
	/// 允许添加事件和延迟，创建时间线。
	/// 支持同步和异步执行。
	/// </remarks>
	public class EventSequenceBuilder
	{
		/// <summary>
		/// 事件总线实例引用
		/// </summary>
		private readonly EventBus _eventBus;

		/// <summary>
		/// 序列中的事件列表，包含延迟、数据和类型
		/// </summary>
		private readonly List<(float delay, object eventData, Type eventType)> _sequence = new();

		/// <summary>
		/// 当前累计的延迟时间
		/// </summary>
		private float _totalDelay = 0;

		/// <summary>
		/// 创建事件序列构建器
		/// </summary>
		/// <param name="eventBus">事件总线实例</param>
		/// <remarks>
		/// 初始化新的序列构建器，关联到指定的事件总线。
		/// </remarks>
		internal EventSequenceBuilder(EventBus eventBus)
		{
			_eventBus = eventBus;
		}

		/// <summary>
		/// 添加延迟到序列
		/// </summary>
		/// <param name="seconds">延迟秒数</param>
		/// <returns>构建器实例，用于链式调用</returns>
		/// <remarks>
		/// 在序列中添加等待时间。
		/// 累计延迟将应用于下一个添加的事件。
		/// </remarks>
		public EventSequenceBuilder Wait(float seconds)
		{
			_totalDelay += seconds;
			return this;
		}

		/// <summary>
		/// 添加泛型事件到序列
		/// </summary>
		/// <typeparam name="TEvent">事件类型</typeparam>
		/// <param name="eventData">事件数据</param>
		/// <returns>构建器实例，用于链式调用</returns>
		/// <remarks>
		/// 向序列添加一个事件，使用当前累计的延迟时间。
		/// 添加事件后不会清除累计延迟，需要手动重置。
		/// </remarks>
		public EventSequenceBuilder Then<TEvent>(TEvent eventData) where TEvent : IEvent
		{
			_sequence.Add((_totalDelay, eventData, typeof(TEvent)));
			return this;
		}

		/// <summary>
		/// 开始执行事件序列
		/// </summary>
		/// <remarks>
		/// 按照指定的顺序和延迟依次触发所有事件。
		/// 使用Godot的计时器实现延迟。
		/// </remarks>
		public void Start()
		{
			foreach (var (delay, eventData, eventType) in _sequence)
			{
				if (delay <= 0)
				{
					PublishEvent(eventData, eventType);
				}
				else
				{
					PublishEventWithDelay(eventData, eventType, delay);
				}
			}
		}

		/// <summary>
		/// 异步启动序列
		/// </summary>
		/// <returns>表示异步操作的任务</returns>
		/// <remarks>
		/// 异步执行序列，可以await等待完成。
		/// 适用于需要等待整个序列完成的场景。
		/// </remarks>
		public async Task StartAsync()
		{
			foreach (var (delay, eventData, eventType) in _sequence)
			{
				if (delay > 0)
				{
					await Task.Delay(TimeSpan.FromSeconds(delay));
				}

				PublishEvent(eventData, eventType);
			}
		}

		/// <summary>
		/// 使用反射发布事件
		/// </summary>
		/// <param name="eventData">事件数据</param>
		/// <param name="eventType">事件类型</param>
		/// <remarks>
		/// 内部辅助方法，通过反射调用适当的泛型Publish方法。
		/// </remarks>
		private void PublishEvent(object eventData, Type eventType)
		{
			// 使用泛型方法进行事件发布
			var method = typeof(EventBus).GetMethod("Publish").MakeGenericMethod(eventType);
			method.Invoke(_eventBus, new[] { eventData });
		}

		/// <summary>
		/// 使用反射延迟发布事件
		/// </summary>
		/// <param name="eventData">事件数据</param>
		/// <param name="eventType">事件类型</param>
		/// <param name="delay">延迟时间（秒）</param>
		/// <remarks>
		/// 内部辅助方法，通过反射调用适当的泛型PublishAfterDelay方法。
		/// </remarks>
		private void PublishEventWithDelay(object eventData, Type eventType, float delay)
		{
			var method = typeof(EventBus).GetMethod("PublishAfterDelay").MakeGenericMethod(eventType);
			method.Invoke(_eventBus, new[] { eventData, delay });
		}
	}

	#endregion

	#region 历史记录管理

	/// <summary>
	/// 获取事件历史记录
	/// </summary>
	/// <returns>只读的事件历史记录列表</returns>
	/// <remarks>
	/// 返回当前保存的所有事件历史记录。
	/// 结果是只读的，不能修改。
	/// 记录按时间倒序排列，最新的事件在前。
	/// </remarks>
	public IReadOnlyList<EventHistoryEntry> GetEventHistory()
	{
		return _eventHistory.AsReadOnly();
	}

	/// <summary>
	/// 清除事件历史记录
	/// </summary>
	/// <remarks>
	/// 清空所有历史记录，释放内存。
	/// 用于重置状态或减少内存占用。
	/// </remarks>
	public void ClearEventHistory()
	{
		_eventHistory.Clear();
		DebugLog("事件历史记录已清空");
	}

	/// <summary>
	/// 获取特定类型事件的历史记录
	/// </summary>
	/// <typeparam name="TEvent">事件类型</typeparam>
	/// <returns>指定类型的事件历史记录列表</returns>
	/// <remarks>
	/// 筛选并返回特定类型的事件历史。
	/// 用于分析特定事件的触发模式和性能。
	/// </remarks>
	public IReadOnlyList<EventHistoryEntry> GetEventHistoryByType<TEvent>() where TEvent : IEvent
	{
		Type eventType = typeof(TEvent);
		return _eventHistory.Where(e => e.EventType == eventType).ToList().AsReadOnly();
	}

	/// <summary>
	/// 获取最近的N条事件历史
	/// </summary>
	/// <param name="count">需要获取的历史条目数量</param>
	/// <returns>最近的事件历史记录列表</returns>
	/// <remarks>
	/// 返回最新的指定数量的事件历史。
	/// 用于查看最近发生的事件。
	/// </remarks>
	public IReadOnlyList<EventHistoryEntry> GetRecentEventHistory(int count)
	{
		count = Math.Min(count, _eventHistory.Count);
		return _eventHistory.Take(count).ToList().AsReadOnly();
	}

	/// <summary>
	/// 在事件触发时记录历史
	/// </summary>
	/// <param name="eventType">事件类型</param>
	/// <param name="eventData">事件数据</param>
	/// <returns>新创建的历史记录条目，如果历史记录未启用则返回null</returns>
	/// <remarks>
	/// 内部方法，创建并记录新的事件历史条目。
	/// 如果历史记录功能未启用，直接返回null。
	/// </remarks>
	private EventHistoryEntry RecordEvent(Type eventType, IEvent eventData)
	{
		if (!_enableHistory)
			return null;

		var entry = new EventHistoryEntry(eventType, eventData);
		_eventHistory.Insert(0, entry);

		TrimHistoryIfNeeded();
		return entry;
	}

	/// <summary>
	/// 记录事件处理器执行信息
	/// </summary>
	/// <typeparam name="TEvent">事件类型</typeparam>
	/// <param name="entry">历史记录条目</param>
	/// <param name="handler">处理器</param>
	/// <param name="filtered">是否被过滤器过滤</param>
	/// <remarks>
	/// 内部方法，记录处理器的执行情况。
	/// 包括处理器所属的类型、方法名和是否被过滤。
	/// </remarks>
	private void RecordHandlerExecution<TEvent>(EventHistoryEntry entry, HandlerItem<TEvent> handler, bool filtered) where TEvent : IEvent
	{
		if (entry == null || !_enableHistory)
			return;

		string handlerInfo = $"{handler.OwnerType?.Name ?? "未知"}.{handler.HandlerName}";

		if (filtered)
			handlerInfo += " (已过滤)";

		entry.HandlerInfo.Add(handlerInfo);
	}

	/// <summary>
	/// 保持历史记录在最大长度内
	/// </summary>
	/// <remarks>
	/// 内部方法，当历史记录超过最大长度时，删除最旧的条目。
	/// 避免历史记录占用过多内存。
	/// </remarks>
	private void TrimHistoryIfNeeded()
	{
		if (_eventHistory.Count > _maxHistorySize)
		{
			_eventHistory.RemoveRange(_maxHistorySize, _eventHistory.Count - _maxHistorySize);
		}
	}

	#endregion

	#region 订阅管理

	/// <summary>
	/// 取消对象的所有事件订阅
	/// </summary>
	/// <param name="target">目标对象</param>
	/// <remarks>
	/// 移除与指定对象相关的所有事件处理器。
	/// 用于在对象销毁前清理订阅，避免内存泄漏和空引用异常。
	/// </remarks>
	public void UnsubscribeAll(object target)
	{
		if (target == null) return;

		// 遍历所有事件类型
		foreach (var eventType in _subscriptions.Keys.ToList())
		{
			// 使用泛型方法进行清理
			var methodInfo = typeof(EventBus).GetMethod("RemoveAllHandlersForTarget",
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			var genericMethod = methodInfo.MakeGenericMethod(eventType);
			bool removed = (bool)genericMethod.Invoke(this, new[] { target });

			if (removed)
			{
				DebugLog($"已取消 {target.GetType().Name} 对事件的所有订阅: {eventType.Name}");
			}
		}
	}

	/// <summary>
	/// 获取对象订阅的所有事件类型
	/// </summary>
	/// <param name="target">目标对象</param>
	/// <returns>对象订阅的事件类型列表</returns>
	/// <remarks>
	/// 收集对象订阅的所有事件类型。
	/// 用于调试和分析组件的事件依赖。
	/// </remarks>
	public List<Type> GetSubscribedEventTypes(object target)
	{
		if (target == null) return new List<Type>();

		List<Type> result = new List<Type>();

		foreach (var eventType in _subscriptions.Keys.ToList())
		{
			// 检查对象是否订阅了此事件
			var methodInfo = typeof(EventBus).GetMethod("HasSubscription",
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			var genericMethod = methodInfo.MakeGenericMethod(eventType);
			bool subscribed = (bool)genericMethod.Invoke(this, new[] { target });

			if (subscribed)
			{
				result.Add(eventType);
			}
		}

		return result;
	}

	/// <summary>
	/// 获取事件的订阅者数量
	/// </summary>
	/// <typeparam name="TEvent">事件类型</typeparam>
	/// <returns>订阅者数量</returns>
	/// <remarks>
	/// 计算特定事件类型当前的订阅者数量。
	/// 用于分析事件的使用情况和依赖性。
	/// </remarks>
	public int GetSubscriptionCount<TEvent>() where TEvent : IEvent
	{
		Type eventType = typeof(TEvent);

		if (!_subscriptions.TryGetValue(eventType, out var handlers))
		{
			return 0;
		}

		return ((List<HandlerItem<TEvent>>)handlers).Count;
	}

	/// <summary>
	/// 清除所有事件订阅
	/// </summary>
	/// <remarks>
	/// 移除所有事件类型的所有订阅。
	/// 用于重置事件总线或在系统关闭时清理资源。
	/// </remarks>
	public void ClearAllSubscriptions()
	{
		_subscriptions.Clear();
		_filters.Clear();
		DebugLog("已清除所有订阅");
	}

	/// <summary>
	/// 移除特定目标的所有处理器
	/// </summary>
	/// <typeparam name="TEvent">事件类型</typeparam>
	/// <param name="target">目标对象</param>
	/// <returns>是否移除了任何处理器</returns>
	/// <remarks>
	/// 内部泛型辅助方法，移除与特定对象相关的所有处理器。
	/// 由UnsubscribeAll方法通过反射调用。
	/// </remarks>
	private bool RemoveAllHandlersForTarget<TEvent>(object target) where TEvent : IEvent
	{
		Type eventType = typeof(TEvent);

		if (!_subscriptions.TryGetValue(eventType, out var handlers))
		{
			return false;
		}

		var typedHandlers = (List<HandlerItem<TEvent>>)handlers;
		int originalCount = typedHandlers.Count;

		// 移除与目标相关的处理器
		typedHandlers.RemoveAll(h => h.Handler.Target == target);

		// 同时清理过滤器
		if (_filters.TryGetValue(eventType, out var filtersObj))
		{
			var filterDict = (Dictionary<Action<TEvent>, EventFilter<TEvent>>)filtersObj;
			var keysToRemove = filterDict.Keys.Where(k => k.Target == target).ToList();

			foreach (var key in keysToRemove)
			{
				filterDict.Remove(key);
			}

			// 如果过滤器字典为空，移除它
			if (filterDict.Count == 0)
			{
				_filters.Remove(eventType);
			}
		}

		// 如果处理器列表为空，移除它
		if (typedHandlers.Count == 0)
		{
			_subscriptions.Remove(eventType);
		}

		return typedHandlers.Count < originalCount;
	}

	/// <summary>
	/// 检查对象是否订阅了事件
	/// </summary>
	/// <typeparam name="TEvent">事件类型</typeparam>
	/// <param name="target">目标对象</param>
	/// <returns>如果对象订阅了事件则为true，否则为false</returns>
	/// <remarks>
	/// 内部泛型辅助方法，检查对象是否订阅了特定类型的事件。
	/// 由GetSubscribedEventTypes方法通过反射调用。
	/// </remarks>
	private bool HasSubscription<TEvent>(object target) where TEvent : IEvent
	{
		Type eventType = typeof(TEvent);

		if (!_subscriptions.TryGetValue(eventType, out var handlers))
		{
			return false;
		}

		var typedHandlers = (List<HandlerItem<TEvent>>)handlers;
		return typedHandlers.Any(h => h.Handler.Target == target);
	}

	#endregion

	#region 自动清理被销毁节点

	/// <summary>
	/// 检查处理程序是否有效
	/// </summary>
	/// <param name="handler">要检查的委托处理器</param>
	/// <returns>如果处理器有效则返回true，否则返回false</returns>
	/// <remarks>
	/// 判断一个事件处理器是否仍然有效，防止调用已销毁对象的方法。
	/// 对于节点类型的目标，检查节点是否已被销毁或正在排队等待销毁。
	/// 对于非节点目标或静态方法（无目标），视为始终有效。
	/// 这是防止"空引用"和"已销毁对象"异常的关键机制。
	/// </remarks>
	private bool IsHandlerValid(Delegate handler)
	{
		// 检查处理程序目标是否是Godot节点
		if (handler.Target is Node node)
		{
			// 对于节点目标，检查节点是否仍然有效且未被排队销毁
			return IsInstanceValid(node) && !node.IsQueuedForDeletion();
		}

		// 非节点目标或没有目标的处理程序（静态方法）被视为有效
		return true;
	}

	/// <summary>
	/// 当发现节点处理程序时自动注册监听其销毁事件
	/// </summary>
	/// <param name="node">要监控的节点</param>
	/// <remarks>
	/// 为节点添加TreeExiting事件监听，在节点被销毁时清理相关订阅。
	/// 避免节点销毁后事件处理器仍被调用导致的异常。
	/// 每个节点只注册一次。
	/// </remarks>
	private void AutoRegisterNodeCleanup(Node node)
	{
		if (node != null && !_registeredNodes.Contains(node))
		{
			_registeredNodes.Add(node);

			// 当节点即将从场景树中移除时，清理与之相关的所有事件处理程序
			node.TreeExiting += () =>
			{
				UnsubscribeAll(node);
				_registeredNodes.Remove(node);
				DebugLog($"已自动清理节点的事件处理器: {node.Name}");
			};

			DebugLog($"已自动注册节点用于清理: {node.Name}");
		}
	}

	/// <summary>
	/// 在发布前过滤掉无效的处理程序
	/// </summary>
	/// <typeparam name="TEvent">事件类型</typeparam>
	/// <param name="eventType">事件类型的Type对象</param>
	/// <param name="handlers">处理器列表</param>
	/// <remarks>
	/// 检查处理器列表中的每个处理器，标记那些目标对象已无效的处理器。
	/// 当无效处理器数量达到一定阈值时(超过总数的1/3或超过5个)，执行物理清理。
	/// 此方法采用惰性清理策略，避免在每次事件发布时都修改集合结构。
	/// 同时清理相关的过滤器，确保系统不会保留对已销毁对象的引用。
	/// </remarks>
	private void FilterInvalidHandlers<TEvent>(Type eventType, List<HandlerItem<TEvent>> handlers) where TEvent : IEvent
	{
		// 记录原始处理器数量
		int originalCount = handlers.Count;
		int invalidCount = 0;

		// 遍历并标记无效处理程序
		foreach (var item in handlers)
		{
			if (!IsHandlerValid(item.Handler))
			{
				// 将处理器标记为无效，而不是立即移除
				item.IsActive = false;
				invalidCount++;
				DebugLog($"标记事件 {eventType.Name} 的无效处理器: 目标已被销毁");
			}
		}

		// 只有当无效处理器数量较多时才执行物理清理
		// 这减少了频繁修改集合的开销
		if (invalidCount > handlers.Count / 3 || invalidCount > 5) // 超过1/3或5个处理程序无效时清理
		{
			// 移除所有被标记为无效的处理器
			handlers.RemoveAll(h => !h.IsActive);

			// 同步清理相应的过滤器
			if (_filters.TryGetValue(eventType, out var filtersObj))
			{
				var filterDict = (Dictionary<Action<TEvent>, EventFilter<TEvent>>)filtersObj;
				var keysToCheck = filterDict.Keys.ToList();

				// 检查并移除无效处理器的过滤器
				foreach (var key in keysToCheck)
				{
					if (!IsHandlerValid(key))
					{
						filterDict.Remove(key);
					}
				}

				// 如果过滤器字典为空，从容器中移除
				if (filterDict.Count == 0)
				{
					_filters.Remove(eventType);
				}
			}

			// 如果处理程序列表现在为空，从订阅字典中移除
			if (handlers.Count == 0)
			{
				_subscriptions.Remove(eventType);
			}

			DebugLog($"已移除事件 {eventType.Name} 的 {invalidCount} 个无效处理器");
		}
	}
	#endregion

	#region 事件系统分析

	/// <summary>
	/// 获取所有事件订阅的统计信息
	/// </summary>
	/// <returns>事件类型及其订阅者数量的字典</returns>
	/// <remarks>
	/// 收集并返回所有已注册事件类型的订阅者数量。
	/// 用于监控事件系统使用情况和负载分析。
	/// 键是事件类型名称，值是订阅者数量。
	/// </remarks>
	public Dictionary<string, int> GetSubscriptionCounts()
	{
		var results = new Dictionary<string, int>();

		foreach (var entry in _subscriptions)
		{
			Type eventType = entry.Key;
			object handlers = entry.Value;

			// 使用反射获取列表计数
			System.Collections.ICollection collection = handlers as System.Collections.ICollection;
			int count = collection?.Count ?? 0;

			results[eventType.Name] = count;
		}

		return results;
	}

	/// <summary>
	/// 获取处理时间超过阈值的事件列表
	/// </summary>
	/// <param name="thresholdMs">阈值（毫秒），默认16ms（一帧时间）</param>
	/// <returns>处理时间超过阈值的事件列表</returns>
	/// <remarks>
	/// 识别处理缓慢的事件，帮助发现性能瓶颈。
	/// 默认阈值16ms代表一个游戏帧，超过此值可能导致卡顿。
	/// 返回包含事件名称和平均处理时间的格式化字符串列表。
	/// </remarks>
	public List<string> GetSlowEvents(double thresholdMs = 16.0)
	{
		if (!EnableHistory || _eventHistory.Count == 0)
			return new List<string>();

		return _eventHistory
			.Where(h => h.ProcessingTimeMs > thresholdMs)
			.GroupBy(h => h.EventType.Name)
			.Select(g => $"{g.Key}: {g.Average(e => e.ProcessingTimeMs):F2}ms")
			.ToList();
	}

	/// <summary>
	/// 获取频繁触发的事件列表
	/// </summary>
	/// <param name="count">要返回的事件数量</param>
	/// <returns>按触发频率排序的事件列表</returns>
	/// <remarks>
	/// 识别系统中最频繁触发的事件。
	/// 可用于发现潜在的过度触发问题。
	/// 返回包含事件名称和触发次数的格式化字符串列表。
	/// </remarks>
	public List<string> GetFrequentEvents(int count = 5)
	{
		if (!EnableHistory || _eventHistory.Count == 0)
			return new List<string>();

		return _eventHistory
			.GroupBy(h => h.EventType.Name)
			.OrderByDescending(g => g.Count())
			.Take(count)
			.Select(g => $"{g.Key}: {g.Count()} 次")
			.ToList();
	}

	/// <summary>
	/// 获取无订阅者的事件列表
	/// </summary>
	/// <returns>已发布但无订阅者的事件类型列表</returns>
	/// <remarks>
	/// 识别系统中发布了但没有相应处理器的事件。
	/// 这可能表明代码中存在逻辑错误或遗留代码。
	/// </remarks>
	public List<string> GetOrphanedEvents()
	{
		if (!EnableHistory || _eventHistory.Count == 0)
			return new List<string>();

		var orphanedEvents = new List<string>();

		foreach (var entry in _eventHistory)
		{
			if (!_subscriptions.ContainsKey(entry.EventType))
			{
				if (!orphanedEvents.Contains(entry.EventName))
				{
					orphanedEvents.Add(entry.EventName);
				}
			}
		}

		return orphanedEvents;
	}

	#endregion

	#region 辅助方法

	/// <summary>
	/// 获取或创建指定事件类型的处理器列表
	/// </summary>
	/// <typeparam name="TEvent">事件类型</typeparam>
	/// <returns>与事件类型关联的处理器列表</returns>
	/// <remarks>
	/// 这是一个内部泛型辅助方法，用于获取特定事件类型的处理器列表。
	/// 如果列表不存在，则创建一个新的列表并将其添加到订阅字典中。
	/// 这种方式避免了在多处重复编写相同的检查和创建逻辑。
	/// </remarks>
	private List<HandlerItem<TEvent>> GetOrCreateHandlerList<TEvent>() where TEvent : IEvent
	{
		// 获取事件的实际类型引用
		Type eventType = typeof(TEvent);

		// 检查该类型的处理器列表是否已存在
		if (!_subscriptions.TryGetValue(eventType, out var existingList))
		{
			// 不存在时创建新的处理器列表
			var handlers = new List<HandlerItem<TEvent>>();

			// 将新列表添加到订阅字典中
			_subscriptions[eventType] = handlers;

			return handlers;
		}

		// 存在时转换并返回现有列表
		// 这里需要类型转换，因为字典存储的是object类型
		return (List<HandlerItem<TEvent>>)existingList;
	}

	/// <summary>
	/// 获取或创建指定事件类型的过滤器字典
	/// </summary>
	/// <typeparam name="TEvent">事件类型</typeparam>
	/// <returns>与事件类型关联的过滤器字典</returns>
	/// <remarks>
	/// 这是一个内部泛型辅助方法，用于获取特定事件类型的过滤器字典。
	/// 过滤器字典将处理器委托映射到相应的过滤条件，实现有条件的事件处理。
	/// 如果字典不存在，则创建一个新的字典并将其添加到过滤器容器中。
	/// </remarks>
	private Dictionary<Action<TEvent>, EventFilter<TEvent>> GetOrCreateFilterDictionary<TEvent>() where TEvent : IEvent
	{
		// 获取事件的实际类型引用
		Type eventType = typeof(TEvent);

		// 检查该类型的过滤器字典是否已存在
		if (!_filters.TryGetValue(eventType, out var existingDict))
		{
			// 不存在时创建新的过滤器字典
			var filterDict = new Dictionary<Action<TEvent>, EventFilter<TEvent>>();

			// 将新字典添加到过滤器容器中
			_filters[eventType] = filterDict;

			return filterDict;
		}

		// 存在时转换并返回现有字典
		// 这里需要类型转换，因为容器存储的是object类型
		return (Dictionary<Action<TEvent>, EventFilter<TEvent>>)existingDict;
	}

	/// <summary>
	/// 输出调试日志
	/// </summary>
	/// <param name="message">日志消息</param>
	/// <remarks>
	/// 根据调试模式设置输出调试日志。
	/// 在Release模式下通过条件编译省略所有日志代码。
	/// </remarks>
	private void DebugLog(string message)
	{
		if (_debugMode)
		{
			GD.Print($"[事件总线] {message}");
		}
	}

	#endregion

	#region 生命周期方法

	/// <summary>
	/// 节点退出场景树时的清理
	/// </summary>
	/// <remarks>
	/// 在事件总线节点从场景树移除时调用。
	/// 清理所有注册的节点、订阅和事件监听器。
	/// 确保所有资源都被正确释放，防止内存泄漏。
	/// </remarks>
	public override void _ExitTree()
	{
		base._ExitTree();

		// 清空注册的节点列表
		_registeredNodes.Clear();

		// 清理所有订阅
		ClearAllSubscriptions();

		// 移除所有事件监听器
		EventPublished = null;
		EventHandled = null;
	}

	#endregion
}