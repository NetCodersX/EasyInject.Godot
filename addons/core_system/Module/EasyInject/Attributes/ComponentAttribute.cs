using System;

namespace EasyInject.Attributes
{
	/// <summary>
	/// 用于标记普通 C# 类为组件 （非 Node）。
	/// 支持依赖注入。
	/// </summary>
	[AttributeUsage(AttributeTargets.Class)]
	public class ComponentAttribute : Attribute
	{
		/// <summary>
		/// Node 名称（可选）。
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// 指定 Node 名称。
		/// </summary>
		public ComponentAttribute(string name = null)
		{
			Name = name;
		}
	}
}