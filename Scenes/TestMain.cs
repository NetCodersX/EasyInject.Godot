using EasyInject.Attributes;
using Godot;
using System;

[GameObjectService]
public partial class TestMain : Node2D
{
	[Autowired]
	public TextLabel TextLabel { get; set; }

	public override void _Ready()
	{
		TextLabel.SetText("测试文本");
	}

}
