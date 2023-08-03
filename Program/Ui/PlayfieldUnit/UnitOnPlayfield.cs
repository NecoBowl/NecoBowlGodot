using Godot;
using System;

using neco_soft.NecoBowlCore.Action;
using neco_soft.NecoBowlCore.Input;

namespace neco_soft.NecoBowlGodot.Program.Ui.Playfield;

public partial class UnitOnPlayfield : Control
{
    public static UnitOnPlayfield Instantiate(NecoUnitInformation unit)
    {
        var node = GD.Load<PackedScene>(Common.GetSceneFile()).Instantiate<UnitOnPlayfield>();
        node.UnitInformation = unit;
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

    public void Populate(NecoUnitInformation unit)
    {
        UnitSprite.SpriteFrames = Asset.Unit.FromModel(unit.UnitModel).GetSpriteFrames();
        UnitPower.Text = unit.Power.ToString();
        UnitHealth.Text = unit.CurrentHealth != unit.MaxHealth
            ? $"{unit.CurrentHealth} / {unit.MaxHealth}"
            : $"{unit.CurrentHealth}";
    }
}
