using Godot;
using System;
using System.Collections.Generic;

using neco_soft.NecoBowlCore.Action;
using neco_soft.NecoBowlDefinitions.Unit;
using neco_soft.NecoBowlGodot.Program.Ui;

using NLog;

public partial class Playfield : Control
{
	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	private NecoPlay Play = null!;
	
	private Control GridHolder => GetNode<Control>("%GridHolder");
	private GridContainer Grid => GridHolder.GetNode<GridContainer>(nameof(Grid));

	private Button StepButton => GetNode<Button>("%StepButton");
	
	public override void _Ready()
	{
		if (Play is null) {
			Logger.Warn("Using default value for Play");
			Play = new NecoPlay(new NecoField(5, 5));
		}
		
		// Scenarios
		var scenarios = GetNode<Control>("%ScenariosList");
		foreach (var (name, func) in Scenarios) {
			var button = new Button {
				Text = name
			};
			button.Pressed += () => {
				Play = func();
				Populate(Play.Field);
			};
			scenarios.AddChild(button);
		}
		
		// Step
		StepButton.Pressed += () => {
			Play.Step();
			Populate(Play.Field);
		};

		Populate(Play.Field);
	}

	public void Populate(NecoField? field = null)
	{
		// Assemble the grid node
		GridHolder.RemoveAndFreeChildren();
		GridHolder.AddChild(new GridContainer { Name = nameof(Grid) });

		if (field is null) {
			throw new ApplicationException("field is not defined");
		}

		Grid.Columns = field.GetBounds().x;

		for (var y = field.GetBounds().y - 1; y >= 0; y--) {
			for (var x = 0; x < field.GetBounds().x; x++) {
				Grid.AddChild(PlayfieldSpace.Instantiate(field[x, y]));
			}
		}
		
		GridHolder.CustomMinimumSize = Grid.GetMinimumSize();
	}

	public Dictionary<string, Func<NecoPlay>> Scenarios = new Dictionary<string, Func<NecoPlay>>() {
		{
			"Test 1", () => {
				var play = new NecoPlay(new NecoField(5, 5));
				play.Field[0, 0] = new(new NecoUnit(Chicken.Instance));
				return play;
			}
		},
		{
			"Test 2", () => {
				var play = new NecoPlay(new NecoField(5, 5));
				play.Field[0, 0] = new(new NecoUnit(Chicken.Instance));
				play.Field[0, 5] = new(new NecoUnit(Boar.Instance));
				return play;
			}
		}
	};
}
