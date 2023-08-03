using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

using neco_soft.NecoBowlCore.Model;
using neco_soft.NecoBowlCore.Tactics;
using neco_soft.NecoBowlDefinitions;
using neco_soft.NecoBowlDefinitions.Card;

namespace neco_soft.NecoBowlGodot;

public static partial class Asset
{
    public partial class Unit : Resource
    {
        private const string HomeDirectoryUnits = "res://Assets/Unit";
        
        public static IEnumerable<Asset.Unit> All => NecoDefinitions.AllUnitModels.Select(m => new Unit(m));
        public static Asset.Unit FromModel(NecoUnitModel model) => All.Single(m => m.UnitModel == model);

        private string AssetDirectory => $"{HomeDirectoryUnits}/{UnitModel.GetType().Name}";

        public readonly NecoUnitModel UnitModel;
        
        private Unit(NecoUnitModel unitModel)
        {
            UnitModel = unitModel;
        }

        public SpriteFrames GetSpriteFrames()
            => GD.Load<SpriteFrames>($"{AssetDirectory}/SpriteFrames.tres");

        public Texture2D GetStaticSprite()
            => GetSpriteFrames().GetFrameTexture("default", 0);
    }

    public partial class Card : Resource
    {
        private const string HomeDirectory = "res://Assets/Unit";
        
        public static IEnumerable<Asset.Card> All => NecoDefinitions.AllCardModels.Select(m => new Card(m));

        public readonly NecoCardModel CardModel;

        public Texture2D Icon
            => CardModel is NecoUnitCardModel unitCard
                ? Asset.Unit.FromModel(unitCard.Model).GetStaticSprite()
                : new PlaceholderTexture2D();

        public static Card From(NecoCard card)
        {
            return All.Single(c => card.CardModel == c.CardModel);
        }

        public static Card From(NecoCardModel cardModel)
        {
            return All.Single(c => cardModel == c.CardModel);
        }

        private Card(NecoCardModel cardModel)
        {
            CardModel = cardModel;
        }
    }
}
