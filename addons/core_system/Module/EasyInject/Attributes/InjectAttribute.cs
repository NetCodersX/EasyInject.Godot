using System;

namespace EasyInject.Attributes
{
	/// <summary>
	/// 用于字段、属性、构造参数的依赖注入标记。
	/// <para>表示此成员需要自动注入服务</para>
	/// </summary>
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Property)]
	public class InjectAttribute : Attribute
	{
		/// <summary>
		/// 指定要注入 Node 的名字（可选）。
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// 构造函数，可指定 Node 名字。
		/// </summary>
		/// <param name="name">Node 名字，可为空</param>
		public InjectAttribute(string name = null)
		{
			Name = name;
		}
	}
}