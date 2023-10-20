using Godot;
using System;
using System.Text.RegularExpressions;

using neco_soft.NecoBowlGodot.Program;
using neco_soft.NecoBowlGodot.Program.Ui;
using NecoBowl.Core.Machine;
using NecoBowl.Core.Reports;
using NecoBowl.Core.Sport.Tactics;
using NLog;
using Asset = neco_soft.NecoBowlGodot.Program.Loader.Asset;

public partial class PlayLog : MarginContainer
{
	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
	
	[Signal] public delegate void RollbackRequestedEventHandler(int stepNumber);

	[Export] public Color PlayerColor1 = Colors.CornflowerBlue;
	[Export] public Color PlayerColor2 = Colors.OrangeRed;
	[Export] public Color PlayerNeutralColor = Colors.Gray;

	private CheckButton ScrollLockToggle => GetNode<CheckButton>($"%{nameof(ScrollLockToggle)}");
	private Container LineContainer => GetNode<Container>($"%{nameof(LineContainer)}");
	private ScrollContainer ScrollContainer => GetNode<ScrollContainer>($"%{nameof(ScrollContainer)}");
	private double MaxVerticalScroll = 0;

	public void AddStepHeaderLine(int stepNumber)
	{
		var scene = GD.Load<PackedScene>($"{Common.GetFileResDirectory()}/PlayLog_LineStepHeader.tscn").Instantiate();
		var button = scene.GetNode<Button>("%Button");
		button.Text = $"Step {stepNumber}";
		button.Pressed += () => {
			EmitSignal(nameof(RollbackRequested), stepNumber);
		};
		LineContainer.AddChild(scene);
	}

	public void AddLine(BaseMutation mutation, NecoBowl.Core.Reports.Playfield field)
	{
		var line = new RichTextLabel() {
			Text = ParseMutationDescription(mutation.Description, field),
			ScrollActive = false,
			FitContent = true,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			BbcodeEnabled = true
		};
		LineContainer.AddChild(line);
	}

	private void ScrollToBottom()
	{
		if (Math.Abs(MaxVerticalScroll - ScrollContainer.GetVScrollBar().MaxValue) > double.Epsilon) {
			MaxVerticalScroll = ScrollContainer.GetVScrollBar().MaxValue;
			ScrollContainer.ScrollVertical = (int)MaxVerticalScroll;
		}
	}

	public string ParseMutationDescription(string mutationDescription, NecoBowl.Core.Reports.Playfield field)
	{
		var matches = NecoUnitId.StringIdRegex.Matches(mutationDescription);
		var sb = mutationDescription;
		foreach (Match match in matches) {
			Logger.Info(match.Groups[1].Value);
			var unit = field.LookupUnit(match.Groups[1].Value);
			sb = sb.Replace(match.Groups[0].Value, CreateUnitBbcode(unit!));
		}

		return sb;
	}

	public string CreateUnitBbcode(Unit unit)
	{
		NecoPlayerRole? role = unit.OwnerId == default ? null : this.NecoMatch().Context.Players.RoleOf(unit.OwnerId);
		string colorStr = role == NecoPlayerRole.Offense ? PlayerColor1.ToHtml()
			: role == NecoPlayerRole.Defense ? PlayerColor2.ToHtml()
			: PlayerNeutralColor.ToHtml();
		return $"[img]{Asset.Unit.FromModel(unit.UnitModel).GetStaticSprite().ResourcePath}[/img] [color={colorStr}]{unit.FullName}[/color]";
	}
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		MaxVerticalScroll = ScrollContainer.GetVScrollBar().MaxValue;
		ScrollContainer.GetVScrollBar().Changed += () => {
			if (ScrollLockToggle.ButtonPressed) {
				ScrollToBottom();
			}
		};
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void ClearText()
	{
		LineContainer.RemoveAndFreeChildren();
	}
}
