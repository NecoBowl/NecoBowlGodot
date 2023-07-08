using Godot;
using System;
using System.Reflection;

using neco_soft.NecoBowlCore.Action;
using neco_soft.NecoBowlGodot;

public partial class PlayfieldSpace : Control
{
	public static PlayfieldSpace Instantiate(NecoField.NecoSpaceContents contents)
	{
		var node = GD.Load<PackedScene>("res://Program/Ui/Playfield/PlayfieldSpace.tscn")
			.Instantiate<PlayfieldSpace>();
		node.SpaceContents = contents;
		return node;
	}

	public NecoField.NecoSpaceContents SpaceContents;

	public override void _Ready()
	{
		if (SpaceContents.Unit is not null) {
			var texture = new Asset.Unit(SpaceContents.Unit.UnitModel).GetSpriteFrames();
			var spriteNode = new AnimatedSprite2D() {
				SpriteFrames = texture,
				Autoplay = "default",
				Centered = false
			};
			spriteNode.Scale = CustomMinimumSize / texture.GetFrameTexture("default", 0).GetSize();
			AddChild(spriteNode);
		}
	}

	public override void _Draw()
	{
		var radius = CustomMinimumSize.X / 2;
		DrawArc(CustomMinimumSize / 2, radius, 0, Mathf.Tau, 32, Colors.White);
	}
}
