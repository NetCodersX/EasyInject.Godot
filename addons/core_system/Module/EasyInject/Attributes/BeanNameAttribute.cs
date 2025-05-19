using System;

namespace EasyInject.Attributes
{
	/// <summary>
	/// 标记字段为 Bean 名称来源（用于 [GameObjectBean(ENameType.FieldValue)]）。
	/// </summary>
	[AttributeUsage(AttributeTargets.Field)]
	public class BeanNameAttribute : Attribute
	{
		// 无需额外参数，仅用于标记
	}
}