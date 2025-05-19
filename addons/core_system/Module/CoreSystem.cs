// using Godot;
// using System;
// using System.Collections.Generic;


// /// <summary>
// /// 核心系统 - 游戏框架的中央管理器
// /// </summary>
// public partial class CoreSystem : NodeSingleton<CoreSystem, Node>
// {
// 	// 模块类型映射字典，类似于GDScript中的_module_scripts
// 	private readonly Dictionary<StringName, Type> _moduleTypes = new Dictionary<StringName, Type>();

// 	// 模块实例字典
// 	private readonly Dictionary<StringName, Node> _modules = new Dictionary<StringName, Node>();

// 	// 各个系统的属性，使用懒加载模式
// 	private EventBus _eventBus;
// 	public EventBus EventBus
// 	{
// 		get
// 		{
// 			if (_eventBus == null)
// 			{
// 				_eventBus = GetModule("EventBus") as EventBus;
// 			}
// 			return _eventBus;
// 		}
// 	}

// 	public override void _Ready()
// 	{
// 		// 注册所有模块类型
// 		RegisterModuleTypes();
// 		GD.Print("[CoreSystem] 初始化完成");
// 	}

// 	/// <summary>
// 	/// 注册所有模块类型
// 	/// </summary>
// 	private void RegisterModuleTypes()
// 	{
// 		// 系统类模块注册
// 		_moduleTypes["EventBus"] = typeof(EventBus);

// 		// 将来可以在这里注册更多模块类型
// 		// _moduleTypes["AudioManager"] = typeof(AudioManager);
// 		// _moduleTypes["InputManager"] = typeof(InputManager);
// 		// ...等等
// 	}

// 	/// <summary>
// 	/// 检查模块是否启用
// 	/// </summary>
// 	/// <param name="moduleId">模块ID</param>
// 	/// <returns>模块是否启用</returns>
// 	public bool IsModuleEnabled(StringName moduleId)
// 	{
// 		string settingName = "core_system/ModuleEnable/" + moduleId;
// 		// 如果设置不存在，默认为启用
// 		if (!ProjectSettings.HasSetting(settingName))
// 		{
// 			return true;
// 		}
// 		return ProjectSettings.GetSetting(settingName).AsBool();
// 	}

// 	/// <summary>
// 	/// 创建模块实例
// 	/// </summary>
// 	/// <param name="moduleId">模块ID</param>
// 	/// <returns>模块实例</returns>
// 	private Node CreateModule(StringName moduleId)
// 	{
// 		if (!_moduleTypes.TryGetValue(moduleId, out Type moduleType))
// 		{
// 			GD.PushError($"无法找到模块类型：{moduleId}");
// 			return null;
// 		}

// 		try
// 		{
// 			// 使用Activator创建模块实例
// 			Node module = Activator.CreateInstance(moduleType) as Node;
// 			if (module == null)
// 			{
// 				GD.PushError($"无法创建模块实例：{moduleId}");
// 				return null;
// 			}

// 			_modules[moduleId] = module;
// 			module.Name = moduleId;
// 			AddChild(module);
// 			GD.Print($"[CoreSystem] 已创建模块：{moduleId}");
// 			return module;
// 		}
// 		catch (Exception ex)
// 		{
// 			GD.PushError($"创建模块实例时出错：{moduleId}, {ex.Message}");
// 			return null;
// 		}
// 	}

// 	/// <summary>
// 	/// 获取模块
// 	/// </summary>
// 	/// <param name="moduleId">模块ID</param>
// 	/// <returns>模块实例</returns>
// 	public Node GetModule(StringName moduleId)
// 	{
// 		if (!_modules.ContainsKey(moduleId))
// 		{
// 			if (IsModuleEnabled(moduleId))
// 			{
// 				Node module = CreateModule(moduleId);
// 				if (module != null)
// 				{
// 					_modules[moduleId] = module;
// 				}
// 			}
// 			else
// 			{
// 				if (_eventBus != null) // 避免循环引用
// 				{
// 					// 这里可以使用自己的日志系统发出警告
// 					GD.Print($"[CoreSystem] 模块未启用：{moduleId}");
// 				}
// 				else
// 				{
// 					GD.PushWarning($"模块未启用：{moduleId}");
// 				}
// 				return null;
// 			}
// 		}
// 		return _modules[moduleId];
// 	}

// 	/// <summary>
// 	/// 添加新的模块类型
// 	/// </summary>
// 	/// <param name="moduleId">模块ID</param>
// 	/// <param name="moduleType">模块类型</param>
// 	public void RegisterModuleType(StringName moduleId, Type moduleType)
// 	{
// 		if (moduleType.IsSubclassOf(typeof(Node)))
// 		{
// 			_moduleTypes[moduleId] = moduleType;
// 			GD.Print($"[CoreSystem] 已注册模块类型：{moduleId}");
// 		}
// 		else
// 		{
// 			GD.PushError($"无法注册模块类型：{moduleId} - 类型必须继承自Node");
// 		}
// 	}
// }