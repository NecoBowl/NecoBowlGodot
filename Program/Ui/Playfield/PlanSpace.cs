using Godot;
using neco_soft.NecoBowlGodot.Program.Loader;
using NecoBowl.Core;
using Reports = NecoBowl.Core.Reports;
using NecoBowl.Core.Sport.Tactics;

namespace neco_soft.NecoBowlGodot.Program.Ui.Playfield;

public partial class PlanSpace : PlayfieldSpace
{
    public Reports.Plan.CardPlay? CardPlay;
    public bool WasPlayedThisTurn;
    
    public static PlanSpace New(Reports.Plan.CardPlay? cardPlay, (int, int) coords, NecoPlayerRole? playerRole, bool playedThisTurn)
    {
        var @this = GD.Load<PackedScene>(Common.GetSceneFile()).Instantiate<PlanSpace>();
        ((PlayfieldSpace)@this).Init(coords, playerRole);
        @this.CardPlay = cardPlay;
        @this.WasPlayedThisTurn = playedThisTurn;
        
        @this.SetMeta("coordsX", coords.Item1);
        @this.SetMeta("coordsY", coords.Item2);
        
        return @this;
    }

    public override void _Ready()
    {
        if (CardPlay is not null) {
            var texture = Asset.Card.From(CardPlay.Card).Icon;
            var spriteAlpha = WasPlayedThisTurn ? 0.3f : 1f;
            var unitPreviewSprite = PlayfieldSpace.CreateStaticSpriteNode(texture, spriteAlpha);
            AddChild(unitPreviewSprite);
        }
    }

    protected override Color CircleColor => PlayerRole == NecoPlayerRole.Offense ? Colors.Blue
        : PlayerRole == NecoPlayerRole.Defense ? Colors.Red
        : Colors.WhiteSmoke;
}