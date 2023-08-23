using System.Collections.Generic;
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
    
    /// <summary>
    /// Recursively finds each child node and flattens.
    /// </summary>
    /// <returns>All children of the node.</returns>
    /// TODO THis will perform horribly, replace asap
    public static IEnumerable<Node> GetChildrenRecursive(this Node node)
    {
        var list = new List<Node>();
        foreach (var child in node.GetChildren()) {
            list.Add(child); 
            list.AddRange(child.GetChildrenRecursive());
        }

        return list;
    }
}