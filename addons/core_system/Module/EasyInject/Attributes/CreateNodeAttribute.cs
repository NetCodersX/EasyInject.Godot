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
		/// Bean名称
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// Bean的命名策略
		/// </summary>
		public ENameType NameType { get; }

		/// <summary>
		/// 是否应添加到场景树
		/// </summary>
		public bool AddToScene { get; }

		/// <summary>
		/// 父节点的Bean名称（如果需要添加到场景树）
		/// </summary>
		public string ParentBeanName { get; }

		/// <summary>
		/// 创建一个CreateNode特性
		/// </summary>
		/// <param name="nameType">Bean命名策略</param>
		/// <param name="addToScene">是否添加到场景树</param>
		/// <param name="parentBeanName">父节点Bean名称</param>
		public CreateNodeAttribute(ENameType nameType = ENameType.ClassName, bool addToScene = true, string parentBeanName = ".")
		{
			NameType = nameType;
			AddToScene = addToScene;
			ParentBeanName = parentBeanName;
			Name = string.Empty;
		}

		/// <summary>
		/// 创建一个CreateNode特性，使用自定义名称
		/// </summary>
		/// <param name="name">自定义Bean名称</param>
		/// <param name="addToScene">是否添加到场景树</param>
		/// <param name="parentBeanName">父节点Bean名称</param>
		public CreateNodeAttribute(string name, bool addToScene = false, string parentBeanName = "")
		{
			Name = name;
			NameType = ENameType.Custom;
			AddToScene = addToScene;
			ParentBeanName = parentBeanName;
		}
	}
}