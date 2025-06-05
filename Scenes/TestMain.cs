using EasyInject.Attributes;
using Godot;
using System;

[NodeService]
public partial class TestMain : Node2D
{
	[Inject]
	public TextLabel TextLabel { get; set; }

	[Inject("Sword")]
	private IWeapon meleeWeapon;

	[Inject("Bow")]
	private IWeapon rangedWeapon;

	public void AttackWithMelee()
	{
		meleeWeapon.Attack();
	}

	public void AttackWithRanged()
	{
		rangedWeapon.Attack();
	}
	public override void _Ready()
	{
		AttackWithMelee();
		AttackWithRanged();
		TextLabel.SetText("测试文本");
	}

}
