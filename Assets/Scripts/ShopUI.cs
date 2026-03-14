using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopUI : MonoBehaviour
{
    [Header("Layout")]
    public Transform CardsContainer;
    public GameObject ShopCardPrefab;
    [Tooltip("Duration of each card's pop-in animation in seconds.")]
    public float ShopCardStaggerDelay = 0.3f;

    [Header("Info Labels")]
    public TextMeshProUGUI EssenceLabel;
    public TextMeshProUGUI LevelLabel;

    [Header("Navigation")]
    public Button NextWaveButton;

    private List<ShopCard> _cards = new List<ShopCard>();
    private Coroutine _staggerCoroutine;

    void Awake()
    {
        GameEvents.OnShopOfferingsChanged += RefreshCards;
        GameEvents.OnShopClosed           += ClearCards;
        GameEvents.OnEssenceChanged       += RefreshEssence;

        PlayerInventory.OnLevelUp += (_) => RefreshLevelLabel();

        if (NextWaveButton != null)
            NextWaveButton.onClick.AddListener(() => GameManager.Instance.OnShopComplete());
    }

    void OnDestroy()
    {
        GameEvents.OnShopOfferingsChanged -= RefreshCards;
        GameEvents.OnShopClosed           -= ClearCards;
        GameEvents.OnEssenceChanged       -= RefreshEssence;
        PlayerInventory.OnLevelUp         -= (_) => RefreshLevelLabel();
    }
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
                UpgradeCategory.NewBall    => "<color=#FFD54F>NEW BALL</color>",
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

        RefreshEssence(GameManager.Instance.Essence);
        RefreshLevelLabel();

        if (_staggerCoroutine != null) StopCoroutine(_staggerCoroutine);
        _staggerCoroutine = StartCoroutine(StaggerCards());
    }

    void ClearCards()
    {
        if (_staggerCoroutine != null) StopCoroutine(_staggerCoroutine);
        foreach (var c in _cards)
            if (c != null) Destroy(c.gameObject);
        _cards.Clear();
    }

    IEnumerator StaggerCards()
    {
        foreach (var card in _cards)
        {
            if (card == null) continue;
            CanvasGroup cg = card.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = 0f;
                card.gameObject.SetActive(true);
                yield return StartCoroutine(PopInCard(cg));
            }
            else
            {
                card.gameObject.SetActive(true);
                yield return new WaitForSecondsRealtime(ShopCardStaggerDelay);
            }
        }
        _staggerCoroutine = null;
    }

    IEnumerator PopInCard(CanvasGroup cg)
    {
        RectTransform rt = cg.GetComponent<RectTransform>();
        float elapsed = 0f;

        while (elapsed < ShopCardStaggerDelay)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / ShopCardStaggerDelay);
            cg.alpha = t;
            if (rt != null)
            {
                float s = t < 0.6f
                    ? Mathf.SmoothStep(0.7f, 1.1f, t / 0.6f)
                    : Mathf.Lerp(1.1f, 1f, (t - 0.6f) / 0.4f);
                rt.localScale = new Vector3(s, s, 1f);
            }
            yield return null;
        }

        cg.alpha = 1f;
        if (rt != null) rt.localScale = Vector3.one;
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
