using System;
using EasyInject.Models;
using Godot;

namespace EasyInject.Attributes
{
	/// <summary>
	/// 用于标记需要由IoC容器创建的节点类或工厂方法。
	/// <para>注意：当父节点为空时默认添加到 root 下。切换场景时是不会注销改节点！！！</para>
	/// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
	public class CreateNodeAttribute : Attribute
	{
		/// <summary>
		/// Node名称
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// Node的命名策略
		/// </summary>
		public NamingStrategy NameType { get; }

		/// <summary>
		/// 是否应添加到场景树
		/// </summary>
		public bool AddToScene { get; }

		/// <summary>
		/// 父节点的名称（如果需要添加到场景树）
		/// </summary>
		public string ParentNodeName { get; }

		/// <summary>
		/// 创建一个CreateNode特性
		/// </summary>
		/// <param name="nameType">node命名策略</param>
		/// <param name="addToScene">是否添加到场景树</param>
		/// <param name="parentNodeName">父节点名称</param>
		public CreateNodeAttribute(NamingStrategy nameType = NamingStrategy.ClassName, bool addToScene = true, string parentNodeName = ".")
		{
			NameType = nameType;
			AddToScene = addToScene;
			ParentNodeName = parentNodeName;
			Name = string.Empty;
		}

		/// <summary>
		/// 创建一个CreateNode特性，使用自定义名称
		/// </summary>
		/// <param name="name">自定义node名称</param>
		/// <param name="addToScene">是否添加到场景树</param>
		/// <param name="parentNodeName">父节点名称</param>
		public CreateNodeAttribute(string name, bool addToScene = false, string parentNodeName = "")
		{
			Name = name;
			NameType = NamingStrategy.Custom;
			AddToScene = addToScene;
			ParentNodeName = parentNodeName;
		}
	}
}