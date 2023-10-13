using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

using neco_soft.NecoBowlGodot.Program.Ui;
using NecoBowl.Core.Tactics;
using Asset = neco_soft.NecoBowlGodot.Program.Loader.Asset;

public partial class CardPlayOptionMenu : PanelContainer
{
	public static CardPlayOptionMenu Instantiate()
	{
		var node = GD.Load<PackedScene>(Common.GetSceneFile()).Instantiate<CardPlayOptionMenu>();
		return node;
	}

	private RichTextLabel CardNameDisplay => GetNode<RichTextLabel>("%CardName");
	private TextureRect CardIcon => GetNode<TextureRect>("%CardIcon");
	private Control OptionsList => GetNode<Control>("%OptionList");
	private IEnumerable<CardOptionLine> OptionsListContents => OptionsList.GetChildren().Cast<CardOptionLine>();

	private bool ControlLock = false;
	
	public override void _Ready()
	{
		CardNameDisplay.Text = string.Empty;
		OptionsList.RemoveAndFreeChildren();
	}

	private void AddOption(Card card, string optionId)
	{
		var line = CardOptionLine.Instantiate(card, optionId);
		OptionsList.AddChild(line);
	}

	public void PopulateWithCard(Card card)
	{
		CardIcon.Texture = Asset.Card.From(card).Icon;
		CardNameDisplay.Text = card.CardModel.Name;
		
		OptionsList.RemoveAndFreeChildren();

		foreach (var (id, option) in card.Options) {
			AddOption(card, id);
		}
	}
}
