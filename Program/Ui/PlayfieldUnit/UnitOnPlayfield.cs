using Godot;
using System;
using System.Diagnostics.CodeAnalysis;

using neco_soft.NecoBowlCore.Action;
using neco_soft.NecoBowlCore.Input;

namespace neco_soft.NecoBowlGodot.Program.Ui.Playfield;

public partial class UnitOnPlayfield : Control
{
    [Export]
    public Color MaxHealthTextColor = Colors.LawnGreen;
    
    public static UnitOnPlayfield Instantiate(NecoUnitInformation unit)
    {
        var node = GD.Load<PackedScene>(Common.GetSceneFile()).Instantiate<UnitOnPlayfield>();
        node.UnitInformation = unit;
        node.SetAnchorsPreset(LayoutPreset.FullRect);
        return node;
    }

    private NecoUnitInformation UnitInformation = null!;

    public override void _Ready()
    {
        Populate(UnitInformation);
        UnitSprite.Play();
    }

    private AnimatedSprite2D UnitSprite => GetNode<AnimatedSprite2D>("%UnitSprite");
    private RichTextLabel UnitPower => GetNode<RichTextLabel>("%UnitPower");
    private RichTextLabel UnitHealth => GetNode<RichTextLabel>("%UnitHealth");
    public CpuParticles2D ParticlesPickup => GetNode<CpuParticles2D>($"%{nameof(ParticlesPickup)}");

    public void Populate(NecoUnitInformation unit)
    {
        UnitSprite.SpriteFrames = Asset.Unit.FromModel(unit.UnitModel).GetSpriteFrames();
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
