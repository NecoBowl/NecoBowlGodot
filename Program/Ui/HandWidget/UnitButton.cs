using Godot;
using System;

using neco_soft.NecoBowlCore.Tactics;
using neco_soft.NecoBowlGodot;
using neco_soft.NecoBowlGodot.Program.Ui;

public partial class UnitButton : Button
{
	public static UnitButton Instantiate(NecoCard card)
	{
		var node = GD.Load<PackedScene>(Common.GetSceneFile()).Instantiate<UnitButton>();
		node.Card = card;
		return node;
	}

	public NecoCard Card { get; private set; } = null!;

	public override void _Ready()
	{
		GetNode<TextureRect>("%CardIcon").Texture = Asset.Card.From(Card.CardModel).Icon;
		GetNode<RichTextLabel>("%CardName").Text = $"[center]{Card.CardModel.Name}[/center]";
		GetNode<RichTextLabel>("%CardCost").Text = Card.Cost.ToString();
	}

	public override void _Process(double delta)
	{
		var boxSize = GetNode<Control>("Contents").Size;
		CustomMinimumSize = boxSize;
	}
}
