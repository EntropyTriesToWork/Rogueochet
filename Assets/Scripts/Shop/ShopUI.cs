using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class ShopUI : MonoBehaviour
{
    #region Inspector

    [Header("Cards")]
    public Transform  CardsContainer;
    public GameObject ShopCardPrefab;
    public float      ShopCardStaggerDelay = 0.3f;

    [Header("Ball Row")]
    [Tooltip("Parent for ball slot icons. Use a Horizontal Layout Group.")]
    public Transform       BallRowContainer;
    [Tooltip("Prefab with an Image component for the ball sprite.")]
    public GameObject      BallSlotIconPrefab;
    [Tooltip("Tint applied to a ball icon when highlighted by a hovered card.")]
    public Color           BallHighlightColor = new Color(1f, 0.85f, 0.2f, 1f);
    [Tooltip("Tooltip panel shown when hovering a ball icon.")]
    public GameObject      TooltipPanel;
    public TextMeshProUGUI TooltipLabel;

    [Header("Labels")]
    public TextMeshProUGUI EssenceLabel;
    [Tooltip("Shows slot count only. Level display is owned by XPBarUI.")]
    public TextMeshProUGUI SlotsLabel;

    [Header("Buttons")]
    public Button          NextWaveButton;
    public Button          RerollButton;
    public TextMeshProUGUI RerollCostLabel;

    #endregion

    #region Private State

    private List<ShopCard>   _cards          = new List<ShopCard>();
    private List<GameObject> _ballIcons      = new List<GameObject>();
    private List<Image>      _ballImages     = new List<Image>();
    private List<Color>      _ballBaseColors = new List<Color>();
    private Coroutine        _staggerCoroutine;

    #endregion

    #region Lifecycle

    void Awake()
    {
        GameEvents.OnShopOfferingsChanged += RefreshCards;
        GameEvents.OnShopClosed           += OnShopClosed;
        GameEvents.OnEssenceChanged       += RefreshEssence;

        PlayerInventory.OnInventoryChanged += RefreshBallRow;

        if (NextWaveButton != null)
            NextWaveButton.onClick.AddListener(() => GameManager.Instance.OnShopComplete());

        if (RerollButton != null)
            RerollButton.onClick.AddListener(OnRerollPressed);

        if (TooltipPanel != null)
            TooltipPanel.SetActive(false);
    }

    void OnDestroy()
    {
        GameEvents.OnShopOfferingsChanged -= RefreshCards;
        GameEvents.OnShopClosed           -= OnShopClosed;
        GameEvents.OnEssenceChanged       -= RefreshEssence;
        PlayerInventory.OnInventoryChanged -= RefreshBallRow;
    }

    void OnShopClosed()
    {
        ClearCards();
        HideTooltip();
        ClearBallHighlights();
    }

    #endregion

    #region Card Rendering

    void RefreshCards()
    {
        ClearCards();

        if (ShopManager.Instance == null || CardsContainer == null || ShopCardPrefab == null) return;

        var offerings = ShopManager.Instance.CurrentOfferings;

        for (int i = 0; i < offerings.Count; i++)
        {
            int          offeringIndex = i;
            ShopOffering offering      = offerings[i];

            GameObject go   = Instantiate(ShopCardPrefab, CardsContainer);
            ShopCard   card = go.GetComponent<ShopCard>();
            if (card == null) continue;

            // Hide via CanvasGroup so the Layout Group still measures the card correctly.
            // SetActive(false) would cause cards to shift as they activate one-by-one.
            CanvasGroup cg = go.GetComponent<CanvasGroup>();
            if (cg == null) cg = go.AddComponent<CanvasGroup>();
            cg.alpha          = 0f;
            cg.blocksRaycasts = false;
            cg.interactable   = false;

            string categoryText = offering.Category switch
            {
                UpgradeCategory.BallDirect => "Ball Upgrade",
                UpgradeCategory.Global     => "Global",
                UpgradeCategory.NewBall    => "New Ball",
                _                          => ""
            };

            bool canAfford = GameManager.Instance.Essence >= offering.Upgrade.Cost;

            UpgradeCategory category   = offering.Category;
            int             targetSlot = offering.TargetBallSlot;

            card.Populate(
                categoryText : categoryText,
                icon         : offering.Upgrade.Icon,
                upgradeName  : offering.Upgrade.UpgradeName,
                description  : offering.Upgrade.Description,
                cost         : offering.Upgrade.Cost,
                canAfford    : canAfford,
                onBuy        : () => ShopManager.Instance.PurchaseOffering(offeringIndex),
                onHoverEnter : () => OnCardHoverEnter(category, targetSlot),
                onHoverExit  : ClearBallHighlights
            );

            _cards.Add(card);
        }

        RefreshEssence(GameManager.Instance.Essence);
        RefreshSlotsLabel();
        RefreshRerollButton();
        RefreshBallRow();

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

    #endregion

    #region Stagger Animation

    IEnumerator StaggerCards()
    {
        foreach (var card in _cards)
        {
            if (card == null) continue;

            CanvasGroup cg = card.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                // Fade in and enable interaction
                float elapsed = 0f;
                while (elapsed < ShopCardStaggerDelay)
                {
                    elapsed += Time.unscaledDeltaTime;
                    cg.alpha = Mathf.Clamp01(elapsed / ShopCardStaggerDelay);
                    yield return null;
                }
                cg.alpha          = 1f;
                cg.blocksRaycasts = true;
                cg.interactable   = true;
            }

            card.PlayPopIn();

            yield return new WaitForSecondsRealtime(ShopCardStaggerDelay);
        }
        _staggerCoroutine = null;
    }

    #endregion

    #region Ball Row

    void RefreshBallRow()
    {
        foreach (var icon in _ballIcons)
            if (icon != null) Destroy(icon);
        _ballIcons.Clear();
        _ballImages.Clear();
        _ballBaseColors.Clear();

        var inv = PlayerInventory.Instance;
        if (inv == null) { Debug.LogWarning("[ShopUI] PlayerInventory not found for ball row."); return; }

        Debug.Log($"[ShopUI] Building ball row: {inv.BallInstances.Count} balls, container={BallRowContainer != null}, prefab={BallSlotIconPrefab != null}");

        for (int i = 0; i < inv.BallInstances.Count; i++)
        {
            int          slotIndex = i;
            BallInstance ball      = inv.BallInstances[i];

            // Create icon — use prefab if assigned, otherwise build a minimal fallback
            GameObject go;
            if (BallSlotIconPrefab != null && BallRowContainer != null)
            {
                go = Instantiate(BallSlotIconPrefab, BallRowContainer);
            }
            else if (BallRowContainer != null)
            {
                go = new GameObject($"BallIcon_{i}", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(BallRowContainer, false);
                var rt = go.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(48f, 48f);
            }
            else
            {
                Debug.LogWarning("[ShopUI] BallRowContainer is not assigned.");
                return;
            }

            Image img = go.GetComponent<Image>();
            if (img != null)
            {
                if (ball.PreviewSprite != null)
                    img.sprite = ball.PreviewSprite;
                else
                    img.color = new Color(0.6f, 0.8f, 1f);   // plain blue placeholder

                _ballImages.Add(img);
                _ballBaseColors.Add(img.color);
            }
            else
            {
                _ballImages.Add(null);
                _ballBaseColors.Add(Color.white);
            }

            EventTrigger trigger = go.GetComponent<EventTrigger>() ?? go.AddComponent<EventTrigger>();
            AddPointerEvent(trigger, EventTriggerType.PointerEnter, (_) => ShowBallTooltip(slotIndex));
            AddPointerEvent(trigger, EventTriggerType.PointerExit,  (_) => HideTooltip());

            _ballIcons.Add(go);
        }
    }

    void AddPointerEvent(EventTrigger trigger, EventTriggerType type, UnityEngine.Events.UnityAction<BaseEventData> action)
    {
        var entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener(action);
        trigger.triggers.Add(entry);
    }

    void ShowBallTooltip(int slotIndex)
    {
        var inv = PlayerInventory.Instance;
        if (inv == null || slotIndex >= inv.BallInstances.Count) return;
        if (TooltipLabel != null) TooltipLabel.text = inv.BallInstances[slotIndex].GetStatSummary();
        if (TooltipPanel != null) TooltipPanel.SetActive(true);
    }

    void HideTooltip()
    {
        if (TooltipPanel != null) TooltipPanel.SetActive(false);
    }

    #endregion

    #region Ball Highlights

    void OnCardHoverEnter(UpgradeCategory category, int targetSlot)
    {
        ClearBallHighlights();

        if (category == UpgradeCategory.Global || category == UpgradeCategory.NewBall)
            HighlightAllBalls();
        else if (category == UpgradeCategory.BallDirect)
            HighlightBall(targetSlot);
    }

    void HighlightBall(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= _ballImages.Count) return;
        if (_ballImages[slotIndex] != null)
            _ballImages[slotIndex].color = BallHighlightColor;
    }

    void HighlightAllBalls()
    {
        for (int i = 0; i < _ballImages.Count; i++)
            if (_ballImages[i] != null)
                _ballImages[i].color = BallHighlightColor;
    }

    void ClearBallHighlights()
    {
        for (int i = 0; i < _ballImages.Count; i++)
            if (_ballImages[i] != null && i < _ballBaseColors.Count)
                _ballImages[i].color = _ballBaseColors[i];
    }

    #endregion

    #region Reroll

    void OnRerollPressed()
    {
        if (ShopManager.Instance != null)
            ShopManager.Instance.Reroll();
    }

    void RefreshRerollButton()
    {
        if (ShopManager.Instance == null) return;
        int  cost      = ShopManager.Instance.RerollCost;
        bool canAfford = GameManager.Instance != null && GameManager.Instance.Essence >= cost;
        if (RerollButton    != null) RerollButton.interactable = canAfford;
        if (RerollCostLabel != null) RerollCostLabel.text      = $"Reroll ({cost})";
    }

    #endregion

    #region Labels

    void RefreshEssence(int essence)
    {
        if (EssenceLabel != null)
            EssenceLabel.text = $"Essence: {essence}";
        RefreshRerollButton();
    }

    void RefreshSlotsLabel()
    {
        if (SlotsLabel == null || PlayerInventory.Instance == null) return;
        var inv = PlayerInventory.Instance;
        SlotsLabel.text = $"Slots: {inv.UsedBallSlots}/{inv.MaxBallSlots}";
    }

    #endregion
}
