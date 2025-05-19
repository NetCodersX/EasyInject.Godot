using System;

namespace EasyInject.Attributes
{
	/// <summary>
	/// 标记此 Node Bean 为跨场景持久化，不随场景卸载。
	/// </summary>
	[AttributeUsage(AttributeTargets.Class)]
	public class PersistAcrossScenesAttribute : Attribute
	{
		// 无需参数，仅用于标记
	}
}