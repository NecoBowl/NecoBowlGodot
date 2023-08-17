using System.Collections.Generic;
using System.Linq;

using Godot;

using neco_soft.NecoBowlCore;
using neco_soft.NecoBowlCore.Action;
using neco_soft.NecoBowlGodot.Program.Ui;

using NLog;

public partial class StepDirector : Node
{
    [Signal] public delegate void DirectorFinishedEventHandler();
    
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private Dictionary<Sprite2D, Vector2> PositionModifications = new();
    
    private Playfield Playfield;
    private List<DirectorTween> ActiveTweens = new();

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
            if (mut is NecoPlayfieldMutation.MovementMutation moveMut) {
                var space = unitIdMap[moveMut.Subject];
                var destSpace = Playfield.GetGridSpace(moveMut.NewPos);
                var tween = GetUnitTween(moveMut.Subject);
                tween.Tween.TweenProperty(space.PlayUnitDisplay, "global_position", destSpace.GlobalPosition, 0.5);
            }
            
            else if (mut is NecoPlayfieldMutation.UnitAttacks attack) {
                var unit1Space = unitIdMap[attack.Attacker];
                var unit2Space = unitIdMap[attack.Receiver];

                GetUnitTween(unit1Space.SpaceContents!.Unit!.Id).CombatTween(unit1Space, unit2Space);
            }
            
            else if (mut is NecoPlayfieldMutation.UnitPicksUpItem pickedUpItemMut) {
                var pickupParticle = unitIdMap[pickedUpItemMut.Subject].PlayUnitDisplay!.ParticlesPickup;
                pickupParticle.Emitting = true;
            }
            
            else if (mut is NecoPlayfieldMutation.UnitBumps bumpMut) {
                var space = unitIdMap[bumpMut.Subject];
                var tween = GetUnitTween(bumpMut.Subject);
                tween.Tween.TweenProperty(space.PlayUnitDisplay, "position", bumpMut.Direction.ToGodotVector2() * 15, 0.25f);
                tween.Tween.TweenProperty(space.PlayUnitDisplay, "position", Vector2.Zero, 0.25f);
            }
        }
    }

    private DirectorTween GetUnitTween(NecoUnitId subject)
    {
        var existingTween = ActiveTweens.FirstOrDefault(t => t.UnitId == subject);
        var tween = existingTween?.Tween ?? CreateTween();
        var directorTween = new DirectorTween(subject, tween);
        if (!ActiveTweens.Any(t => t.UnitId == directorTween.UnitId)) {
            ActiveTweens.Add(directorTween);
            tween.Finished += () => OnTweenFinished(tween);
        }

        return directorTween;
    }

    private void OnTweenFinished(Tween tween)
    {
        ActiveTweens.RemoveAll(t => t.Tween == tween);
        if (!ActiveTweens.Any()) {
            EmitSignal(nameof(DirectorFinished));
        }
    }

    private record class DirectorTween(NecoUnitId UnitId, Tween Tween)
    {
        public void CombatTween(PlayfieldSpace unit1Space, PlayfieldSpace unit2Space)
        {
            var startingPos = unit1Space.GlobalPosition;
            Tween.TweenProperty(unit1Space.PlayUnitDisplay,
                "position",
                unit1Space.GlobalPosition.DirectionTo(unit2Space.GlobalPosition) * 10,
                0.09f);
            Tween.Chain().TweenProperty(unit1Space.PlayUnitDisplay,
                    "global_position",
                    startingPos,
                    0.25f);
        }
    }
}
