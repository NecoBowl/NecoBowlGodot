using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using neco_soft.NecoBowlGodot.Program.Loader;
using neco_soft.NecoBowlGodot.Program.Networking;
using neco_soft.NecoBowlGodot.Program.ResourceTypes;
using NecoBowl.Core;
using NecoBowl.Core.Input;
using NecoBowl.Core.Model;
using NecoBowl.Core.Reports;
using NecoBowl.Core.Sport.Tactics;
using NecoBowl.Core.Tactics;
using NLog;

namespace neco_soft.NecoBowlGodot.Program.Ui.Playfield;

public partial class Playfield : Control
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static Playfield New(bool isSinglePlayer)
    {
        var @this = GD.Load<PackedScene>(Common.GetSceneFile()).Instantiate<Playfield>();
        @this.IsSinglePlayer = isSinglePlayer;
        return @this;
    }

    public Play? Play = null!;

    private NecoBowlMatch ContextSingleton => GetNode<NecoBowlMatch>("/root/NecoMatch");
	
    private Control SpaceLines => GetNode<Control>("%SpaceLines");

    private Button StepButton => GetNode<Button>("%StepButton");
    private PlayLog PlayLog => GetNode<PlayLog>($"%{nameof(PlayLog)}");

    private RichTextLabel TurnIndexDisplay => GetNode<RichTextLabel>("%TurnIndexDisplay");
    private RichTextLabel TurnStatusDisplay => GetNode<RichTextLabel>("%TurnStatusDisplay");
    private RichTextLabel MoneyDisplay => GetNode<RichTextLabel>("%MoneyDisplay");
    private CardInformationPanel.CardInformationPanel CardInfo => GetNode<CardInformationPanel.CardInformationPanel>($"%{nameof(CardInfo)}");
    private CheckBox AutoplayToggle => GetNode<CheckBox>($"%{nameof(AutoplayToggle)}");
    private Button PreviewPlayButton => GetNode<Button>($"%{nameof(PreviewPlayButton)}");
    private Button SubmitTurnButton => GetNode<Button>($"%{nameof(SubmitTurnButton)}");
    
    private Button EndPlayButton => GetNode<Button>($"%{nameof(EndPlayButton)}");
    
    public TabContainer RightPanelTabs => GetNode<TabContainer>($"%{nameof(RightPanelTabs)}");

    private CardPlayOptionMenu? CardPlayOptionMenu;

    private StepDirector? Director = null;

    private PlayfieldState CurrentState => Play is null ? PlayfieldState.Plan : PlayfieldState.Play;
    private CardModel? SelectedCardModel = null;
    private bool Autoplay = true;
    private bool ShouldEndPlay = false;
    private bool PlayIsPreview = false;
    private bool IsSinglePlayer = true;
    private uint TurnIndex = 0;
    private bool WaitingForOther = false;

    public override void _Ready()
    {
        TurnIndex = this.NecoMatch().Context.Push.CurrentTurnIndex;
        
        // Unit grid
        var unitSelect = GetNode<HandWidget>("%AllUnits");
        unitSelect.UnitPressed += OnCardSelected;

        PreviewPlayButton.Pressed += () => ShowPlay(true);
        SubmitTurnButton.Pressed += SubmitTurn;
		
        // Step
        StepButton.Pressed += StartStep;

        AutoplayToggle.Toggled += (state) => {
            Autoplay = state;

            if (Autoplay) {
                StepButton.Disabled = true;
                if (Director is null) {
                    StartStep();
                }
            } else {
                StepButton.Disabled = false;
            }
        };
        AutoplayToggle.ButtonPressed = Autoplay;

        EndPlayButton.Pressed += () => {
            if (!PlayIsPreview) {
                this.NecoClient().RequestEndPlay();
            }
            else {
                ShouldEndPlay = true;
            }

            Populate();
        };

        PlayLog.RollbackRequested += (turn) => {
            if (Play is null) {
                Logger.Error("Rollback requested but no play is active");
                return;
            }
            
            Play = ContextSingleton.Context.GetPlay();
            Play.Step((uint)turn);
            Populate();
        };

        this.NecoClient().InputSent += InputSubmitted;
        this.NecoClient().EndPlayRequested += EndPlayRequested;

        UpdateControls();
        Populate();
    }

    private void EndPlayRequested()
    {
        if (Play is null || PlayIsPreview)
        {
            Logger.Warn("weird");
            return;
        }

        ShouldEndPlay = true;
    }

    private void InputSubmitted()
    {
        Logger.Debug(this.NecoMatch().Context.Push.CurrentTurnIndex);
        if (this.NecoMatch().Context.Push.CurrentTurnIndex != TurnIndex)
        {
            // We advanced to a new turn.
            WaitingForOther = false;
            TurnIndex = this.NecoMatch().Context.Push.CurrentTurnIndex;
            ForceShowPlay();
        }
        else
        {
            if (this.NecoClient().Player is not null
             && this.NecoMatch()
                    .Context.SendInput(new NecoInput.RequestEndTurn(this.NecoClient().Player!.Id, true))
                    .ResponseKind == NecoInputResponse.Kind.Illegal) {
                // Waiting for other player to end turn
                WaitingForOther = true;
            }
            Populate();
        }
    }


    private void ForceShowPlay()
    {
        ShowPlay(false);
    }

    private void StartStep()
    {
        if (CurrentState != PlayfieldState.Play) {
            throw new InvalidOperationException($"invalid state {CurrentState}");
        }

        StepButton.Disabled = true;

        var mutations = Play!.Step();
        Director = new(this, this.NecoClient().PlayerRole == NecoPlayerRole.Defense ? -Vector2.One : Vector2.One);
        AddChild(Director);
        Director.ApplyStep(mutations);
        Director.MutationFinished += OnStepDirectorMutationFinished;
        Director.DirectorFinished += OnStepDirectorFinished;

        PlayLog.AddStepHeaderLine((int)Play.StepCount);
    }

    private void OnStepDirectorMutationFinished(PlayfieldMutation mut)
    {
        PlayLog.AddLine(mut.Mutation, Play!.GetPlayfield());
    }

    public PlayfieldSpace GetGridSpace((int x, int y) coords)
    => (PlayfieldSpace)SpaceLines.GetChildrenRecursive().Single(node => (int)node.GetMeta("coordsX", -1) == coords.x && (int)node.GetMeta("coordsY", -1) == coords.y);

    public List<PlayfieldSpace> GetGridSpaces()
        => SpaceLines.GetChildren().SelectMany(line => line.GetChildren()).Cast<PlayfieldSpace>().ToList();

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton { ButtonIndex: MouseButton.Left } mouseInput) {
            if (CardPlayOptionMenu is not null) {
                RemoveChild(CardPlayOptionMenu);
                CardPlayOptionMenu!.QueueFree();
                CardPlayOptionMenu = null;
            }
        }
    }

    private void SubmitTurn()
    {
        if (this.NecoClient().Player is null)
        {
            // Single-player mode, submit for both
            foreach (var player in this.NecoMatch().Context.Players.Enumerate())
            {
                this.NecoClient().SendNecoInput(new NecoInput.RequestEndTurn(player.Id, false));
            }
        }
        else
        {
            this.NecoClient().SendNecoInput(new NecoInput.RequestEndTurn(this.NecoClient().Player!.Id, false));
        }
    }

    private void ShowPlay(bool isPreview)
    {
        PlayIsPreview = isPreview;
        Play = ContextSingleton.Context.GetPlay();
        
        Populate();

        PlayLog.ClearText();
        RightPanelTabs.CurrentTab = 1;
        
        if (Autoplay)
            CallDeferred(nameof(StartStep));
    }

    private void UpdateControls()
    {
        foreach (var node in this.GetChildrenRecursive().OfType<Control>().Where(n => n.IsInGroup("ui_PlanPhase"))) {
            node.Visible = CurrentState == PlayfieldState.Plan;
        }
        foreach (var node in this.GetChildrenRecursive().OfType<Control>().Where(n => n.IsInGroup("ui_PlayPhase"))) {
            node.Visible = CurrentState == PlayfieldState.Play;
        }
    }

    private void UpdateInfoDisplay()
    {
        TurnIndexDisplay.Text = $"Turn {ContextSingleton.Context.Push.CurrentTurnIndex}";
        TurnStatusDisplay.Text = CurrentState == PlayfieldState.Plan ? "Planning" 
            : PlayIsPreview ? "Playing Preview" 
            : "Playing";
        MoneyDisplay.Text = ContextSingleton.Context.Push.RemainingMoney(this.NecoClient().PlayerRole).ToString();
    }

    private void Populate()
    {
        if (ShouldEndPlay) {
            Play = null;
            RightPanelTabs.CurrentTab = 0;

            ShouldEndPlay = false;
        }
        
        UpdateControls();
        UpdateInfoDisplay();
        
        // Grid spaces
        SpaceLines.RemoveAndFreeChildren();
        var fieldParams = ContextSingleton.Context.FieldParameters;

        for (var _y = fieldParams.Bounds.Y - 1; _y >= 0; _y--) {
            var container = new HBoxContainer { Name = _y.ToString() };
            
            for (var _x = 0; _x < fieldParams.Bounds.X; _x++)
            {
                var playerRole = this.NecoClient().PlayerRole;
                Vector2i coords = playerRole == NecoPlayerRole.Offense
                    ? (_x, _y)
                    : (Vector2i)this.NecoMatch().Context.FieldParameters.Bounds - (1 + _x, 1 + _y);
                PlayfieldSpace space;
                
                if (CurrentState == PlayfieldState.Plan) {
                    var affiliation = ContextSingleton.Context.FieldParameters.GetPlayerAffiliation((coords.X, coords.Y));
                    if (ContextSingleton.Context.GetTurn(playerRole).CardPlays
                            .SingleOrDefault(p => p.Position == (coords.X, coords.Y)) is { } turnCardPlay) {
                        // Card played this turn.
                        space = PlanSpace.New(turnCardPlay, (coords.X, coords.Y), affiliation, true);
                    } else if (ContextSingleton.Context.GetPlan(playerRole).CardPlayAt((coords.X, coords.Y)) is {} planCardPlay) {
                        // Card played a previous turn.
                        space = PlanSpace.New(planCardPlay, (coords.X, coords.Y), affiliation, false);
                    }
                    else
                    {
                        // Nothing on this space.
                        space = PlanSpace.New(null, (coords.X, coords.Y), affiliation, false);
                    }

                    space.Pressed += () => OnSpacePressed(space);
                } else if (CurrentState == PlayfieldState.Play) {
                    var field = Play!.GetPlayfield();
                    space = PlaySpace.New(field[coords.X, coords.Y], (coords.X, coords.Y));
                } else {
                    throw new ApplicationException("invalid state");
                }

                container.AddChild(space);
            }
            SpaceLines.AddChild(container);
        }

        if (Play is not null && this.NecoClient().LocalHasRequestedEndPlay())
        {
            EndPlayButton.SetMeta("oldtext", EndPlayButton.Text);
            EndPlayButton.Text = "Waiting for other player to end play...";
        } else if (Play is null && WaitingForOther)
        {
            SubmitTurnButton.SetMeta("oldtext", SubmitTurnButton.Text);
            SubmitTurnButton.Text = "Waiting for other player to end turn...";
        }
        
        UpdateSpaceHoverTexture();
    }

    private void OnStepDirectorFinished()
    {
        RemoveChild(Director);
        Director!.QueueFree();
        Director = null;
        
        Populate();

        if (Autoplay) {
            SetStepTimer();
        } else {
            StepButton.Disabled = false;
        }
    }

    private void OnSpacePressed(PlayfieldSpace space)
    {
        Logger.Debug($"Click on space {space.Coords}");
        if (SelectedCardModel is UnitCardModel unitCardModel) {
            if (!space.PlayerRole.HasValue)
                return;
            var player = ContextSingleton.Context.Players[space.PlayerRole!.Value];
            
            // For now, we create the card here.
            var newCard = new UnitCard(unitCardModel);
            this.NecoClient().SendNecoInput(new NecoInput.PlaceCard(
                player.Id,
                newCard,
                space.Coords));
            DeselectCard();
        } else {
            var cardPlay = ContextSingleton.Context.GetTurn(this.NecoClient().PlayerRole).CardPlays.SingleOrDefault(p => p.Position == space.Coords);
            if (cardPlay is not null) {
                CardPlayOptionMenu = CardPlayOptionMenu.Instantiate();
                AddChild(CardPlayOptionMenu);
                CardPlayOptionMenu.PopulateWithCard(cardPlay.Card);
                CardPlayOptionMenu.GlobalPosition = space.GlobalPosition;
            }
            else
            {
                var oldCardPlay = ContextSingleton.Context.GetPlan(this.NecoClient().PlayerRole).CardPlayAt(space.Coords);
                if (oldCardPlay is not null) {
                    // TODO Show static card info. 
                } 
            }
        }
        Populate();
    }

    private void SetStepTimer()
    {
        var timer = GetTree().CreateTimer(0.18f);
        timer.Timeout += () => {
            if (CurrentState == PlayfieldState.Play && Autoplay) StartStep();
        };
    }

    private void OnCardSelected(Asset.Card card)
    {
        if (CurrentState != PlayfieldState.Plan)
            return;
        
        SelectedCardModel = card.CardModel;
        UpdateSpaceHoverTexture();
        CardInfo.UpdateFromCard(new UnitCard((UnitCardModel)card.CardModel), CardInformationPanel_NodeCardStatus.Variant.InHand);
    }

    private void UpdateSpaceHoverTexture()
    {
        if (SelectedCardModel is null)
            return;
        
        foreach (var space in GetGridSpaces())
        {
            var sprite = Asset.Card.From(SelectedCardModel).Icon;
            space.SetHoverSprite(Asset.Card.From(SelectedCardModel).Icon);
        }
    }

    private void DeselectCard()
    {
        SelectedCardModel = null;
        foreach (var space in GetGridSpaces()) {
            space.SetHoverSprite(null);
        }
    }
}

public enum PlayfieldState
{
    Plan,
    Play
}