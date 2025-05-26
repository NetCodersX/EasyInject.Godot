using Godot;
using EasyInject.Services;

namespace EasyInject.Controllers
{
	/// <summary>
	/// 全局初始化器节点。
	/// <para>挂载于主场景根节点，负责场景加载时初始化 IoC 容器。</para>
	/// </summary>
	public partial class ContainerInitializer : Node
	{
		/// <summary>
		/// 全局唯一 IoC 实例（静态单例）。
		/// </summary>
		public static INodeContainer Instance { get; } = new NodeContainer();
		/// <summary>
		/// 
		/// </summary>
		private string _sceneName;
		/// <summary>
		/// 
		/// </summary>
		public override void _Ready()
		{
			_sceneName = GetTree().CurrentScene?.Name;
			// 初始化 IoC 容器，注册场景所有 Node
			Instance.Initialize();
		}

		/// <summary>
		/// 
		/// </summary>
		public override void _ExitTree()
		{
			// 场景卸载时，自动清除本场景 Node
			Instance.ClearNodesService(_sceneName);
		}
	}
}