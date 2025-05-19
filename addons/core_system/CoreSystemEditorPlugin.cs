#if TOOLS
using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// 核心系统编辑器插件 - 负责在项目中注册和管理核心系统的设置和自动加载
/// </summary>
[Tool]
public partial class CoreSystemEditorPlugin : EditorPlugin
{
	/// <summary>
	/// 核心系统自动加载的名称
	/// </summary>
	public const string SYSTEM_NAME = "CoreSystem";
	/// <summary>
	/// 核心系统主脚本的路径，将被注册为自动加载
	/// </summary>
	private string SYSTEM_PATH;

	/// <summary>
	/// 设置信息字典 - 从CoreSystemSettings获取，用于注册项目设置
	/// </summary>
	private static readonly Dictionary<string, Dictionary<string, Variant>> SETTING_INFO_DICT = CoreSystemSettings.SETTING_INFO_DICT;


	/// <summary>
	/// 依赖注入
	/// </summary>
	private string SYSTEM_PATH_EasyInject;

	/// <summary>
	/// 插件进入场景树时的初始化 - 添加项目设置和自动加载
	/// </summary>
	public override void _EnterTree()
	{
		// 获取脚本路径 - 使用正确的方法访问当前脚本
		// SYSTEM_PATH = GetScriptDirectoryPath() + "/Module/CoreSystem.cs";
		SYSTEM_PATH = GetScriptDirectoryPath() + "/Module/EasyInject/Controllers/GlobalInitializer.cs";

		// 添加项目设置和自动加载单例
		AddProjectSettings();
		AddAutoloadSingleton(SYSTEM_NAME, SYSTEM_PATH);

		ProjectSettings.Save();
	}

	/// <summary>
	/// 插件退出场景树时的清理 - 移除项目设置和自动加载
	/// </summary>
	public override void _ExitTree()
	{
		// 插件禁用时清理
		RemoveAutoloadSingleton(SYSTEM_NAME);
		RemoveProjectSettings();

		ProjectSettings.Save();
	}

	/// <summary>
	/// 获取当前脚本所在的目录路径
	/// </summary>
	private string GetScriptDirectoryPath()
	{
		// 在C#中正确获取脚本路径
		Script script = GetScript().As<Script>();
		string scriptPath = script.ResourcePath;
		return scriptPath.Substring(0, scriptPath.LastIndexOf("/"));
	}

	/// <summary>
	/// 添加配置脚本中定义的所有设置项到项目设置
	/// </summary>
	private void AddProjectSettings()
	{
		foreach (var settingDict in SETTING_INFO_DICT.Values)
		{
			AddSettingDict(settingDict);
		}
	}

	/// <summary>
	/// 从项目设置中移除所有由插件添加的设置项
	/// </summary>
	private void RemoveProjectSettings()
	{
		foreach (var settingDict in SETTING_INFO_DICT.Values)
		{
			RemoveSettingDict(settingDict);
		}
	}

	/// <summary>
	/// 使用字典信息添加单个设置项到项目设置
	/// </summary>
	private void AddSettingDict(Dictionary<string, Variant> infoDict)
	{
		string settingName = infoDict["Name"].AsString();
		if (!ProjectSettings.HasSetting(settingName))
		{
			ProjectSettings.SetSetting(settingName, infoDict["Default"]);
		}

		ProjectSettings.SetAsBasic(settingName, infoDict["Basic"].AsBool());
		ProjectSettings.SetInitialValue(settingName, infoDict["Default"]);

		// 构建属性信息字典 - 需要转换为Godot的字典
		var propertyInfo = new Godot.Collections.Dictionary();
		foreach (var key in infoDict.Keys)
		{
			propertyInfo[key] = infoDict[key];
		}

		ProjectSettings.AddPropertyInfo(propertyInfo);
	}

	/// <summary>
	/// 从项目设置中移除单个设置项
	/// </summary>
	private void RemoveSettingDict(Dictionary<string, Variant> infoDict)
	{
		string settingName = infoDict["Name"].AsString();
		if (ProjectSettings.HasSetting(settingName))
		{
			ProjectSettings.SetSetting(settingName, new Variant());
		}
	}

	/// <summary>
	/// 添加自定义设置项到项目设置
	/// </summary>
	private void AddSetting(
		string name,
		Variant defaultValue,
		int type,
		int hint,
		string hintString = "")
	{
		if (!ProjectSettings.HasSetting(name))
		{
			ProjectSettings.SetSetting(name, defaultValue);
		}

		ProjectSettings.SetInitialValue(name, defaultValue);

		var propertyInfo = new Godot.Collections.Dictionary
		{
			{"Name", name},
			{"Type", type},
			{"Hint", hint},
			{"Hint_string", hintString}
		};

		ProjectSettings.AddPropertyInfo(propertyInfo);
	}

	/// <summary>
	/// 确保项目设置中包含核心系统的模块分类
	/// </summary>
	private void EnsureProjectSettingsCategory()
	{
		if (!ProjectSettings.HasSetting("core_system/modules"))
		{
			ProjectSettings.SetSetting("core_system/modules", new Godot.Collections.Dictionary());
			ProjectSettings.SetAsBasic("core_system/modules", true);

			var propertyInfo = new Godot.Collections.Dictionary
			{
				{"Name", "core_system/modules"},
				{"Type", (int)Variant.Type.Dictionary},
				{"Hint", (int)PropertyHint.None},
				{"Hint_string", "Core System Modules"}
			};

			ProjectSettings.AddPropertyInfo(propertyInfo);
		}
	}
}

#endif
