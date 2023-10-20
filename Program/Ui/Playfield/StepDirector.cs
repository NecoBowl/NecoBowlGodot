using System.Collections.Generic;
using System.Linq;
using Godot;
using neco_soft.NecoBowlGodot.Program.ResourceTypes;
using NecoBowl.Core;
using NecoBowl.Core.Machine;
using NecoBowl.Core.Machine.Mutations;
using NecoBowl.Core.Reports;
using NecoBowl.Core.Sport.Play;
using NLog;
using Asset = neco_soft.NecoBowlGodot.Program.Loader.Asset;

namespace neco_soft.NecoBowlGodot.Program.Ui.Playfield;

public partial class StepDirector : Node
{
    [Signal] public delegate void DirectorFinishedEventHandler();

    [Signal] public delegate void MutationFinishedEventHandler(PlayfieldMutation mut);
    
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private Dictionary<Sprite2D, Vector2> PositionModifications = new();
    private Queue<IEnumerable<BaseMutation>> MutationListQueue = new();

    private Playfield Playfield;
    private HashSet<DirectorTween> ActiveAnimations = new();

    private Step? Step;

    /// <summary>
    /// X and Y multipliers that should be applied to relative movements
    /// </summary>
    private Vector2 Transform;

    public StepDirector(Playfield field, Vector2 transform)
    {
        Playfield = field;
        Transform = transform;
    }

    public void ApplyStep(Step step)
    {
        Step = step;
        MutationListQueue.Enqueue(step.GetAllMutations());
        RunMutationGroup();
    }

    private bool RunMutationGroup()
    {
        if (!MutationListQueue.Any()) {
            return false;
        }
        
        var unitIdMap = new Dictionary<NecoUnitId, PlaySpace>();
        foreach (var space in Playfield.GetGridSpaces().Cast<PlaySpace>()) {
            if (space.Space?.Unit is not null) {
                unitIdMap[space.Space.Unit.Id] = space;
            }
        }

        var tweensByUnitId = new Dictionary<NecoUnitId, DirectorTween>();
        
        var muts = MutationListQueue.Dequeue().ToList();

        foreach (var movement in Step!.GetAllMovements().Where(s => s.IsChange)) {
            var unitSpace = unitIdMap[movement.UnitId];
            TweenForUnit(movement.UnitId).MoveTween(unitSpace.PlayUnitDisplay!, Playfield.GetGridSpace(movement.NewPos));
        }

        foreach (var mut in muts) {
            tweensByUnitId[mut.Subject] = TweenForUnit(mut.Subject);
        }

        foreach (var mut in muts)
        {
            var tween = TweenForUnit(mut.Subject);
            tween.EmptyTween(0.1f);

            if (mut is UnitAttacks attack)
            {
                var attackerSpace = unitIdMap[attack.Attacker];
                var receiverSpace = unitIdMap[attack.Receiver];

                if (mut is UnitAttacksOnSpace spaceAttack)
                {

                    var direction = attackerSpace.GlobalPosition.DirectionTo(receiverSpace.GlobalPosition).Normalized();
                    if (true)
                    {
                        tween.MoveTween(attackerSpace.PlayUnitDisplay!, Playfield.GetGridSpace(spaceAttack.ConflictPosition));
                        tween.Chain().MoveInDirectionTween(attackerSpace.PlayUnitDisplay!, -direction, 30f, 0.2f);
                        tween.Chain().EmptyTween(0.3f);
                        tween.Chain().MoveInDirectionTween(attackerSpace.PlayUnitDisplay!, direction, 25f, 0.1f);
                        tween.Chain().MoveTween(attackerSpace.PlayUnitDisplay!, attackerSpace);
//                        tween.MoveInDirectionTween(attackerSpace.PlayUnitDisplay!, direction.Inverse(), 30f, 0.2f);
                    }
                }
                else if (mut is UnitAttacksBetweenSpaces betweenSpaceAttack)
                {
                    tween.CombatTween(attackerSpace, receiverSpace);
                }
            }
            else if (mut is UnitPicksUpItem pickedUpItemMut)
            {
                var pickupParticle = unitIdMap[pickedUpItemMut.Subject].PlayUnitDisplay!.ParticlesPickup;
                pickupParticle.Emitting = true;
                tween.EmptyTween(0.5f);
            }
            else if (mut is UnitBumps bumpMut)
            {
                var space = unitIdMap[bumpMut.Subject];
                tween.BumpTween(space, bumpMut.Direction);
            }
            else if (mut is UnitHandsOffItem handoffMut)
            {
                var sourceSpace = unitIdMap[handoffMut.Subject];
                var destSpace = unitIdMap[handoffMut.Receiver];
                var handoffItem = Playfield.Play!.GetPlayfield().GetUnit(handoffMut.Item);
                var handoffSprite = new Sprite2D()
                    { Texture = Asset.Unit.FromModel(handoffItem!.UnitModel).GetStaticSprite() };
                sourceSpace.AddChild(handoffSprite);
                tween.Tween.TweenProperty(handoffSprite, "global_position", destSpace.PlayUnitDisplay!.GlobalPosition,
                    0.5f);
                tween.Tween.Finished += () =>
                {
                    sourceSpace.RemoveChild(handoffSprite);
                    handoffSprite.QueueFree();
                };

                tween.Chain().Tween
                    .TweenCallback(Callable.From(()
                        => EmitSignal(nameof(MutationFinished), new PlayfieldMutation(mut))));
            }
        }

        return true;
    }

