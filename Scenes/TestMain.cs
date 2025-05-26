using EasyInject.Attributes;
using Godot;
using System;

[NodeService]
public partial class TestMain : Node2D
{
	[Inject]
	public TextLabel TextLabel { get; set; }

	public override void _Ready()
	{
		TextLabel.SetText("测试文本");
	}

}
