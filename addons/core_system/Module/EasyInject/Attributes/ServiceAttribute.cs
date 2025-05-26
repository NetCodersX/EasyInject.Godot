using System;

namespace EasyInject.Attributes
{
	/// <summary>
	/// 用于标记普通 C#类注入。
	/// </summary>
	[AttributeUsage(AttributeTargets.Class)]
	public class ServiceAttribute : Attribute
	{
		/// <summary>
		/// Node 名称（可选）。
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// 指定 Node 名称。
		/// </summary>
		public ServiceAttribute(string name = null)
		{
			Name = name;
		}
	}
}