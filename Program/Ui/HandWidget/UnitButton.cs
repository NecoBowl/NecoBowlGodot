using Godot;

using neco_soft.NecoBowlGodot.Program.Ui;
using NecoBowl.Core.Tactics;
using Asset = neco_soft.NecoBowlGodot.Program.Loader.Asset;

public partial class UnitButton : Button
{
	public static UnitButton Instantiate(Card card)
	{
		var node = GD.Load<PackedScene>(Common.GetSceneFile()).Instantiate<UnitButton>();
		node.Card = card;
		return node;
	}

	public Card Card { get; private set; } = null!;

	public override void _Ready()
	{
		GetNode<TextureRect>("%CardIcon").Texture = Asset.Card.From(Card.CardModel).Icon;
//		GetNode<RichTextLabel>("%CardName").Text = $"[center]{Card.CardModel.Name}[/center]";
		GetNode<RichTextLabel>("%CardCost").Text = Card.Cost.ToString();
	}

	public override void _Process(double delta)
	{
		var boxSize = GetNode<Control>("Contents").Size;
		CustomMinimumSize = boxSize;
	}
}
