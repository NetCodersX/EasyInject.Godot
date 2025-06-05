// 另一个实现
using EasyInject.Attributes;
using Godot;

[CreateNode("Bow")]
public partial class Bow : Node3D, IWeapon
{
	public void Attack()
	{
		GD.Print("Bow attack!");
	}
}