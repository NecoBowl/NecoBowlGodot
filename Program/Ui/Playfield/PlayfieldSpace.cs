using Godot;
using System;

using neco_soft.NecoBowlGodot.Program.Ui;
using neco_soft.NecoBowlGodot.Program.Ui.Playfield;
using NecoBowl.Core.Reports;
using NecoBowl.Core.Sport.Tactics;
using NLog;
using Asset = neco_soft.NecoBowlGodot.Program.Loader.Asset;

public partial class PlayfieldSpace : TextureButton
{
	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	public static PlayfieldSpace InstantiateForPlan(Plan.CardPlay? cardPlay, (int, int) coords, NecoPlayerRole? playerRole)
	{
		var node = GD.Load<PackedScene>(Common.GetSceneFile()).Instantiate<PlayfieldSpace>(); node.CardPlay = cardPlay;
		node.Coords = coords;
		node.State = PlayfieldState.Plan;
		node.PlayerRole = playerRole;
		
		node.AssertValidState();

		return node;
	}
	
	public static PlayfieldSpace InstantiateForPlay(Space contents, (int, int) coords)
	{
		var node = GD.Load<PackedScene>("res://Program/Ui/Playfield/PlayfieldSpace.tscn")
			.Instantiate<PlayfieldSpace>();

		node.SpaceContents = contents;
		node.Coords = coords;
		node.State = PlayfieldState.Play;

		node.AssertValidState();	
		
		return node;
	}

	public (int x, int y) Coords { get; private set; }

	public NecoPlayerRole? PlayerRole {
		get => SpaceContents is null ? _playerRole : SpaceContents.Role;
		private set => _playerRole = value;
	}

	private Plan.CardPlay? CardPlay = null!;
	public Space? SpaceContents { get; private set; } = null!;
	private PlayfieldState State;
	private NecoPlayerRole? _playerRole = null!;
	
	public UnitOnPlayfield? PlayUnitDisplay { get; set; } = null!;
	
	public override void _Ready()
	{
		if (Engine.IsEditorHint())
			return;
		
		switch (State) {
			case PlayfieldState.Plan:
				InitFromPlan();
				break;
			case PlayfieldState.Play:
				 InitFromPlay();
				 break;
		}
	}

	private void InitFromPlay()
	{
		if (SpaceContents!.Unit is not null) {
			PlayUnitDisplay = UnitOnPlayfield.Instantiate(SpaceContents.Unit);
			AddChild(PlayUnitDisplay);
			PlayUnitDisplay.Position = Vector2.Zero;
		}
	}

	private void InitFromPlan()
	{
		if (CardPlay is not null) {
			var texture = Asset.Card.From(CardPlay!.Card).Icon;
			var unitPreviewSprite = CreateStaticSpriteNode(texture);
			AddChild(unitPreviewSprite);
		}
	}

	private Sprite2D CreateStaticSpriteNode(Texture2D texture)
	{
		var unitPreviewSprite = new Sprite2D {
			Texture = texture,
			Modulate = new Color(1, 1, 1, 0.3f),
			TextureFilter = TextureFilterEnum.Nearest,
			Scale = Size / texture.GetSize(),
			Centered = false
		};
		return unitPreviewSprite;
	}

	public override void _Draw()
	{
		Color color;
		if (State is PlayfieldState.Plan) {
			color = PlayerRole == NecoPlayerRole.Offense ? Colors.Blue
				: PlayerRole == NecoPlayerRole.Defense ? Colors.Red
				: Colors.White;
		} else {
			color = Colors.White;
		}
		
		var radius = CustomMinimumSize.X / 2;
		DrawArc(CustomMinimumSize / 2, radius, 0, Mathf.Tau, 32, color, 1, true);
	}

	public void SetHoverSprite(Texture2D? texture)
	{
		StretchMode = StretchModeEnum.Scale;
		TextureFilter = TextureFilterEnum.Nearest;
		TextureHover = texture;
	}

	private void AssertValidState()
	{
		void Error() => throw new ApplicationException("invalid state");
		if (State == PlayfieldState.Play) {
			if (SpaceContents is null || CardPlay is not null) {
				Error();
			}
		} else if (State == PlayfieldState.Plan) {
			if (SpaceContents is not null) {
				Error();
			}
		}
	}
}