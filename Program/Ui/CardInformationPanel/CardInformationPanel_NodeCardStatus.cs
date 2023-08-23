using Godot;
using System;

using NLog;

public partial class CardInformationPanel_NodeCardStatus : PanelContainer
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [Export] private StyleBox InHand = null!;
    [Export] private StyleBox Player = null!;
    [Export] private StyleBox Enemy = null!;

    private RichTextLabel Label = null!;
    
    private Variant _variantKind;

    public override void _Ready()
    {
        Label = new() { Name = nameof(Label), FitContent = true, AutowrapMode = TextServer.AutowrapMode.Off };
        AddChild(Label);
        Update();
    }

    public void Update()
    {
        StyleBox styleBox;
        switch (VariantKind) {
            case Variant.InHand: {
                styleBox = InHand;
                Label.Text = "In Hand";
                break;
            }
            case Variant.Player: {
                styleBox = Player;
                Label.Text = "Offense";
                break;
            }
            case Variant.Enemy: {
                styleBox = Enemy;
                Label.Text = "Defense";
                break;
            }
            default:
                throw new();
        }
        
        if (styleBox is null)
           Logger.Warn("designated stylebox is null");
        
        AddThemeStyleboxOverride("panel", styleBox);
    }

    public enum Variant
    {
        InHand,
        Player,
        Enemy
    }

    public Variant VariantKind {
        get => _variantKind;
        set {
            _variantKind = value;
            Update();
        }
    }
}
