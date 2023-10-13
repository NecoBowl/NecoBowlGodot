using System.Collections.Generic;
using System.Linq;
using Godot;
using neco_soft.NecoBowlDefinitions;
using NecoBowl.Core.Model;

namespace neco_soft.NecoBowlGodot.Program.Loader;

public static partial class Asset
{
    public partial class Unit : Resource
    {
        private const string HomeDirectoryUnits = "res://Assets/Unit";
        
        public static IEnumerable<Asset.Unit> All => NecoDefinitions.AllUnitModels.Select(m => new Unit(m));
        public static Asset.Unit FromModel(UnitModel model) => All.Single(m => m.UnitModel == model);

        private string AssetDirectory => $"{HomeDirectoryUnits}/{UnitModel.GetType().Name}";

        public readonly UnitModel UnitModel;
        
        private Unit(UnitModel unitModel)
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

        public readonly CardModel CardModel;

        public Texture2D Icon
            => CardModel is UnitCardModel unitCard
                ? Asset.Unit.FromModel(unitCard.Model).GetStaticSprite()
                : new PlaceholderTexture2D();

        public static Card From(NecoBowl.Core.Tactics.Card card)
        {
            return All.Single(c => card.CardModel == c.CardModel);
        }

        public static Card From(CardModel cardModel)
        {
            return All.Single(c => cardModel == c.CardModel);
        }

        private Card(CardModel cardModel)
        {
            CardModel = cardModel;
        }
    }
}
