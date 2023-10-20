using System;
using Godot;
using NecoBowl.Core;
using NecoBowl.Core.Reports;
using NecoBowl.Core.Sport.Tactics;
using NLog;
using Asset = neco_soft.NecoBowlGodot.Program.Loader.Asset;

namespace neco_soft.NecoBowlGodot.Program.Ui.Playfield;

public abstract partial class PlayfieldSpace : TextureButton
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private static readonly Vector2 NodeSize = new(52, 52);

    public void Init((int x, int y) coords, NecoPlayerRole? playerRole)
    {
        Coords = coords;
        PlayerRole = playerRole;
        
        CustomMinimumSize = NodeSize;
    }

    public (int x, int y) Coords { get; private set; }
    public NecoPlayerRole? PlayerRole { get; private set; }

    private PlayfieldState State;

    public override void _Ready()
    {
        throw new InvalidOperationException("how tf did this get instantiated");
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is CollisionObject2D.MouseShapeEnteredEventHandler)
        {
            Logger.Info("hovered"); 
        }
    }

    public static Sprite2D CreateStaticSpriteNode(Texture2D texture, float alpha = 1f)
    {
        var unitPreviewSprite = new Sprite2D {
            Texture = texture,
            Modulate = new Color(1, 1, 1, alpha),
            TextureFilter = TextureFilterEnum.Nearest,
            Scale = NodeSize / texture.GetSize(),
            Centered = false
        };
        return unitPreviewSprite;
    }

    public override void _Draw()
    {
        var radius = CustomMinimumSize.X / 2;
        DrawArc(CustomMinimumSize / 2, radius, 0, Mathf.Tau, 32, CircleColor, 1, true);
    }
    
    protected virtual Color CircleColor => Colors.White;

    public void SetHoverSprite(Texture2D? texture)
    {
        StretchMode = StretchModeEnum.Scale;
        TextureFilter = TextureFilterEnum.Nearest;
        TextureHover = texture;
    }
}