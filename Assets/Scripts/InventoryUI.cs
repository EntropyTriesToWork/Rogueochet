using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Tab-key overlay showing:
///   - Ball slots with per-ball stats and Discard button
///   - Global stats panel
///   - Run stats (kills, bounces, balls launched, damage)
/// 
/// Required UI hierarchy (set up in Inspector):
///   InventoryPanel
///     ├─ BallSlotsContainer (vertical layout group, parent for BallSlotEntry prefabs)
///     ├─ GlobalStatsLabel (TMP)
///     └─ RunStatsLabel (TMP)
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
        PlayerInventory.OnInventoryChanged += RefreshIfOpen;
        PlayerInventory.OnLevelUp          += OnLevelUp;
    }

    void OnDestroy()
    {
        PlayerInventory.OnInventoryChanged -= RefreshIfOpen;
        PlayerInventory.OnLevelUp          -= OnLevelUp;
    }

    void Update()
    {
        // Tab toggles inventory; but not during shop, game over, or victory
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            var state = GameManager.Instance?.State ?? GameState.Idle;
            bool allowedState = state == GameState.RoundActive
                             || state == GameState.Wave
                             || state == GameState.RoundEnd;

            if (!allowedState && !_isOpen) return;
            ToggleInventory();
        }
    }

    // ── Open / Close ───────────────────────────────────────────────

    public void ToggleInventory()
    {
        if (_isOpen) CloseInventory();
        else         OpenInventory();
    }

    public void OpenInventory()
    {
        _isOpen = true;
        if (InventoryPanel != null) InventoryPanel.SetActive(true);
        Time.timeScale = 0f;    // pause while browsing inventory
        Refresh();
    }

    public void CloseInventory()
    {
        _isOpen = false;
        if (InventoryPanel != null) InventoryPanel.SetActive(false);

        // Restore time scale — respect pause manager
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

        // Clear old entries
        foreach (var entry in _slotEntries)
            if (entry != null) Destroy(entry.gameObject);
        _slotEntries.Clear();

        var inv = PlayerInventory.Instance;

        // Show occupied slots
        for (int i = 0; i < inv.BallInstances.Count; i++)
        {
            int slotIndex = i;  // capture for lambda
            GameObject go = Instantiate(BallSlotEntryPrefab, BallSlotsContainer);
            BallSlotEntry entry = go.GetComponent<BallSlotEntry>();
            if (entry != null)
            {
                entry.Populate(inv.BallInstances[slotIndex], slotIndex, () => OnDiscardBall(slotIndex));
                _slotEntries.Add(entry);
            }
        }

        // Show empty slots
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

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("<b>── Global Stats ──</b>");
        sb.AppendLine($"Damage Mult:    x{inv.GlobalDamageMultiplier:F2}");
        sb.AppendLine($"Speed Mult:     x{inv.GlobalSpeedMultiplier:F2}");
        sb.AppendLine($"Durability +{inv.GlobalDurabilityBonus}");
        sb.AppendLine($"Essence Gain:   x{inv.EssenceGainMultiplier:F2}");
        sb.AppendLine($"Max HP Bonus:   +{inv.MaxHPBonus}");
        sb.AppendLine($"Paddle Speed +{inv.PaddleSpeedBonus:F1}");
        sb.AppendLine($"Paddle Size  +{inv.PaddleSizeBonus:F2}");
        sb.AppendLine($"Ramp Delay   +{inv.SpeedRampDelayBonus:F1}s");

        if (inv.GlobalUpgrades.Count > 0)
        {
            sb.AppendLine("\n<b>Owned Global Upgrades:</b>");
            foreach (var u in inv.GlobalUpgrades)
                sb.AppendLine($"  • {u.UpgradeName}");
        }

        GlobalStatsLabel.text = sb.ToString();
    }

    void RefreshRunStats()
    {
        if (RunStatsLabel == null) return;
        var inv = PlayerInventory.Instance;

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("<b>── Run Stats ──</b>");
        sb.AppendLine($"Kills:          {inv.TotalKills}");
        sb.AppendLine($"Damage Dealt:   {inv.TotalDamageDealt}");
        sb.AppendLine($"Bounces:        {inv.TotalBounces}");
        sb.AppendLine($"Balls Launched: {inv.TotalBallsLaunched}");
        sb.AppendLine($"Time:           {Utils.FormatTimeToMinutes(inv.TotalTimeElapsed)}");

        RunStatsLabel.text = sb.ToString();
    }

    // ── Discard ────────────────────────────────────────────────────

    void OnDiscardBall(int slotIndex)
    {
        if (PlayerInventory.Instance.UsedBallSlots <= 1)
        {
            Debug.Log("[InventoryUI] Cannot discard last ball.");
            // Optionally show a warning label
            return;
        }
        PlayerInventory.Instance.DiscardBall(slotIndex);
    }
}
