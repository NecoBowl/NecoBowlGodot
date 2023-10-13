using NecoBowl.Core;

namespace neco_soft.NecoBowlGodot.Program;

public class ContextSingleton
{
    public static readonly NecoBowlContext Context = new(new());
}