    private DirectorTween TweenForUnit(NecoUnitId subject)
    {
        var existingTween = ActiveAnimations.FirstOrDefault(t => t.UnitId == subject);
        var tween = existingTween?.Tween ?? CreateTween();
        var directorTween = new DirectorTween(subject, tween, Transform);
        if (existingTween is null) {
            tween.Finished += () => OnAnimationFinished(directorTween);
        }

        ActiveAnimations.Add(directorTween);
        return directorTween;
    }

    private void OnAnimationFinished(DirectorTween directorAnimation)
    {
        ActiveAnimations.Remove(directorAnimation);
        if (!ActiveAnimations.Any()) {
            if (!RunMutationGroup()) {
                EmitSignal(nameof(DirectorFinished));
            }
        }
    }
    
    public record DirectorTween(NecoUnitId UnitId, Tween Tween, Vector2 Transform)
    {
        public readonly NecoUnitId UnitId = UnitId;
        public Tween Tween { get; private set; } = Tween;

        public void MoveTween(UnitOnPlayfield unit, PlayfieldSpace destSpace)
        {
            Tween.TweenProperty(unit,
                "global_position",
                destSpace.GlobalPosition,
                0.45f);
        }

        public void MoveInDirectionTween(UnitOnPlayfield unit, Vector2 direction, float distance, float time)
        {
            Tween.TweenProperty(unit, "position", direction * distance * Transform, time).AsRelative();
        }
        
        public void BumpTween(PlaySpace space, AbsoluteDirection direction)
        {
            var startingPos = space.PlayUnitDisplay!.GlobalPosition;
            Tween.TweenProperty(space.PlayUnitDisplay, "global_position", startingPos + direction.ToGodotVector2() * 15f * Transform, .25f);
            Tween.TweenProperty(space.PlayUnitDisplay, "global_position", startingPos, 0.25f);
        }

        public void CombatTween(PlaySpace unit1Space, PlayfieldSpace destSpace)
        {
            var startingPos = unit1Space.GlobalPosition;
            Tween.TweenProperty(unit1Space.PlayUnitDisplay,
                "global_position",
                destSpace.GlobalPosition,
                0.09f);
            Tween.Chain()
                .TweenProperty(unit1Space.PlayUnitDisplay,
                    "global_position",
                    startingPos,
                    0.25f);
        }

        public void ShakeTween(UnitOnPlayfield unit)
        {
            const float shakeMagnitude = 8f;
            const float shakeTime = 0.07f;
            var startPos = unit.Position;
            for (int i = 0; i < 10; i++) {
                Tween.Chain()
                    .TweenProperty(unit,
                        "position",
                        new Vector2(GD.Randf() * shakeMagnitude, GD.Randf() * shakeMagnitude),
                        shakeTime)
                    .AsRelative();
                Tween.Chain().TweenProperty(unit, "position", startPos, shakeTime);
            }
        }

        public void EmptyTween(double duration)
        {
            Tween.TweenInterval(duration);
        }

        public DirectorTween Chain()
        {
            Tween = Tween.Chain();
            return this;
        }
    }
}