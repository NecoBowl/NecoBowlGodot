using Godot;
using System;

public partial class UnitOnPlayfield_SpriteRoot : Control
{
	public AnimationPlayer AnimationPlayer => GetNode<AnimationPlayer>("%AnimationPlayer");
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}
}
