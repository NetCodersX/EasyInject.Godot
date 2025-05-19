using System;

namespace EasyInject.Attributes
{
	/// <summary>
	/// 用于字段、属性、构造参数的依赖注入标记。
	/// 表示此成员需要自动注入 Bean。
	/// </summary>
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Property)]
	public class AutowiredAttribute : Attribute
	{
		/// <summary>
		/// 指定要注入 Bean 的名字（可选）。
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// 构造函数，可指定 Bean 名字。
		/// </summary>
		/// <param name="name">Bean 名字，可为空</param>
		public AutowiredAttribute(string name =null)
		{
			Name = name;
		}
	}
}