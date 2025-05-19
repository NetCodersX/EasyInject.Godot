using Godot;
using EasyInject.Utils;

namespace EasyInject.Controllers
{
	/// <summary>
	/// 全局初始化器节点。
	/// 挂载于主场景根节点，负责场景加载时初始化 IoC 容器。
	/// </summary>
	public partial class GlobalInitializer : Node
	{
		/// <summary>
		/// 全局唯一 IoC 实例（静态单例）。
		/// </summary>
		public static IIoC Instance { get; } = new MyIoC();
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
            // 初始化 IoC 容器，注册场景所有 Bean
            Instance.Init();
		}

		/// <summary>
		/// 
		/// </summary>
		public override void _ExitTree()
		{
			// 场景卸载时，自动清除本场景 Bean
			Instance.ClearBeans(_sceneName);
		}
	}
}