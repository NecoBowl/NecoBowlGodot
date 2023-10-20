using Godot;
using System;

using NecoBowl.Core.Reports;
using Chicken = neco_soft.NecoBowlDefinitions.Unit.Chicken;

namespace neco_soft.NecoBowlGodot.Program.Ui.Playfield;

public partial class UnitOnPlayfield : Control
{
    [Export]
    public Color MaxHealthTextColor = Colors.LawnGreen;

    public AnimationPlayer AnimationPlayer => GetNode<AnimationPlayer>($"%{nameof(AnimationPlayer)}");

    public static UnitOnPlayfield New(Unit unit)
    {
        var node = GD.Load<PackedScene>(Common.GetSceneFile()).Instantiate<UnitOnPlayfield>();
        node.SetAnchorsPreset(LayoutPreset.FullRect);
        node.UnitInformation = unit;
        return node;
    }

    private Unit? UnitInformation = null!;

    public override void _Ready()
    {
        if (UnitInformation is null) {
            // TODO Expose some "default" UnitInformation so we don't have to return here
            GD.PushWarning("no unit specified for UnitOnPlayfield, using default");
            return;
        }
        
        Populate(UnitInformation);
        UnitSprite.Play();
    }

    private AnimatedSprite2D UnitSprite => GetNode<AnimatedSprite2D>("%UnitSprite");
    private RichTextLabel UnitPower => GetNode<RichTextLabel>("%UnitPower");
    private RichTextLabel UnitHealth => GetNode<RichTextLabel>("%UnitHealth");
    public CpuParticles2D ParticlesPickup => GetNode<CpuParticles2D>($"%{nameof(ParticlesPickup)}");

    public void Populate(Unit unit)
    {
        UnitSprite.SpriteFrames = Loader.Asset.Unit.FromModel(unit.UnitModel).GetSpriteFrames();
        var (scaleX, scaleY) = Size / UnitSprite.SpriteFrames.GetFrameTexture("default", 0).GetSize();
        var scale = Math.Min(scaleX, scaleY);
        UnitSprite.Scale = new(scale, scale);
        UnitPower.Text = unit.Power.ToString();

        if (unit.CurrentHealth == unit.MaxHealth) {
            UnitHealth.AddThemeColorOverride("default_color", MaxHealthTextColor);
        }
        UnitHealth.Text = unit.CurrentHealth != unit.MaxHealth
            ? $"{unit.CurrentHealth} / {unit.MaxHealth}"
            : $"{unit.CurrentHealth}";
    }
}
