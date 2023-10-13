using Godot;
using System;

using neco_soft.NecoBowlGodot.Program.Ui;
using NecoBowl.Core.Model;
using NecoBowl.Core.Tactics;
using NLog.LayoutRenderers.Wrappers;
using Asset = neco_soft.NecoBowlGodot.Program.Loader.Asset;

public partial class HandWidget : Control
{
	[Signal] public delegate void UnitPressedEventHandler(Asset.Card card);

	private Control Grid => GetNode<Control>("%Grid");
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Grid.RemoveAndFreeChildren();
		
		foreach (var card in Asset.Card.All) {
			if (card.CardModel is not UnitCardModel) {
				continue;
			}

			var button = UnitButton.Instantiate(new UnitCard((UnitCardModel)card.CardModel));
			button.Pressed += () => EmitSignal(nameof(UnitPressed), card);
			Grid.AddChild(button);
		}	
	}
}
