using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

using neco_soft.NecoBowlCore.Action;
using neco_soft.NecoBowlCore.Input;
using neco_soft.NecoBowlCore.Tactics;
using neco_soft.NecoBowlCore.Tags;
using neco_soft.NecoBowlGodot;
using neco_soft.NecoBowlGodot.Program;

public partial class CardOptionLine : HBoxContainer
{
	public static CardOptionLine Instantiate(NecoCard card, string optionId)
	{
		var scene = GD.Load<PackedScene>("Program/Ui/CardPlayOptionMenu/CardOptionLine.tscn");
		var line = scene.Instantiate<CardOptionLine>();
		line.Card = card;
		line.OptionId = optionId;
		return line;
	}

	private NecoCard Card = null!;
	private string OptionId = null!;

	private RichTextLabel OptionName => GetNode<RichTextLabel>("%OptionName");
	private OptionButton OptionDropdown => GetNode<OptionButton>("%OptionDropdown");
	
	private readonly List<object> OptionValues = new();

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		var permission = Card.CardModel.OptionPermissions.Single(p => p.Identifier == OptionId);

		OptionName.Text = permission.Identifier;
		foreach (var (opt, optDisplay) in permission.AllowedValues.Select(v => (v, permission.AllowedValueVisual(v)))) {
//			if (permission.GetType().Attributes)
			OptionDropdown.AddItem(optDisplay, OptionValues.Count);
			OptionValues.Add(opt);
		}

		OptionDropdown.ItemSelected += (index) => {
			var input = new NecoInput.SetPlanMod(
				ContextSingleton.Context.Players[NecoPlayerRole.Offense],
				Card,
				OptionId,
				OptionValues[(int)index]);
			ContextSingleton.Context.SendInput(input);
			
			UpdateDropdownValue();
		};
	}

	private void UpdateDropdownValue()
	{
		var selectedIndex = OptionValues.FindIndex(o => Card.Options[OptionId] == o);
		if (selectedIndex == -1)
			throw new InvalidOperationException($"invalid index in OptionValues ({selectedIndex})");
		OptionDropdown.Selected = selectedIndex;
	}

	public void SetControlLock(bool locked)
	{
		if (locked) {
			OptionDropdown.Disabled = true;
		} else {
			OptionDropdown.Disabled = false;
		}
	}
}
