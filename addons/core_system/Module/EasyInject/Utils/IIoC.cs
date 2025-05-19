using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace EasyInject.Utils
{
	/// <summary>
	///  IoC容器
	/// </summary>
	public interface IIoC
	{
		/// <summary>
		/// 创建一个Node作为Bean
		/// </summary>
		/// <param name="original">原型</param>
		/// <param name="beanName">名字</param>
		/// <typeparam name="T">Bean类型</typeparam>
		/// <returns>Bean实例</returns>
		T CreateNodeAsBean<T>(T original, string beanName) where T : Node;

		/// <summary>
		/// 创建一个Node作为Bean
		/// </summary>
		/// <param name="original">原型</param>
		/// <param name="beanName">名字</param>
		/// <param name="parent">父节点</param>
		/// <typeparam name="T">Bean类型</typeparam>
		/// <returns>Bean实例</returns>
		T CreateNodeAsBean<T>(T original, string beanName, Node parent) where T : Node;

		/// <summary>
		/// 创建一个Node作为Bean
		/// </summary>
		/// <param name="original">原型</param>
		/// <param name="beanName">名字</param>
		/// <param name="parent">父节点</param>
		/// <param name="inWorld">是否在世界节点下实例化</param>
		/// <typeparam name="T">Bean类型</typeparam>
		/// <returns>Bean实例</returns>
		T CreateNodeAsBean<T>(T original, string beanName, Node parent, bool inWorld) where T : Node;

		/// <summary>
		/// 创建一个Node作为Bean
		/// </summary>
		/// <param name="original">原型</param>
		/// <param name="beanName">名字</param>
		/// <param name="position">位置</param>
		/// <param name="rotation">旋转</param>
		/// <param name="parent">父节点</param>
		/// <typeparam name="T">Bean类型</typeparam>
		/// <returns>Bean实例</returns>
		T CreateNodeAsBean<T>(T original, string beanName, Vector3 position, Quaternion rotation, Node parent = null) where T : Node3D;

		/// <summary>
		/// 删除一个Node Bean
		/// </summary>
		/// <param name="bean">Bean实例</param>
		/// <param name="beanName">Bean名字</param>
		/// <param name="deleteNode">是否删除节点</param>
		/// <param name="t">延迟时间（秒）</param>
		/// <typeparam name="T">Bean类型</typeparam>
		/// <returns>是否删除成功</returns>
		bool DeleteNodeBean<T>(T bean, string beanName = "", bool deleteNode = false, float t = 0.0f) where T : Node;

		/// <summary>
		/// 立即删除一个Node Bean
		/// </summary>
		/// <param name="bean">Bean实例</param>
		/// <param name="beanName">Bean名字</param>
		/// <param name="deleteNode">是否删除节点</param>
		/// <typeparam name="T">Bean类型</typeparam>
		/// <returns>是否删除成功</returns>
		bool DeleteNodeBeanImmediate<T>(T bean, string beanName = "", bool deleteNode = false) where T : Node;

		/// <summary>
		/// 获取一个Bean
		/// </summary>
		/// <param name="name">Bean的名字</param>
		/// <typeparam name="T">Bean的类型</typeparam>
		/// <returns>Bean实例</returns>
		T GetBean<T>(string name = "") where T : class;

		/// <summary>
		/// 获取场景中需要注入的Node实例
		/// </summary>
		void Init();

		/// <summary>
		/// 清空该场景的Bean
		/// </summary>
		/// <param name="scene">场景名称</param>
		/// <param name="clearAcrossScenesBeans">是否清空跨场景的Bean</param>
		void ClearBeans(string scene = null, bool clearAcrossScenesBeans = false);

		/// <summary>
		/// 清空该场景的Bean
		/// </summary>
		/// <param name="clearAcrossScenesBeans">是否清空跨场景的Bean</param>
		void ClearBeans(bool clearAcrossScenesBeans);
	}
}