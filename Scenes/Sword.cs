// 实现接口的Node
using EasyInject.Attributes;
using Godot;

[CreateNode("Sword")]
public partial class Sword : Node3D, IWeapon
{
	public void Attack()
	{
		GD.Print("Sword attack!");
	}
}