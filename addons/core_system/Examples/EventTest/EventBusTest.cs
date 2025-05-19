using EasyInject.Attributes;
using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// EventBus测试类 - 演示EventBus的各种功能和用法
/// </summary>
public partial class EventBusTest : Node2D
{
	// 事件总线实例引用
	[Autowired]
	private EventBus _eventBus { get; set; }

	// 自定义事件类型定义
	public struct PlayerMoveEvent : EventBus.IEvent
	{
		public string Direction;
		public int Distance;

		public PlayerMoveEvent(string direction, int distance)
		{
			Direction = direction;
			Distance = distance;
		}
	}

	public struct PlayerAttackEvent : EventBus.IEvent
	{
		public string Weapon;
		public int Damage;

		public PlayerAttackEvent(string weapon, int damage)
		{
			Weapon = weapon;
			Damage = damage;
		}
	}

	public override void _Ready()
	{
		// 启用调试模式和历史记录
		_eventBus.DebugMode = true;
		_eventBus.EnableHistory = true;

		// 1. 基本订阅（普通优先级）
		_eventBus.Subscribe<PlayerMoveEvent>(OnPlayerMove);

		// 2. 高优先级订阅
		_eventBus.Subscribe<PlayerMoveEvent>(OnPlayerMoveHighPriority,
			new EventBus.EventMetadata((int)EventBus.Priority.High));

		// 3. 低优先级订阅
		_eventBus.Subscribe<PlayerMoveEvent>(OnPlayerMoveLowPriority,
			new EventBus.EventMetadata((int)EventBus.Priority.Low));

		// 4. 一次性订阅
		_eventBus.SubscribeOnce<PlayerAttackEvent>(OnPlayerAttackOnce);

		// 5. 带过滤器的订阅（只处理向右移动）
		_eventBus.Subscribe<PlayerMoveEvent>(OnPlayerMoveRight,
			new EventBus.EventMetadata(),
			(PlayerMoveEvent evt) => evt.Direction == "right");

		// 延迟1秒后开始演示
		GetTree().CreateTimer(1.0f).Timeout += StartDemo;
	}

	/// <summary>
	/// 开始演示
	/// </summary>
	private void StartDemo()
	{
		GD.Print("\n=== 开始EventBus演示 ===");

		// 1. 测试不同优先级的事件处理
		GD.Print("\n1. 测试事件优先级：");
		_eventBus.Publish(new PlayerMoveEvent("left", 100));

		// 2. 测试一次性订阅
		GD.Print("\n2. 测试一次性订阅：");
		_eventBus.Publish(new PlayerAttackEvent("sword", 50));
		GD.Print("再次触发player_attack事件（不会有响应）：");
		_eventBus.Publish(new PlayerAttackEvent("sword", 30));

		// 3. 测试事件过滤器
		GD.Print("\n3. 测试事件过滤器：");
		GD.Print("向左移动（过滤器不会响应）：");
		_eventBus.Publish(new PlayerMoveEvent("left", 50));
		GD.Print("向右移动（过滤器会响应）：");
		_eventBus.Publish(new PlayerMoveEvent("right", 50));

		// 4. 测试延迟事件
		GD.Print("\n4. 测试延迟事件：");
		// 使用OnNextProcess而不是PublishDeferred
		GD.Print("使用OnNextProcess方法延迟事件：");
		_eventBus.PublishOnNextProcess(new PlayerMoveEvent("up", 100));

		// 5. 测试事件序列
		GD.Print("\n5. 测试事件序列：");
		var sequence = _eventBus.CreateEventSequence();
		sequence
			.Then(new PlayerMoveEvent("sequence-start", 10))
			.Wait(0.5f)
			.Then(new PlayerMoveEvent("sequence-middle", 20))
			.Wait(0.5f)
			.Then(new PlayerMoveEvent("sequence-end", 30));
		sequence.Start();

		// 6. 测试延迟事件
		GD.Print("\n6. 测试延迟发布事件：");
		_eventBus.PublishAfterDelay(new PlayerMoveEvent("delayed", 75), 1.0f);

		// 7. 测试去抖动功能
		GD.Print("\n7. 测试去抖动发布：");
		TestDebouncing();

		// 延迟显示历史记录，确保所有事件都已处理
		GetTree().CreateTimer(3.0f).Timeout += ShowEventHistory;
	}

	// 用于去抖动测试的计数器
	private int _debounceCounter = 0;

	/// <summary>
	/// 测试去抖动功能
	/// </summary>
	private void TestDebouncing()
	{
		// 使用计时器模拟快速连续调用
		_debounceCounter = 0;

		// 发布第一个去抖动事件
		GD.Print("发布去抖动事件 #1");
		PublishDebouncedEventSafely(1);

		// 模拟快速连续发布
		var timer = GetTree().CreateTimer(0.1f);
		timer.Timeout += () =>
		{
			GD.Print("发布去抖动事件 #2");
			PublishDebouncedEventSafely(2);
		};

		timer = GetTree().CreateTimer(0.2f);
		timer.Timeout += () =>
		{
			GD.Print("发布去抖动事件 #3");
			PublishDebouncedEventSafely(3);
		};

		timer = GetTree().CreateTimer(0.3f);
		timer.Timeout += () =>
		{
			GD.Print("发布去抖动事件 #4");
			PublishDebouncedEventSafely(4);
		};

		timer = GetTree().CreateTimer(0.4f);
		timer.Timeout += () =>
		{
			GD.Print("发布去抖动事件 #5 - 只有这个应该被处理");
			PublishDebouncedEventSafely(5);
		};
	}

