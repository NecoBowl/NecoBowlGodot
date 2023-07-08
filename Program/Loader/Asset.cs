using Godot;
using System;

using neco_soft.NecoBowlCore.Model;

namespace neco_soft.NecoBowlGodot;

public static class Asset
{
    public record Unit(NecoUnitModel UnitModel)
    {
        private static string HomeDirectoryUnits = "res://Assets/Unit";
        
        private string AssetDirectory = $"{HomeDirectoryUnits}/{UnitModel.GetType().Name}";

        public SpriteFrames GetSpriteFrames()
            => GD.Load<SpriteFrames>($"{AssetDirectory}/SpriteFrames.tres");

        public Texture2D GetStaticSprite()
            => GetSpriteFrames().GetFrameTexture("default", 0);
    }
}
