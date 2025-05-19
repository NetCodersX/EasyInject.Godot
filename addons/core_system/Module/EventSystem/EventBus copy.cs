using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// 事件总线 - 提供基于发布-订阅模式的事件系统
/// </summary>
/// <remarks>
/// 事件总线允许在不同系统之间通过事件进行松耦合通信
/// 支持事件过滤、优先级排序和一次性订阅等高级特性
/// </remarks>
public partial class EventBus1 : Node
{
	/// <summary>
	/// 事件元数据类 - 存储事件订阅的附加信息
	/// </summary>
	/// <remarks>
	/// 包含事件处理的优先级、是否为一次性订阅以及过滤条件
	/// </remarks>
	[Serializable] // 添加此特性使其序列化支持更好
	public partial struct EventMetadata : IEquatable<EventMetadata>
	{
		/// <summary>
		/// 事件优先级 - 数值越小优先级越高
		/// </summary>
		public int Priority { get; set; }

		/// <summary>
		/// 是否为一次性订阅 - 触发一次后自动取消订阅
		/// </summary>
		public bool OneTime { get; set; }

		/// <summary>
		/// 过滤条件委托 - 返回true表示事件应被处理
		/// </summary>
		[NonSerialized] // 标记不需要序列化的字段
		public Func<Variant, bool> Filter;

		/// <summary>
		/// 创建默认的事件元数据
		/// </summary>
		public EventMetadata()
		{
			Priority = 0;
			OneTime = false;
			Filter = null;
		}

		/// <summary>
		/// 创建包含指定优先级和一次性标记的事件元数据
		/// </summary>
		/// <param name="priority">事件优先级</param>
		/// <param name="oneTime">是否为一次性订阅</param>
		/// <param name="filter">过滤条件委托</param>
		public EventMetadata(int priority, bool oneTime = false, Func<Variant, bool> filter = null)
		{
			Priority = priority;
			OneTime = oneTime;
			Filter = filter;
		}

		// 实现 IEquatable<T> 接口 
		public bool Equals(EventMetadata other)
		{
			return Priority == other.Priority &&
				   OneTime == other.OneTime;
			// 注意：我们不比较Filter委托，因为委托比较通常不是按值比较
		}

		public override bool Equals(object obj)
		{
			return obj is EventMetadata metadata && Equals(metadata);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(Priority, OneTime);
		}

		public static bool operator ==(EventMetadata left, EventMetadata right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(EventMetadata left, EventMetadata right)
		{
			return !left.Equals(right);
		}
	}

	/// <summary>
	/// 事件处理器委托类型 - 接收事件数据并进行处理
	/// </summary>
	/// <param name="data">事件数据</param>
	public delegate void EventHandler(Variant data);

	/// <summary>
	/// 事件订阅字典 - 存储所有事件订阅
	/// 键：事件名称，值：处理器和元数据的元组列表
	/// </summary>
	private readonly Dictionary<string, List<(EventHandler handler, EventMetadata metadata)>> _subscriptions
		= new Dictionary<string, List<(EventHandler, EventMetadata)>>();

	/// <summary>
	/// 订阅事件 - 注册处理器以响应特定事件
	/// </summary>
	/// <param name="eventName">事件名称</param>
	/// <param name="handler">事件处理器</param>
	/// <param name="metadata">事件元数据（可选）</param>
	/// <remarks>
	/// 订阅后，当指定事件被触发时，处理器将被调用
	/// 可以通过元数据设置优先级、一次性订阅和过滤条件
	/// </remarks>
	public void Subscribe(string eventName, EventHandler handler, EventMetadata metadata = default)
	{
		// 确保事件名称有对应的订阅列表
		if (!_subscriptions.ContainsKey(eventName))
		{
			_subscriptions[eventName] = new List<(EventHandler, EventMetadata)>();
		}

		// 添加处理器和元数据到订阅列表
		_subscriptions[eventName].Add((handler, metadata));

		// 按优先级排序
		_subscriptions[eventName].Sort((a, b) => a.metadata.Priority.CompareTo(b.metadata.Priority));

		GD.Print($"[EventBus] Subscribed to event: {eventName}, priority: {metadata.Priority}, oneTime: {metadata.OneTime}");
	}

