using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

using neco_soft.NecoBowlGodot.Program;
using neco_soft.NecoBowlGodot.Program.ResourceTypes;
using neco_soft.NecoBowlGodot.Program.Ui;
using neco_soft.NecoBowlGodot.Program.Ui.CardInformationPanel;
using NecoBowl.Core.Input;
using NecoBowl.Core.Model;
using NecoBowl.Core.Reports;
using NecoBowl.Core.Sport.Tactics;
using NecoBowl.Core.Tactics;
using NLog;
using Asset = neco_soft.NecoBowlGodot.Program.Loader.Asset;

public partial class Playfield : Control
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public Play? Play = null!;
	
    private Control SpaceLines => GetNode<Control>("%SpaceLines");

    private Button StepButton => GetNode<Button>("%StepButton");
    private Button StartButton => GetNode<Button>("%StartButton");
    private PlayLog PlayLog => GetNode<PlayLog>($"%{nameof(PlayLog)}");

    private RichTextLabel MoneyDisplay => GetNode<RichTextLabel>("%MoneyDisplay");
    private CardInformationPanel CardInfo => GetNode<CardInformationPanel>($"%{nameof(CardInfo)}");
    private CheckBox AutoplayToggle => GetNode<CheckBox>($"%{nameof(AutoplayToggle)}");
    private Button EndPlayButton => GetNode<Button>($"%{nameof(EndPlayButton)}");
    
    public TabContainer RightPanelTabs => GetNode<TabContainer>($"%{nameof(RightPanelTabs)}");

    private CardPlayOptionMenu? CardPlayOptionMenu;

    private StepDirector? Director = null;

    private PlayfieldState CurrentState => Play is null ? PlayfieldState.Plan : PlayfieldState.Play;
    private CardModel? SelectedCardModel = null;
    private bool Autoplay = true;

    public override void _Ready()
    {
        // Unit grid
        var unitSelect = GetNode<HandWidget>("%AllUnits");
        unitSelect.UnitPressed += OnCardSelected;

        StartButton.Pressed += StartPlay;
		
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
            ContextSingleton.Context.SendInput(new NecoInput.RequestEndPlay(ContextSingleton.Context.Players.Offense));
            ContextSingleton.Context.SendInput(new NecoInput.RequestEndPlay(ContextSingleton.Context.Players.Defense));
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

        UpdateControls();
        Populate();
    }

    private void StartStep()
    {
        if (CurrentState != PlayfieldState.Play) {
            throw new InvalidOperationException($"invalid state {CurrentState}");
        }

        StepButton.Disabled = true;

        var mutations = Play!.Step();
        Director = new(this);
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

    public Control GetGridSpace((int x, int y) coords)
        => SpaceLines.GetChild(SpaceLines.GetChildren().Count - 1 - coords.y).GetChild<Control>(coords.x);

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

    private void StartPlay()
    {
        ContextSingleton.Context.FinishTurn();
        Play = ContextSingleton.Context.GetPlay();
        Populate();

        RightPanelTabs.CurrentTab = 1;
        PlayLog.ClearText();

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
        MoneyDisplay.Text = ContextSingleton.Context.Push.RemainingMoney(NecoPlayerRole.Offense).ToString();
    }

    private void Populate()
    {
        if (Play?.IsFinished ?? false) {
            // Play just finished.
            Play = null;
            RightPanelTabs.CurrentTab = 0;
        }
        
        UpdateControls();
        UpdateInfoDisplay();
        
        // Grid spaces
        SpaceLines.RemoveAndFreeChildren();
        var fieldParams = ContextSingleton.Context.FieldParameters;

        for (var y = fieldParams.Bounds.Y - 1; y >= 0; y--) {
            var container = new HBoxContainer { Name = y.ToString() };
            
            for (var x = 0; x < fieldParams.Bounds.X; x++) {
                PlayfieldSpace space;
                
                if (CurrentState == PlayfieldState.Plan) {
                    var turn = ContextSingleton.Context.GetPlan(NecoPlayerRole.Offense);
                    var cardPlay = turn.CardPlayAt((x, y));
                    space = PlayfieldSpace.InstantiateForPlan(cardPlay, (x, y), fieldParams.GetPlayerAffiliation((x, y)));
                    space.Pressed += () => OnSpacePressed(space);
                } else if (CurrentState == PlayfieldState.Play) {
                    var field = Play!.GetPlayfield();
                    space = PlayfieldSpace.InstantiateForPlay(field[x, y], (x, y));
                } else {
                    throw new ApplicationException("invalid state");
                }

                container.AddChild(space);
            }
            SpaceLines.AddChild(container);
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
        if (SelectedCardModel is not null) {
            if (!space.PlayerRole.HasValue)
                return;
            var playerId = ContextSingleton.Context.Players[space.PlayerRole!.Value];
            ContextSingleton.Context.SendInput(new NecoInput.PlaceCard(
                playerId,
                new Card(SelectedCardModel),
                space.Coords));
            DeselectCard();
        } else {
            var cardPlay = ContextSingleton.Context.GetPlan(NecoPlayerRole.Offense).CardPlayAt(space.Coords);
            if (cardPlay is not null) {
                CardPlayOptionMenu = CardPlayOptionMenu.Instantiate();
                AddChild(CardPlayOptionMenu);
                CardPlayOptionMenu.PopulateWithCard(cardPlay.Card);
                CardPlayOptionMenu.GlobalPosition = space.GlobalPosition;
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
        CardInfo.UpdateFromCard(new Card((UnitCardModel)card.CardModel), CardInformationPanel_NodeCardStatus.Variant.InHand);
    }

    private void UpdateSpaceHoverTexture()
    {
        if (SelectedCardModel is null)
            return;
        
        foreach (var space in GetGridSpaces()) {
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