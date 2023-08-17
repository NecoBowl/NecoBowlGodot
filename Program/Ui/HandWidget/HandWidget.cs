using Godot;
using System;

using neco_soft.NecoBowlCore.Action;
using neco_soft.NecoBowlCore.Model;
using neco_soft.NecoBowlCore.Tactics;
using neco_soft.NecoBowlDefinitions;
using neco_soft.NecoBowlGodot;
using neco_soft.NecoBowlGodot.Program.Ui;

using NLog.LayoutRenderers.Wrappers;

public partial class HandWidget : Control
{
	[Signal] public delegate void UnitPressedEventHandler(Asset.Card card);

	private Control Grid => GetNode<Control>("%Grid");
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Grid.RemoveAndFreeChildren();
		
		foreach (var card in Asset.Card.All) {
			if (card.CardModel is not NecoUnitCardModel) {
				continue;
			}

			var button = UnitButton.Instantiate(new NecoUnitCard((NecoUnitCardModel)card.CardModel));
			button.Pressed += () => EmitSignal(nameof(UnitPressed), card);
			Grid.AddChild(button);
		}	
	}
}
