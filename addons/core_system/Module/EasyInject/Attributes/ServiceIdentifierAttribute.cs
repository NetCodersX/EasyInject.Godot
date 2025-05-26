using System;

namespace EasyInject.Attributes
{
	/// <summary>
	/// 标记字段为 node 名称来源（用于 [NodeServiceAttribute(ENameType.FieldValue)]）。
	/// </summary>
	[AttributeUsage(AttributeTargets.Field)]
	public class ServiceIdentifierAttribute : Attribute
	{
		// 无需额外参数，仅用于标记
	}
}