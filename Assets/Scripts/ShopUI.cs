using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Renders ShopManager.CurrentOfferings as clickable cards.
/// 
/// Required prefab layout for ShopCardEntry:
///   ShopCard
///     ├─ CategoryLabel (TMP)  e.g. "BALL UPGRADE" / "GLOBAL" / "NEW BALL"
///     ├─ IconImage (Image)
///     ├─ NameLabel (TMP)
///     ├─ DescLabel (TMP)
///     ├─ CostLabel (TMP)
///     ├─ TargetLabel (TMP)    e.g. "→ Slot 1: Fire Ball"
///     └─ BuyButton (Button)
/// </summary>
public class ShopUI : MonoBehaviour
{
    [Header("Layout")]
    public Transform CardsContainer;
    public GameObject ShopCardPrefab;

    [Header("Info Labels")]
    public TextMeshProUGUI EssenceLabel;
    public TextMeshProUGUI LevelLabel;

    [Header("Navigation")]
    public Button NextWaveButton;

    private List<ShopCard> _cards = new List<ShopCard>();

    void Start()
    {
        GameEvents.OnShopOpened          += OnShopOpened;
        GameEvents.OnShopClosed          += OnShopClosed;
        GameEvents.OnEssenceChanged      += RefreshEssence;
        GameEvents.OnShopOfferingsChanged += RefreshCards;

        PlayerInventory.OnLevelUp        += (_) => RefreshLevelLabel();

        if (NextWaveButton != null)
            NextWaveButton.onClick.AddListener(() => GameManager.Instance.OnShopComplete());
    }

    void OnDestroy()
    {
        GameEvents.OnShopOpened          -= OnShopOpened;
        GameEvents.OnShopClosed          -= OnShopClosed;
        GameEvents.OnEssenceChanged      -= RefreshEssence;
        GameEvents.OnShopOfferingsChanged -= RefreshCards;
    }

    void OnShopOpened()
    {
        RefreshCards();
        RefreshEssence(GameManager.Instance.Essence);
        RefreshLevelLabel();
    }

    void OnShopClosed() => ClearCards();

    void RefreshCards()
    {
        ClearCards();

        if (ShopManager.Instance == null || CardsContainer == null || ShopCardPrefab == null) return;

        var offerings = ShopManager.Instance.CurrentOfferings;
        var inv       = PlayerInventory.Instance;

        for (int i = 0; i < offerings.Count; i++)
        {
            int offeringIndex = i;   // capture
            ShopOffering offering  = offerings[i];

            GameObject go = Instantiate(ShopCardPrefab, CardsContainer);
            ShopCard card = go.GetComponent<ShopCard>();
            if (card == null) continue;

            // Category badge colour
            string categoryText = offering.Category switch
            {
                UpgradeCategory.BallDirect => "<color=#4FC3F7>BALL UPGRADE</color>",
                UpgradeCategory.Global     => "<color=#A5D6A7>GLOBAL</color>",
                UpgradeCategory.NewBall    => "<color=#FFD54F>✦ NEW BALL</color>",
                _                          => ""
            };

            // Target ball hint
            string targetText = "";
            if (offering.Category == UpgradeCategory.BallDirect && inv != null
                && offering.TargetBallSlot >= 0 && offering.TargetBallSlot < inv.BallInstances.Count)
            {
                targetText = $"→ Slot {offering.TargetBallSlot + 1}: {inv.BallInstances[offering.TargetBallSlot].BallTypeName}";
            }

            bool canAfford = GameManager.Instance.Essence >= offering.Upgrade.Cost;

            card.Populate(
                categoryBadge : categoryText,
                icon          : offering.Upgrade.Icon,
                upgradeName   : offering.Upgrade.UpgradeName,
                description   : offering.Upgrade.Description,
                cost          : offering.Upgrade.Cost,
                targetHint    : targetText,
                canAfford     : canAfford,
                onBuy         : () => ShopManager.Instance.PurchaseOffering(offeringIndex)
            );

            _cards.Add(card);
        }
    }

    void ClearCards()
    {
        foreach (var c in _cards)
            if (c != null) Destroy(c.gameObject);
        _cards.Clear();
    }

    void RefreshEssence(int essence)
    {
        if (EssenceLabel != null)
            EssenceLabel.text = $"Essence: {essence}";
    }

    void RefreshLevelLabel()
    {
        if (LevelLabel == null || PlayerInventory.Instance == null) return;
        var inv = PlayerInventory.Instance;
        LevelLabel.text = $"Level {inv.CurrentLevel}  |  Slots: {inv.UsedBallSlots}/{inv.MaxBallSlots}";
    }
}