	/// <summary>
	/// 安全地发布去抖动事件
	/// </summary>
	private void PublishDebouncedEventSafely(int value)
	{
		try
		{
			_eventBus.PublishDebounced(new PlayerMoveEvent("debounce", value), 0.5f);
			_debounceCounter = value;
		}
		catch (Exception ex)
		{
			GD.PrintErr($"去抖动发布异常: {ex.Message}");

			// 如果去抖动方法出错，直接使用延迟方法作为替代
			if (_debounceCounter < value)
			{
				GetTree().CreateTimer(0.5f).Timeout += () =>
				{
					GD.Print($"替代去抖动：使用延迟发布事件 #{value}");
					_eventBus.Publish(new PlayerMoveEvent("debounce-fallback", value));
				};
			}
		}
	}

	/// <summary>
	/// 显示事件历史记录
	/// </summary>
	private void ShowEventHistory()
	{
		GD.Print("\n8. 显示事件历史：");
		var history = _eventBus.GetEventHistory();
		foreach (var entry in history)
		{
			if (entry.EventData is PlayerMoveEvent moveEvent)
			{
				GD.Print($"事件：{entry.EventName}，参数：方向={moveEvent.Direction}, 距离={moveEvent.Distance}, 处理时间={entry.ProcessingTimeMs:F3}ms");
			}
			else if (entry.EventData is PlayerAttackEvent attackEvent)
			{
				GD.Print($"事件：{entry.EventName}，参数：武器={attackEvent.Weapon}, 伤害={attackEvent.Damage}, 处理时间={entry.ProcessingTimeMs:F3}ms");
			}
		}

		// 显示统计信息
		GD.Print("\n9. 事件系统统计：");

		// 订阅数量
		var counts = _eventBus.GetSubscriptionCounts();
		GD.Print("订阅统计：");
		foreach (var pair in counts)
		{
			GD.Print($"  {pair.Key}: {pair.Value}个订阅");
		}

		// 慢事件
		var slowEvents = _eventBus.GetSlowEvents(5.0); // 超过5ms就算慢
		if (slowEvents.Count > 0)
		{
			GD.Print("慢事件统计：");
			foreach (var evt in slowEvents)
			{
				GD.Print($"  {evt}");
			}
		}

		// 频繁事件
		var frequentEvents = _eventBus.GetFrequentEvents(3);
		GD.Print("频繁事件统计：");
		foreach (var evt in frequentEvents)
		{
			GD.Print($"  {evt}");
		}

		// 测试节点自动清理功能
		TestNodeCleanup();

		GD.Print("\n=== EventBus演示结束 ===");
	}

	#region 事件处理方法

	/// <summary>
	/// 普通优先级移动事件处理
	/// </summary>
	private void OnPlayerMove(PlayerMoveEvent evt)
	{
		GD.Print($"普通优先级：玩家向{evt.Direction}移动了{evt.Distance}单位");
	}

	/// <summary>
	/// 高优先级移动事件处理
	/// </summary>
	private void OnPlayerMoveHighPriority(PlayerMoveEvent evt)
	{
		GD.Print($"高优先级：玩家向{evt.Direction}移动了{evt.Distance}单位");
	}

	/// <summary>
	/// 低优先级移动事件处理
	/// </summary>
	private void OnPlayerMoveLowPriority(PlayerMoveEvent evt)
	{
		GD.Print($"低优先级：玩家向{evt.Direction}移动了{evt.Distance}单位");
	}

	/// <summary>
	/// 一次性攻击事件处理
	/// </summary>
	private void OnPlayerAttackOnce(PlayerAttackEvent evt)
	{
		GD.Print($"一次性订阅：玩家使用{evt.Weapon}造成了{evt.Damage}点伤害");
	}

	/// <summary>
	/// 只处理向右移动的事件处理
	/// </summary>
	private void OnPlayerMoveRight(PlayerMoveEvent evt)
	{
		GD.Print($"过滤器订阅：玩家向右移动了{evt.Distance}单位");
	}

	#endregion

	/// <summary>
	/// 测试节点自动清理
	/// </summary>
	private void TestNodeCleanup()
	{
		GD.Print("\n10. 测试节点自动清理：");

		// 创建临时子节点
		var tempNode = new Node();
		AddChild(tempNode);
		tempNode.Name = "TempSubscriberNode";

		// 让子节点订阅事件
		_eventBus.Subscribe<PlayerMoveEvent>(evt =>
		{
			GD.Print($"临时节点收到事件：{evt.Direction}");
		});

		// 发布一次事件，临时节点应该收到
		GD.Print("临时节点存在时发布事件：");
		_eventBus.Publish(new PlayerMoveEvent("cleanup-test-before", 99));

		// 删除子节点，EventBus应自动清理此订阅
		GD.Print("删除临时节点...");
		tempNode.QueueFree();

		// 等待一帧确保节点被销毁
		CallDeferred(nameof(TestCleanupAfterNodeDeletion));
	}

	/// <summary>
	/// 在节点删除后测试清理效果
	/// </summary>
	private void TestCleanupAfterNodeDeletion()
	{
		// 发布事件，已删除节点的处理器不应被调用
		GD.Print("临时节点删除后发布事件（不应有临时节点的响应）：");
		_eventBus.Publish(new PlayerMoveEvent("cleanup-test-after", 88));
	}
}