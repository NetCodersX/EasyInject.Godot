using System;
using System.Collections.Generic;

namespace EasyInject.Models
{
	/// <summary>
	/// Node 注入信息类，作为字典 Key 唯一标识一个 node（名字+类型）。
	/// </summary>
	public class NodeInfo
	{
		/// <summary>
		/// Node 名称
		/// </summary>
		public readonly string Name;
		/// <summary>
		/// Node 类型
		/// </summary>
		public readonly Type Type;
		/// <summary>
		/// Node 所在场景列表
		/// </summary>
		public readonly List<string> Scenes = new();

		public NodeInfo(string name, Type type)
		{
			Name = name;
			Type = type;
		}

		public NodeInfo(string name, Type type, string scene)
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
			if (obj is NodeInfo nodeInfo)
				return Name == nodeInfo.Name && Type == nodeInfo.Type;
			return false;
		}

		public override int GetHashCode()
		{
			return Name.GetHashCode() ^ Type.GetHashCode();
		}

		public override string ToString()
		{
			return $"NodeInfo: {Name} - {Type}";
		}
	}
}