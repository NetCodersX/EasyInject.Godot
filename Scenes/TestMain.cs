using EasyInject.Attributes;
using Godot;
using System;

[GameObjectBean]
public partial class TestMain : Node2D
{
	[Autowired]
	public TextLabel TextLabel { get; set; }

	public override void _Ready()
	{
		TextLabel.SetText("测试文本");
	}

}
