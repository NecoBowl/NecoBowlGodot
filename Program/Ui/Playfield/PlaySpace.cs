using Godot;
using NecoBowl.Core.Reports;

namespace neco_soft.NecoBowlGodot.Program.Ui.Playfield;

public partial class PlaySpace : PlayfieldSpace
{
	public Space Space = null!;
	
    public UnitOnPlayfield? PlayUnitDisplay { get; set; } = null!;

	public static PlaySpace New(Space space, (int, int) coords)
	{
		var @this = GD.Load<PackedScene>(Common.GetSceneFile()).Instantiate<PlaySpace>();
		@this.Init(coords, space.Role);
			
		@this.Space = space;
		
        @this.SetMeta("coordsX", coords.Item1);
        @this.SetMeta("coordsY", coords.Item2);

		return @this;
	}
	
	public override void _Ready()
	{
		if (Space.Unit is not null) {
			var unitOnSpace = UnitOnPlayfield.New(Space.Unit);
			PlayUnitDisplay = unitOnSpace;
			AddChild(unitOnSpace);
		}		
	}

	protected override Color CircleColor => Colors.WhiteSmoke;
}