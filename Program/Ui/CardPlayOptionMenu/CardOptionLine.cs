using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

using neco_soft.NecoBowlGodot.Program;
using neco_soft.NecoBowlGodot.Program.Ui;
using NecoBowl.Core.Input;
using NecoBowl.Core.Sport.Tactics;
using NecoBowl.Core.Tactics;
using NecoBowl.Core.Tags;

public partial class CardOptionLine : HBoxContainer
{
	public static CardOptionLine Instantiate(Card card, string optionId)
	{
		var scene = GD.Load<PackedScene>("Program/Ui/CardPlayOptionMenu/CardOptionLine.tscn");
		var line = scene.Instantiate<CardOptionLine>();
		line.Card = card;
		line.OptionId = optionId;
		line.Permission = line.Card.CardModel.OptionPermissions.Single(p => p.Identifier == line.OptionId);
		return line;
	}
	
	[Export] public Theme? OptionItemButtonTheme = null;

	private Card Card = null!;
	private string OptionId = null!;
	private CardOptionPermission Permission = null!;

	private RichTextLabel OptionName => GetNode<RichTextLabel>("%OptionName");
	private Container OptionItems => GetNode<Container>($"%{nameof(OptionItems)}");
	
	public override void _Ready()
	{
		OptionName.Text = Permission.Identifier;

		var currentValue = Card.Options.GetValue(Permission.Identifier);

		if (Permission.ArgumentType == typeof(bool)) {
			PopulateBoolean((bool)currentValue!);
		} else {
			PopulateButtons(currentValue);
		}
	}

	private void PopulateBoolean(bool currentValue)
	{
		OptionItems.RemoveAndFreeChildren();

		var button = new CheckBox() { ButtonPressed = currentValue };
		button.Pressed += () => SendInput(button.ButtonPressed);
		
		OptionItems.AddChild(button);
	}
	
	private void PopulateButtons(object? currentValue)
	{
		OptionItems.RemoveAndFreeChildren();
		
		var buttonGroup = new ButtonGroup();
		foreach (var (optionDisplay, optionValue) in Permission.GetOptionItems()) {
			var button = new Button() {
				Text = optionDisplay,
				ButtonGroup = buttonGroup,
				Flat = true,
				Theme = OptionItemButtonTheme,
				ToggleMode = true,
				ButtonPressed = optionValue.Equals(currentValue)
			};

			button.Pressed += () => {
				SendInput(optionValue);
			};

			OptionItems.AddChild(button);
		}
	}

	private void SendInput(object optionValue)
		=> this.NecoMatch().Context.SendInput(new NecoInput.SetPlanMod(
			this.NecoMatch().Context.Players[this.NecoClient().PlayerRole].Id,
			Card,
			OptionId,
			optionValue
		));
}
