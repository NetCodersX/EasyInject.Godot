using System;

namespace EasyInject.Attributes
{
	/// <summary>
	/// 标记 Node 为需要 IoC 管理的服务对象。
	/// <para>可指定名字或命名方式。</para>
	/// </summary>
	[AttributeUsage(AttributeTargets.Class)]
	public class NodeServiceAttribute : Attribute
	{
		/// <summary>
		/// node 名字
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// 名字类型（枚举）
		/// </summary>
		public NamingStrategy NameType { get; }

		/// <summary>
		/// 通过自定义名字作为 node 名
		/// </summary>
		public NodeServiceAttribute(string name)
		{
			Name = name;
			NameType = NamingStrategy.Custom;
		}
		/// <summary>
		/// 默认自定义名字（空）
		/// </summary>
		public NodeServiceAttribute()
		{
			Name = string.Empty;
			NameType = NamingStrategy.Custom;
		}
		/// <summary>
		/// 通过指定名字类型
		/// </summary>
		public NodeServiceAttribute(NamingStrategy nameType)
		{
			NameType = nameType;
		}
	}

	/// <summary>
	/// Node 命名策略
	/// </summary>
	public enum NamingStrategy
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
		NodeName,
		/// <summary>
		/// 使用字段的值
		/// </summary>
		FieldValue
	}
}