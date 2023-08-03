using System.Collections.Generic;
using System.Linq;

using Godot;

using neco_soft.NecoBowlCore.Action;

using NLog;

public partial class StepDirector : Node
{
    [Signal] public delegate void DirectorFinishedEventHandler();
    
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private Dictionary<Sprite2D, Vector2> PositionModifications = new();
    
    private Playfield Playfield;
    private List<Tween> ActiveTweens = new();

    public StepDirector(Playfield field)
    {
        Playfield = field;
    }

    public void ApplyStep(IEnumerable<NecoPlayfieldMutation> mutations)
    {
        var unitIdMap = new Dictionary<NecoUnitId, PlayfieldSpace>();
        foreach (var space in Playfield.GetGridSpaces()) {
            if (space.SpaceContents?.Unit is not null) {
                unitIdMap[space.SpaceContents.Unit.Id] = space;
            }
        }

        foreach (var mut in mutations) {
            if (mut is NecoPlayfieldMutation.MoveUnit moveMut) {
                var space = unitIdMap[moveMut.Subject];
                var destSpace = Playfield.GetGridSpace(moveMut.DestSpace);
                var tween = CreateDirectorTween();
                tween.TweenProperty(space.PlayUnitDisplay, "global_position", destSpace.GlobalPosition, 0.5);
            }
            
            else if (mut is NecoPlayfieldMutation.FightUnits fightMut) {
                var unit1Space = unitIdMap[fightMut.Unit1];
                var unit2Space = unitIdMap[fightMut.Unit2];

                CreateDirectorTween().CombatTween(unit1Space, unit2Space);
                CreateDirectorTween().CombatTween(unit2Space, unit1Space);
            }
        }
    }

    public Tween CreateDirectorTween()
    {
        var tween = CreateTween();
        ActiveTweens.Add(tween);
        tween.Finished += () => OnTweenFinished(tween);
        return tween;
    }

    private void OnTweenFinished(Tween tween)
    {
        ActiveTweens.Remove(tween);
        if (!ActiveTweens.Any()) {
            EmitSignal(nameof(DirectorFinished));
        }
    }
}

public static class TweenExt
{
    public static void CombatTween(this Tween tween, PlayfieldSpace unit1Space, PlayfieldSpace unit2Space)
    {
        var startingPos = unit1Space.GlobalPosition;
        tween.TweenProperty(unit1Space.PlayUnitDisplay,
            "position",
            unit1Space.GlobalPosition.DirectionTo(unit2Space.GlobalPosition) * 5,
            0.25f);
        tween.Chain()
            .TweenProperty(unit1Space.PlayUnitDisplay,
                "global_position",
                startingPos,
                0.25f);
    }
    
}