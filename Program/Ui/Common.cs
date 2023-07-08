using Godot;

namespace neco_soft.NecoBowlGodot.Program.Ui;

public static class GodotExt
{
    public static void RemoveAndFreeChildren(this Node node)
    {
        while (node.GetChildCount() > 0) {
            var child = node.GetChild(0);
            node.RemoveChild(child);
            child.QueueFree();
        }
    }
}