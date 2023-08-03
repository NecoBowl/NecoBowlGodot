using neco_soft.NecoBowlCore.Input;

namespace neco_soft.NecoBowlGodot.Program;

public class ContextSingleton
{
    public static readonly NecoBowlContext Context = new(new());
}