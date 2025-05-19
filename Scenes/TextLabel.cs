using EasyInject.Attributes;
using Godot;
using System;

[GameObjectBean]
public partial class TextLabel : Label
{
	public void SetText(string val)
	{
		Text = val;
		GD.Print("文本值：" + val);
	}
}
