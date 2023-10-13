using System.Net.Mime;

using Godot;
using NecoBowl.Core.Model;
using NecoBowl.Core.Tactics;
using NLog;

namespace neco_soft.NecoBowlGodot.Program.Ui.CardInformationPanel;

public partial class CardInformationPanel : Control
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    RichTextLabel LabelCardName => GetNode<RichTextLabel>("%LabelCardName");
    CardInformationPanel_NodeCardStatus PanelCardInformation => GetNode<CardInformationPanel_NodeCardStatus>($"%{nameof(PanelCardInformation)}");
    RichTextLabel LabelCost => GetNode<RichTextLabel>($"%{nameof(LabelCost)}");
    RichTextLabel LabelStats => GetNode<RichTextLabel>($"%{nameof(LabelStats)}");
    TextureRect TextureRectIcon => GetNode<TextureRect>($"%{nameof(TextureRectIcon)}");
    RichTextLabel LabelBehavior => GetNode<RichTextLabel>($"%{nameof(LabelBehavior)}");
    ItemList ItemListTags => GetNode<ItemList>($"%{nameof(ItemListTags)}");
    TabContainer UnitPlacementInfoTabs => GetNode<TabContainer>($"%{nameof(UnitPlacementInfoTabs)}");

    public override void _Ready()
    {
        UpdateToNoSelection();
    }

    public void UpdateToNoSelection()
    {
        UnitPlacementInfoTabs.CurrentTab = 1;
    }
    
    public void UpdateFromCard(Card card, CardInformationPanel_NodeCardStatus.Variant cardSource)
    {
        
        LabelCardName.Text = card.Name;
        if (card.IsUnitCard(out var unitCard)) {
            UpdateFromUnitCard(unitCard!);
        }

        PanelCardInformation.VariantKind = cardSource;

        UnitPlacementInfoTabs.CurrentTab = 0;
    }

    private void UpdateFromUnitCard(UnitCard unitCard)
    {
        void UpdateItemList()
        {
            while (ItemListTags.ItemCount > 0) {
                ItemListTags.RemoveItem(0);
            }
            foreach (var tag in unitCard.UnitModel.Tags) {
                ItemListTags.AddItem(tag.ToString(), null);
            }
        }
        
        LabelCost.Text = unitCard.Cost.ToString();
        LabelStats.Text = LabelStats_GenerateText(unitCard!.UnitModel);
        TextureRectIcon.Texture = Loader.Asset.Card.From(unitCard).Icon;
        LabelBehavior.Text = unitCard.UnitModel.BehaviorDescription;
        UpdateItemList();
    }

    private static string LabelStats_GenerateText(UnitModel model)
        => $"[color=red]{model.Power}[/color] / [color=aqua]{model.Health}[/color]";
}