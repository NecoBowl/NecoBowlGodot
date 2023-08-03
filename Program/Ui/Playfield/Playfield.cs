using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

using Microsoft.VisualBasic;

using neco_soft.NecoBowlCore.Action;
using neco_soft.NecoBowlCore.Input;
using neco_soft.NecoBowlCore.Model;
using neco_soft.NecoBowlCore.Tactics;
using neco_soft.NecoBowlDefinitions;
using neco_soft.NecoBowlDefinitions.Unit;
using neco_soft.NecoBowlGodot;
using neco_soft.NecoBowlGodot.Program;
using neco_soft.NecoBowlGodot.Program.Ui;

using NLog;

public partial class Playfield : Control
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private NecoPlayInformation? Play = null!;
	
    private Control SpaceLines => GetNode<Control>("%SpaceLines");
    private CardPlayOptionMenu CardPlayOptionMenu => GetNode<CardPlayOptionMenu>("%CardPlayOptionMenu");

    private Button StepButton => GetNode<Button>("%StepButton");
    private Button StartButton => GetNode<Button>("%StartButton");

    private RichTextLabel MoneyDisplay => GetNode<RichTextLabel>("%MoneyDisplay");

    private PlayfieldState CurrentState => Play is null ? PlayfieldState.Plan : PlayfieldState.Play;
    private NecoUnitCardModel? SelectedCardModel = null;
	
    public override void _Ready()
    {
        // Unit grid
        var unitSelect = GetNode<HandWidget>("%AllUnits");
        unitSelect.UnitPressed += OnCardSelected;

        StartButton.Pressed += StartPlay;
		
        // Step
        StepButton.Pressed += () => {
            if (CurrentState != PlayfieldState.Play) {
                throw new InvalidOperationException($"invalid state {CurrentState}");
            }
            
            var mutations = Play!.Step();
            StepDirector director = new StepDirector(this);
            AddChild(director);
            director.ApplyStep(mutations);
            director.DirectorFinished += () => OnStepDirectorFinished(director);
        };

        UpdateControls();
        Populate();
    }

    public Control GetGridSpace((int x, int y) coords)
        => SpaceLines.GetChild(SpaceLines.GetChildren().Count - 1 - coords.y).GetChild<Control>(coords.x);

    public List<PlayfieldSpace> GetGridSpaces()
        => SpaceLines.GetChildren().SelectMany(line => line.GetChildren()).Cast<PlayfieldSpace>().ToList();

    private void StartPlay()
    {
        ContextSingleton.Context.FinishTurn();
        Play = ContextSingleton.Context.BeginPlay();
        Populate();
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
        Logger.Debug("Redrawing field");
        
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
                CardPlayOptionMenu.PopulateWithCard(cardPlay.Card);            
            } 
        }
        Populate();
    }

    private void OnCardSelected(Asset.Card card)
    {
        if (CurrentState != PlayfieldState.Plan)
            return;
        
        SelectedCardModel = (NecoUnitCardModel)card.CardModel;
        UpdateSpaceHoverTexture();
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