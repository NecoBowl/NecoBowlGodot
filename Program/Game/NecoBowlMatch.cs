using Godot;
using NecoBowl.Core;

namespace neco_soft.NecoBowlGodot.Program;

public partial class NecoBowlMatch : Node
{
    public NecoBowlContext Context { get; set; }

    public NecoBowlMatch()
    {
        Context = new(new());
    }
}