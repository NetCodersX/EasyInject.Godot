using Godot;
using System.Collections.Generic;


/// <summary>
/// 核心系统设置类 - 提供系统配置和项目设置访问
/// </summary>
/// <remarks>
/// 本类负责管理核心系统的配置设置，提供统一的设置访问接口
/// </remarks>
public partial class CoreSystemSettings : RefCounted
{
	/// <summary>
	/// 模块启用设置路径前缀
	/// </summary>
	public const string SETTING_MODULE_ENABLE = "core_system/ModuleEnable/";

	/// <summary>
	/// 事件总线模块的设置信息字典 - EditorPlugin将使用此字典注册项目设置
	/// </summary>
	public static readonly Dictionary<string, Dictionary<string, Variant>> SETTING_INFO_DICT = new()
	{
		{
			"ModuleEnable/EventBus",
			new Dictionary<string, Variant>
			{
				{ "Name", SETTING_MODULE_ENABLE + "EventBus" },
				{ "Type", (int)Variant.Type.Bool },
				{ "Hint", (int)PropertyHint.None },
				{ "Hint_string", "" },
				{ "Basic", true },
				{ "Default", true }
			}
		}
	};

	/// <summary>
	/// 傻瓜式获取设置值的方法 - 支持通过设置路径或字典键获取设置
	/// </summary>
	/// <param name="settingName">设置名称或字典键</param>
	/// <param name="defaultValue">默认值（如未指定则使用注册的默认值）</param>
	/// <returns>设置值</returns>
	/// <remarks>
	/// 此方法允许通过设置路径或字典键获取设置值，简化了设置访问
	/// 只需填写设置路径或字典键中的一个，方法会自动查找对应的设置
	/// </remarks>
	public static Variant GetSettingValue(string settingName, Variant defaultValue = new Variant())
	{
		Dictionary<string, Variant> settingDict = new();

		// 首先检查是否可以直接从字典键获取设置信息
		if (SETTING_INFO_DICT.ContainsKey(settingName))
		{
			settingDict = SETTING_INFO_DICT[settingName];
			settingName = settingDict["Name"].AsString();
		}

		// 如果没有通过字典键找到，尝试通过设置路径在字典中查找
		if (settingDict.Count == 0)
		{
			foreach (var dict in SETTING_INFO_DICT.Values)
			{
				if (dict["Name"].AsString() == settingName)
				{
					settingDict = dict;
					break;
				}
			}
		}

		// 如果存在默认值且传入的是空Variant，使用注册的默认值
		if (settingDict.ContainsKey("Default") && defaultValue.VariantType == Variant.Type.Nil)
		{
			defaultValue = settingDict["Default"];
		}

		// 从项目设置中获取值，如果不存在则返回默认值
		if (ProjectSettings.HasSetting(settingName))
		{
			return ProjectSettings.GetSetting(settingName);
		}

		return defaultValue;
	}
}
