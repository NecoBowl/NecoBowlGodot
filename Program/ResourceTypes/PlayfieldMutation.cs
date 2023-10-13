using Godot;
using NecoBowl.Core.Machine;

namespace neco_soft.NecoBowlGodot.Program.ResourceTypes;

public partial class PlayfieldMutation : Resource
{
    public readonly BaseMutation Mutation;

    public PlayfieldMutation(BaseMutation mutation)
    {
        Mutation = mutation;
    }
}