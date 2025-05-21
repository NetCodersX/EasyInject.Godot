using Godot;
using EasyInject.Attributes;

namespace EasyInject.Behaviours
{
	/// <summary>
	/// 跨场景 Node 节点。
	/// 自动标记 [PersistAcrossScenes]，并在 _Ready 阶段保证自己不会被场景卸载。
	/// </summary>
	[PersistAcrossScenes]
	public partial class AcrossScenesNodeObject : NodeObject
	{
		public override void _Ready()
		{
			// 使自己成为主树的直接子节点，从而实现跨场景持久化
			if (GetParent() != null)
				GetParent().RemoveChild(this);
			GetTree().Root.AddChild(this, true);
		}
	}
}