	/// <summary>
	/// 取消订阅事件 - 移除事件处理器
	/// </summary>
	/// <param name="eventName">事件名称</param>
	/// <param name="handler">要移除的事件处理器</param>
	/// <returns>是否成功取消订阅</returns>
	/// <remarks>
	/// 取消订阅后，处理器将不再响应该事件
	/// 如果同一处理器多次订阅同一事件，只会移除第一个匹配的订阅
	/// </remarks>
	public bool Unsubscribe(string eventName, EventHandler handler)
	{
		// 检查事件是否存在
		if (!_subscriptions.ContainsKey(eventName))
		{
			GD.Print($"[EventBus] Cannot unsubscribe: event {eventName} not found");
			return false;
		}

		// 查找匹配的处理器
		int index = _subscriptions[eventName].FindIndex(sub => sub.handler == handler);
		if (index == -1)
		{
			GD.Print($"[EventBus] Cannot unsubscribe: handler not found for event {eventName}");
			return false;
		}

		// 移除处理器
		_subscriptions[eventName].RemoveAt(index);
		GD.Print($"[EventBus] Unsubscribed from event: {eventName}");

		// 如果列表为空，移除事件键
		if (_subscriptions[eventName].Count == 0)
		{
			_subscriptions.Remove(eventName);
		}

		return true;
	}

	/// <summary>
	/// 触发事件 - 通知所有订阅者
	/// </summary>
	/// <param name="eventName">事件名称</param>
	/// <param name="eventData">事件数据</param>
	/// <remarks>
	/// 触发事件时，所有订阅的处理器将按优先级顺序被调用
	/// 一次性订阅会在处理后自动取消
	/// 如果设置了过滤条件，只有条件满足的处理器会被调用
	/// </remarks>
	public void Emit(string eventName, Variant eventData = default)
	{
		// 检查事件是否存在
		if (!_subscriptions.ContainsKey(eventName))
		{
			// 没有订阅者，直接返回
			return;
		}

		// 创建要移除的一次性订阅列表
		var oneTimeHandlers = new List<EventHandler>();

		// 调用所有处理器
		foreach (var (handler, metadata) in _subscriptions[eventName])
		{
			// 检查过滤条件
			if (metadata.Filter != null && !metadata.Filter(eventData))
			{
				continue; // 过滤条件不满足，跳过
			}

			// 调用处理器
			handler(eventData);

			// 如果是一次性订阅，添加到移除列表
			if (metadata.OneTime)
			{
				oneTimeHandlers.Add(handler);
			}
		}

		// 移除所有一次性订阅
		foreach (var handler in oneTimeHandlers)
		{
			Unsubscribe(eventName, handler);
		}
	}

	/// <summary>
	/// 清除所有事件订阅
	/// </summary>
	/// <remarks>
	/// 用于重置事件总线或在系统关闭时清理资源
	/// </remarks>
	public void ClearAllSubscriptions()
	{
		_subscriptions.Clear();
		GD.Print("[EventBus] All subscriptions cleared");
	}

	/// <summary>
	/// 获取事件的订阅数量
	/// </summary>
	/// <param name="eventName">事件名称</param>
	/// <returns>订阅数量</returns>
	public int GetSubscriptionCount(string eventName)
	{
		return _subscriptions.ContainsKey(eventName) ? _subscriptions[eventName].Count : 0;
	}

	/// <summary>
	/// 检查事件是否有订阅者
	/// </summary>
	/// <param name="eventName">事件名称</param>
	/// <returns>是否有订阅者</returns>
	public bool HasSubscribers(string eventName)
	{
		return _subscriptions.ContainsKey(eventName) && _subscriptions[eventName].Count > 0;
	}
}


