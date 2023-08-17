using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

using neco_soft.NecoBowlCore.Action;
using neco_soft.NecoBowlCore.Model;
using neco_soft.NecoBowlCore.Tactics;
using neco_soft.NecoBowlCore.Tags;
using neco_soft.NecoBowlDefinitions.Unit;
using neco_soft.NecoBowlGodot;
using neco_soft.NecoBowlGodot.Program.Ui;

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

	private void AddOption(NecoCard card, string optionId)
	{
		var line = CardOptionLine.Instantiate(card, optionId);
		if (ControlLock) {
			line.SetControlLock(true);
		}
		OptionsList.AddChild(line);
	}

	public void PopulateWithCard(NecoCard card)
	{
		CardIcon.Texture = Asset.Card.From(card).Icon;
		CardNameDisplay.Text = card.CardModel.Name;
		
		OptionsList.RemoveAndFreeChildren();

		foreach (var (id, option) in card.Options) {
			AddOption(card, id);
		}
	}

	private void SetControlsLocked(bool locked)
	{
		foreach (var option in OptionsListContents) {
			option.SetControlLock(locked);
		}

		ControlLock = locked;
	}
}
