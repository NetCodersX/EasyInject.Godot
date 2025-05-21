using System;

namespace EasyInject.Attributes
{
	/// <summary>
	/// 标记字段为 node 名称来源（用于 [GameObjectService(ENameType.FieldValue)]）。
	/// </summary>
	[AttributeUsage(AttributeTargets.Field)]
	public class ServiceNameAttribute : Attribute
	{
		// 无需额外参数，仅用于标记
	}
}