using System;

namespace EasyInject.Attributes
{
	/// <summary>
	/// 标记 Node 为需要 IoC 管理的场景 Bean。
	/// 可指定名字或命名方式。
	/// </summary>
	[AttributeUsage(AttributeTargets.Class)]
	public class GameObjectBeanAttribute : Attribute
	{
		/// <summary>
		/// Bean 名字
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// 名字类型（枚举）
		/// </summary>
		public ENameType NameType { get; }

		/// <summary>
		/// 通过自定义名字作为 Bean 名
		/// </summary>
		public GameObjectBeanAttribute(string name)
		{
			Name = name;
			NameType = ENameType.Custom;
		}
		/// <summary>
		/// 默认自定义名字（空）
		/// </summary>
		public GameObjectBeanAttribute()
		{
			Name = string.Empty;
			NameType = ENameType.Custom;
		}
		/// <summary>
		/// 通过指定名字类型
		/// </summary>
		public GameObjectBeanAttribute(ENameType nameType)
		{
			NameType = nameType;
		}
	}

	/// <summary>
	/// Bean 名字类型枚举
	/// </summary>
	public enum ENameType
	{
		/// <summary>
		/// 自定义名字
		/// </summary>
		Custom,
		/// <summary>
		/// 使用类名
		/// </summary>
		ClassName,
		/// <summary>
		///  使用 Node 名字
		/// </summary>
		GameObjectName,
		/// <summary>
		/// 使用字段的值
		/// </summary>
		FieldValue
	}
}