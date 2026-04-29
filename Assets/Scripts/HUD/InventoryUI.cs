using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Tab-key overlay showing:
///   - Ball slots with per-ball stats and Discard button
///   - Global stats panel (from GameStats)
///   - Run stats panel (from GameStats)
/// </summary>
public class InventoryUI : MonoBehaviour
{
    public static InventoryUI Instance { get; private set; }

    [Header("Root")]
    public GameObject InventoryPanel;

    [Header("Ball Slots")]
    [Tooltip("Parent transform that holds BallSlotEntry instances.")]
    public Transform BallSlotsContainer;
    [Tooltip("Prefab with BallSlotEntry component.")]
    public GameObject BallSlotEntryPrefab;

    [Header("Stats Panels")]
    public TextMeshProUGUI GlobalStatsLabel;
    public TextMeshProUGUI RunStatsLabel;
    public TextMeshProUGUI LevelLabel;

    private bool _isOpen = false;
    private List<BallSlotEntry> _slotEntries = new List<BallSlotEntry>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (InventoryPanel != null) InventoryPanel.SetActive(false);
        GameEvents.OnInventoryChanged += RefreshIfOpen;
        GameEvents.OnLevelUp += OnLevelUp;
    }

    void OnDestroy()
    {
        GameEvents.OnInventoryChanged -= RefreshIfOpen;
        GameEvents.OnLevelUp -= OnLevelUp;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            var state = GameManager.Instance?.State ?? GameState.Idle;
            bool allowedState = state == GameState.RoundActive || state == GameState.Wave || state == GameState.RoundEnd;

            if (!allowedState && !_isOpen) return;
            ToggleInventory();
        }
    }

    public void ToggleInventory()
    {
        if (_isOpen) CloseInventory();
        else OpenInventory();
    }

    public void OpenInventory()
    {
        _isOpen = true;
        if (InventoryPanel != null) InventoryPanel.SetActive(true);
        Time.timeScale = 0f;
        Refresh();
    }

    public void CloseInventory()
    {
        _isOpen = false;
        if (InventoryPanel != null) InventoryPanel.SetActive(false);

        if (PauseManager.Instance != null && !PauseManager.Instance.IsPaused)
            Time.timeScale = 1f;
    }

    void RefreshIfOpen()
    {
        if (_isOpen) Refresh();
    }

    void OnLevelUp(int newLevel)
    {
        if (LevelLabel != null)
            LevelLabel.text = $"Level {newLevel}  |  Ball Slots: {PlayerInventory.Instance.MaxBallSlots}";
        if (_isOpen) Refresh();
    }

    // ── Refresh ────────────────────────────────────────────────────

    public void Refresh()
    {
        RefreshBallSlots();
        RefreshGlobalStats();
        RefreshRunStats();
        RefreshLevelLabel();
    }

    void RefreshLevelLabel()
    {
        if (LevelLabel == null) return;
        var inv = PlayerInventory.Instance;
        LevelLabel.text = $"Level {inv.CurrentLevel}  |  Slots: {inv.UsedBallSlots}/{inv.MaxBallSlots}" +
                          $"  |  XP: {inv.EssenceAccumulated}/{inv.EssenceToNextLevel}";
    }

    void RefreshBallSlots()
    {
        if (BallSlotsContainer == null || BallSlotEntryPrefab == null) return;

        foreach (var entry in _slotEntries)
            if (entry != null) Destroy(entry.gameObject);
        _slotEntries.Clear();

        var inv = PlayerInventory.Instance;

        for (int i = 0; i < inv.BallInstances.Count; i++)
        {
            int slotIndex = i;
            GameObject go = Instantiate(BallSlotEntryPrefab, BallSlotsContainer);
            BallSlotEntry entry = go.GetComponent<BallSlotEntry>();
            if (entry != null)
            {
                entry.Populate(inv.BallInstances[slotIndex], slotIndex, () => OnDiscardBall(slotIndex));
                _slotEntries.Add(entry);
            }
        }

        int emptySlots = inv.MaxBallSlots - inv.UsedBallSlots;
        for (int i = 0; i < emptySlots; i++)
        {
            GameObject go = Instantiate(BallSlotEntryPrefab, BallSlotsContainer);
            BallSlotEntry entry = go.GetComponent<BallSlotEntry>();
            if (entry != null)
            {
                entry.PopulateEmpty();
                _slotEntries.Add(entry);
            }
        }
    }

    void RefreshGlobalStats()
    {
        if (GlobalStatsLabel == null) return;
        var inv = PlayerInventory.Instance;
        var stats = inv.Stats;
        if (stats == null) return;

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("<b>── Global Stats ──</b>");
        sb.AppendLine($"Damage Mult:    x{stats.GlobalDamageMultiplier:F2}");
        sb.AppendLine($"Speed Mult:     x{stats.GlobalSpeedMultiplier:F2}");
        sb.AppendLine($"Durability:     +{stats.GlobalDurabilityBonus}");
        sb.AppendLine($"Essence Gain:   x{stats.EssenceGainMultiplier:F2}");
        sb.AppendLine($"Max HP Bonus:   +{stats.MaxHPBonus}");
        sb.AppendLine($"Paddle Speed:   +{stats.PaddleSpeedBonus:F1}");
        sb.AppendLine($"Paddle Size:    +{stats.PaddleSizeBonus:F2}");
        sb.AppendLine($"Ramp Delay:     +{stats.SpeedRampDelayBonus:F1}s");

        // Optional: show level-up only stats
        if (stats.CriticalChanceBonus > 0)
            sb.AppendLine($"Crit Chance:    +{stats.CriticalChanceBonus * 100:F0}%");
        if (stats.CriticalDamageBonus > 0)
            sb.AppendLine($"Crit Damage:    +{stats.CriticalDamageBonus * 100:F0}%");
        if (stats.BallLifeSteal > 0)
            sb.AppendLine($"Life Steal:     {stats.BallLifeSteal * 100:F0}%");
        if (stats.BallPierceChance > 0)
            sb.AppendLine($"Pierce Chance:  {stats.BallPierceChance * 100:F0}%");
        if (stats.ExtraBounces > 0)
            sb.AppendLine($"Extra Bounces:  +{stats.ExtraBounces}");
        if (stats.EssenceOnHitChance > 0)
            sb.AppendLine($"Essence on Hit: {stats.EssenceOnHitChance * 100:F0}%");

        //if (inv.GlobalUpgrades.Count > 0)
        //{
        //    sb.AppendLine("\n<b>Owned Upgrades:</b>");
        //    foreach (var u in inv.GlobalUpgrades)
        //        sb.AppendLine($"  • {u.UpgradeName}");
        //}
        //GlobalStatsLabel.text = sb.ToString();
    }

    void RefreshRunStats()
    {
        if (RunStatsLabel == null) return;
        var stats = PlayerInventory.Instance.Stats;
        if (stats == null) return;

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("<b>── Run Stats ──</b>");
        sb.AppendLine($"Kills:          {stats.TotalKills}");
        sb.AppendLine($"Damage Dealt:   {stats.TotalDamageDealt}");
        sb.AppendLine($"Bounces:        {stats.TotalBounces}");
        sb.AppendLine($"Balls Launched: {stats.TotalBallsLaunched}");
        // TotalTimeElapsed is stored in GameManager's _stats? We'll keep it similar.
        // If you have a timer, you can read it from GameStats as well.
        // For now, we'll use the value from GameStats if you update it every frame.
        sb.AppendLine($"Time:           {Utils.FormatTimeToMinutes(stats.TotalGameTime)}");

        RunStatsLabel.text = sb.ToString();
    }

    void OnDiscardBall(int slotIndex)
    {
        if (PlayerInventory.Instance.UsedBallSlots <= 1)
        {
            Debug.Log("[InventoryUI] Cannot discard last ball.");
            return;
        }
        PlayerInventory.Instance.DiscardBall(slotIndex);
    }
}