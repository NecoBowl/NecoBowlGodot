using System.Runtime.CompilerServices;

using Godot;

using neco_soft.NecoBowlCore;

using NLog;

namespace neco_soft.NecoBowlGodot.Program.Ui;

internal static class Common
{
    internal static string GetSceneFile([CallerFilePath] string path = null!)
        => $"res://{path.Split("NecoBowlGodot\\")[1].Replace("\\", "/").Replace(".cs", ".tscn")}";
    
}

public static class GodotExt
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    internal static Vector2 ToGodotVector2(this AbsoluteDirection direction)
        => new Vector2(direction.ToVector2i().X, -direction.ToVector2i().Y);
    
    public static void RemoveAndFreeChildren(this Node node)
    {
        while (node.GetChildCount() > 0) {
            var child = node.GetChild(0);
            node.RemoveChild(child);
            child.QueueFree();
        }
    }
}