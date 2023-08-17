using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

using neco_soft.NecoBowlCore.Input;
using neco_soft.NecoBowlCore.Model;
using neco_soft.NecoBowlCore.Tactics;
using neco_soft.NecoBowlGodot;
using neco_soft.NecoBowlGodot.Program;
using neco_soft.NecoBowlGodot.Program.Ui;
using neco_soft.NecoBowlGodot.Program.Ui.CardInformationPanel;

using NLog;

public partial class Playfield : Control
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private NecoPlayInformation? Play = null!;
	
    private Control SpaceLines => GetNode<Control>("%SpaceLines");

    private Button StepButton => GetNode<Button>("%StepButton");
    private Button StartButton => GetNode<Button>("%StartButton");

    private RichTextLabel MoneyDisplay => GetNode<RichTextLabel>("%MoneyDisplay");
    private CardInformationPanel CardInfo => GetNode<CardInformationPanel>($"%{nameof(CardInfo)}");
    private CheckBox AutoplayToggle => GetNode<CheckBox>($"%{nameof(AutoplayToggle)}");
    private Button EndPlayButton => GetNode<Button>($"%{nameof(EndPlayButton)}");

    private CardPlayOptionMenu? CardPlayOptionMenu;

    private PlayfieldState CurrentState => Play is null ? PlayfieldState.Plan : PlayfieldState.Play;
    private NecoUnitCardModel? SelectedCardModel = null;
    private bool Autoplay = false;
	
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
        };
        AutoplayToggle.ButtonPressed = Autoplay;

        EndPlayButton.Pressed += () => {
            ContextSingleton.Context.SendInput(new NecoInput.RequestEndPlay(ContextSingleton.Context.Players.Offense));
            ContextSingleton.Context.SendInput(new NecoInput.RequestEndPlay(ContextSingleton.Context.Players.Defense));
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

        var mutations = Play!.Step();
        StepDirector director = new StepDirector(this);
        AddChild(director);
        director.ApplyStep(mutations);
        director.DirectorFinished += () => OnStepDirectorFinished(director);
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
        Play = ContextSingleton.Context.BeginPlay();
        Populate();
        
        if (Autoplay)
            StartStep();
    }

    private void UpdateControls()
    {
        switch (CurrentState) {
            case PlayfieldState.Plan:
                StepButton.Visible = false;
                StartButton.Visible = true;
                break;
            case PlayfieldState.Play:
                StepButton.Visible = true;
                StartButton.Visible = false;
                break;
        }
    }

    private void UpdateInfoDisplay()
    {
        MoneyDisplay.Text = ContextSingleton.Context.Push.RemainingMoney(NecoPlayerRole.Offense).ToString();
    }

    private void Populate()
    {
        if (Play?.IsFinished ?? false) {
            Play = null;
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
                    var turn = ContextSingleton.Context.GetTurn();
                    var cardPlay = turn.CardPlayAt((x, y));
                    space = PlayfieldSpace.InstantiateForPlan(cardPlay, (x, y), fieldParams.GetPlayerAffiliation((x, y)));
                    space.Pressed += () => OnSpacePressed(space);
                } else if (CurrentState == PlayfieldState.Play) {
                    var field = Play!.Field;
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

    private void OnStepDirectorFinished(StepDirector director)
    {
        RemoveChild(director);
        Populate();

        if (Autoplay) {
            SetStepTimer();
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
                new NecoUnitCard(SelectedCardModel),
                space.Coords));
            DeselectCard();
        } else {
            var cardPlay = ContextSingleton.Context.GetTurn().CardPlayAt(space.Coords);
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
        var timer = GetTree().CreateTimer(0.5f);
        timer.Timeout += StartStep;
    }

    private void OnCardSelected(Asset.Card card)
    {
        if (CurrentState != PlayfieldState.Plan)
            return;
        
        SelectedCardModel = (NecoUnitCardModel)card.CardModel;
        UpdateSpaceHoverTexture();
        CardInfo.UpdateFromCard(new NecoUnitCard((NecoUnitCardModel)card.CardModel), CardInformationPanel_NodeCardStatus.Variant.InHand);
        // TODO Don't make a new Necocard!
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