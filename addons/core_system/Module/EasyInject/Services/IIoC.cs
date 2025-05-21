using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace EasyInject.Services
{
	/// <summary>
	///  IoC容器
	/// </summary>
	public interface IIoC
	{
		/// <summary>
		/// 创建一个Node作为服务注入
		/// </summary>
		/// <param name="original">原型</param>
		/// <param name="nodeName">名字</param>
		/// <typeparam name="T">node类型</typeparam>
		/// <returns>node实例</returns>
		T CreateNodeAsService<T>(T original, string nodeName) where T : Node;

		/// <summary>
		/// 创建一个Node作为服务注入
		/// </summary>
		/// <param name="original">原型</param>
		/// <param name="nodeName">名字</param>
		/// <param name="parent">父节点</param>
		/// <typeparam name="T">node类型</typeparam>
		/// <returns>node实例</returns>
		T CreateNodeAsService<T>(T original, string nodeName, Node parent) where T : Node;

		/// <summary>
		/// 创建一个Node作为服务注入
		/// </summary>
		/// <param name="original">原型</param>
		/// <param name="nodeName">名字</param>
		/// <param name="parent">父节点</param>
		/// <param name="inWorld">是否在世界节点下实例化</param>
		/// <typeparam name="T">node类型</typeparam>
		/// <returns>node实例</returns>
		T CreateNodeAsService<T>(T original, string nodeName, Node parent, bool inWorld) where T : Node;

		/// <summary>
		/// 创建一个Node作为服务注入
		/// </summary>
		/// <param name="original">原型</param>
		/// <param name="nodeName">名字</param>
		/// <param name="position">位置</param>
		/// <param name="rotation">旋转</param>
		/// <param name="parent">父节点</param>
		/// <typeparam name="T">node类型</typeparam>
		/// <returns>node实例</returns>
		T CreateNodeAsService<T>(T original, string nodeName, Vector3 position, Quaternion rotation, Node parent = null) where T : Node3D;

		/// <summary>
		/// 删除一个Node并注销服务
		/// </summary>
		/// <param name="node">node实例</param>
		/// <param name="nodeName">node名字</param>
		/// <param name="deleteNode">是否删除节点</param>
		/// <param name="t">延迟时间（秒）</param>
		/// <typeparam name="T">node类型</typeparam>
		/// <returns>是否删除成功</returns>
		bool DeleteNode<T>(T node, string nodeName = "", bool deleteNode = false, float t = 0.0f) where T : Node;

		/// <summary>
		/// 立即删除一个Node并注销服务
		/// </summary>
		/// <param name="node">node实例</param>
		/// <param name="nodeName">node名字</param>
		/// <param name="deleteNode">是否删除节点</param>
		/// <typeparam name="T">node类型</typeparam>
		/// <returns>是否删除成功</returns>
		bool DeleteNodeImmediate<T>(T node, string nodeName = "", bool deleteNode = false) where T : Node;

		/// <summary>
		/// 获取一个node
		/// </summary>
		/// <param name="name">node的名字</param>
		/// <typeparam name="T">node的类型</typeparam>
		/// <returns>node实例</returns>
		T GetNode<T>(string name = "") where T : class;

		/// <summary>
		/// 获取场景中需要注入的Node实例
		/// </summary>
		void Init();

		/// <summary>
		/// 清空该场景的node
		/// </summary>
		/// <param name="scene">场景名称</param>
		/// <param name="clearAcrossScenesnodes">是否清空跨场景的node</param>
		void ClearNodes(string scene = null, bool clearAcrossScenesnodes = false);

		/// <summary>
		/// 清空该场景的node
		/// </summary>
		/// <param name="clearAcrossScenesnodes">是否清空跨场景的node</param>
		void ClearNodes(bool clearAcrossScenesnodes);
	}
}