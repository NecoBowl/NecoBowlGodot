using System.Runtime.CompilerServices;

using Godot;

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

    public static void RemoveAndFreeChildren(this Node node)
    {
        while (node.GetChildCount() > 0) {
            var child = node.GetChild(0);
            node.RemoveChild(child);
            child.QueueFree();
        }
    }
}