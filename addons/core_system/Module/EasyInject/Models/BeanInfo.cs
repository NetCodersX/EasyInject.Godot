using System;
using System.Collections.Generic;

namespace EasyInject.Models
{
	/// <summary>
	/// Bean 信息类，作为字典 Key 唯一标识一个 Bean（名字+类型）。
	/// </summary>
	public class BeanInfo
	{
		/// <summary>
		/// Bean 名称
		/// </summary>
		public readonly string Name;
		/// <summary>
		/// Bean 类型
		/// </summary>
		public readonly Type Type;
		/// <summary>
		/// Bean 所在场景列表
		/// </summary>
		public readonly List<string> Scenes = new();

		public BeanInfo(string name, Type type)
		{
			Name = name;
			Type = type;
		}

		public BeanInfo(string name, Type type, string scene)
		{
			Name = name;
			Type = type;
			Scenes.Add(scene);
		}

		/// <summary>
		/// 重写 Equals，实现唯一性比较
		/// </summary>
		public override bool Equals(object obj)
		{
			if (obj is BeanInfo beanInfo)
				return Name == beanInfo.Name && Type == beanInfo.Type;
			return false;
		}

		public override int GetHashCode()
		{
			return Name.GetHashCode() ^ Type.GetHashCode();
		}

		public override string ToString()
		{
			return $"BeanInfo: {Name} - {Type}";
		}
	}
}