using Godot;

using neco_soft.NecoBowlCore.Action;
using neco_soft.NecoBowlCore.Input;

namespace neco_soft.NecoBowlGodot.Program.ResourceTypes;

public partial class PlayfieldMutation : Resource
{
    public readonly NecoPlayfieldMutation Mutation;

    public PlayfieldMutation(NecoPlayfieldMutation mutation)
    {
        Mutation = mutation;
    }
}