using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public static PlayerInventory Instance { get; private set; }

    [Header("Leveling")]
    [Tooltip("How much essence is required per level. Level N costs N * EssencePerLevel.")]
    public int EssencePerLevel = 10;
    [Tooltip("Starting number of ball slots before any level-ups.")]
    public int StartingBallSlots = 1;

    public int MaxBallSlots  { get; private set; }
    public int UsedBallSlots => _ballInstances.Count;
    public IReadOnlyList<BallInstance> BallInstances => _ballInstances;

    private List<BallInstance> _ballInstances = new List<BallInstance>();

    public int CurrentLevel           { get; private set; } = 1;
    public int EssenceAccumulated     { get; private set; } = 0;   // lifetime essence for leveling
    public int EssenceToNextLevel     => CurrentLevel * EssencePerLevel;

    public List<UpgradeData> GlobalUpgrades { get; private set; } = new List<UpgradeData>();

    public float GlobalDamageMultiplier  { get; private set; } = 1f;
    public float GlobalSpeedMultiplier   { get; private set; } = 1f;
    public int   GlobalDurabilityBonus   { get; private set; } = 0;
    public float EssenceGainMultiplier   { get; private set; } = 1f;
    public int   MaxHPBonus              { get; private set; } = 0;
    public float PaddleSpeedBonus        { get; private set; } = 0f;
    public float PaddleSizeBonus         { get; private set; } = 0f;
    public float SpeedRampDelayBonus     { get; private set; } = 0f;

    public int   TotalKills         { get; private set; } = 0;
    public int   TotalBallsLaunched { get; private set; } = 0;
    public int   TotalBounces       { get; private set; } = 0;
    public int   TotalDamageDealt   { get; private set; } = 0;
    public float TotalTimeElapsed   { get; private set; } = 0f;

    public static System.Action OnInventoryChanged;
    public static System.Action<int> OnLevelUp;   // new level

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        MaxBallSlots = StartingBallSlots;
        SubscribeToEvents();
    }

    void OnDestroy() => UnsubscribeFromEvents();

    void SubscribeToEvents()
    {
        GameEvents.OnEnemyDied     += HandleEnemyDied;
        GameEvents.OnEnemyDamaged  += HandleEnemyDamaged;
        GameEvents.OnBallLaunched  += HandleBallLaunched;
        GameEvents.OnRoundTimerTick+= HandleTimerTick;
        GameEvents.OnGameOver      += ResetRunStats;
        GameEvents.OnVictory        += HandleVictory;
    }

    void UnsubscribeFromEvents()
    {
        GameEvents.OnEnemyDied     -= HandleEnemyDied;
        GameEvents.OnEnemyDamaged  -= HandleEnemyDamaged;
        GameEvents.OnBallLaunched  -= HandleBallLaunched;
        GameEvents.OnRoundTimerTick-= HandleTimerTick;
        GameEvents.OnGameOver      -= ResetRunStats;
        GameEvents.OnVictory -= HandleVictory;
    }
    void HandleEnemyDied(Enemy _, int __)
    {
        TotalKills++;
        // Award leveling essence (separate from spendable essence)
        int awarded = Mathf.RoundToInt(1 * EssenceGainMultiplier);
        AddLevelingEssence(awarded);
    }

    void HandleEnemyDamaged(Enemy _, int damage, int __)
    {
        TotalDamageDealt += damage;
        TotalBounces++;          // every damage hit = a bounce off that enemy
    }

    void HandleBallLaunched(Ball _) => TotalBallsLaunched++;

    void HandleTimerTick(float elapsed) => TotalTimeElapsed = elapsed;

    void HandleVictory()
    {
        //In case of stat gains on victory. 
    }

    void AddLevelingEssence(int amount)
    {
        EssenceAccumulated += amount;
        while (EssenceAccumulated >= EssenceToNextLevel)
        {
            EssenceAccumulated -= EssenceToNextLevel;
            LevelUp();
        }
    }

    void LevelUp()
    {
        CurrentLevel++;
        MaxBallSlots++;
        Debug.Log($"[PlayerInventory] Level up! Now level {CurrentLevel}, ball slots: {MaxBallSlots}");
        OnLevelUp?.Invoke(CurrentLevel);
        OnInventoryChanged?.Invoke();
    }

    public bool CanAddBall() => UsedBallSlots < MaxBallSlots;

    public bool AddBall(UpgradeData ballUpgrade)
    {
        if (!CanAddBall())
        {
            Debug.LogWarning("[PlayerInventory] No free ball slots!");
            return false;
        }
        var instance = new BallInstance(ballUpgrade.BallTypeName, ballUpgrade.BallPrefab);
        _ballInstances.Add(instance);
        OnInventoryChanged?.Invoke();
        return true;
    }

    /// <summary>Discard a ball by slot index.</summary>
    public bool DiscardBall(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= _ballInstances.Count) return false;
        _ballInstances.RemoveAt(slotIndex);
        Debug.Log($"[PlayerInventory] Ball slot {slotIndex} discarded.");
        OnInventoryChanged?.Invoke();
        return true;
    }

    public void ApplyDirectUpgrade(UpgradeData upgrade, int ballSlotIndex)
    {
        if (ballSlotIndex < 0 || ballSlotIndex >= _ballInstances.Count) return;
        _ballInstances[ballSlotIndex].AddUpgrade(upgrade);
        OnInventoryChanged?.Invoke();
        Debug.Log($"[PlayerInventory] Applied '{upgrade.UpgradeName}' to ball slot {ballSlotIndex}.");
    }

    public void ApplyGlobalUpgrade(UpgradeData upgrade)
    {
        GlobalUpgrades.Add(upgrade);
        RebuildGlobalStats();
        ApplyGlobalEffectsToSystems();
        OnInventoryChanged?.Invoke();
        Debug.Log($"[PlayerInventory] Applied global upgrade '{upgrade.UpgradeName}'.");
    }

    void RebuildGlobalStats()
    {
        GlobalDamageMultiplier = 1f;
        GlobalSpeedMultiplier  = 1f;
        GlobalDurabilityBonus  = 0;
        EssenceGainMultiplier  = 1f;
        MaxHPBonus             = 0;
        PaddleSpeedBonus       = 0f;
        PaddleSizeBonus        = 0f;
        SpeedRampDelayBonus    = 0f;

        foreach (var u in GlobalUpgrades)
        {
            switch (u.Effect)
            {
                case UpgradeEffect.GlobalDamagePercent:  GlobalDamageMultiplier *= 1f + u.Value / 100f; break;
                case UpgradeEffect.GlobalSpeedPercent:   GlobalSpeedMultiplier  *= 1f + u.Value / 100f; break;
                case UpgradeEffect.GlobalDurabilityFlat: GlobalDurabilityBonus  += Mathf.RoundToInt(u.Value); break;
                case UpgradeEffect.EssenceGainPercent:   EssenceGainMultiplier  *= 1f + u.Value / 100f; break;
                case UpgradeEffect.PlayerMaxHPFlat: MaxHPBonus += Mathf.RoundToInt(u.Value); break;
                case UpgradeEffect.PaddleSpeedFlat: PaddleSpeedBonus += u.Value; break;
                case UpgradeEffect.PaddleSizeFlat: PaddleSizeBonus += u.Value; break;
                case UpgradeEffect.SpeedRampDelay: SpeedRampDelayBonus += u.Value; break;
            }
        }
    }

    void ApplyGlobalEffectsToSystems()
    {
        if (PaddleController.Instance != null)
        {
            // Recalculate paddle stats from base + all global bonuses
            // This is additive over the run — we track bonuses separately
            // In production you'd store a base value; here we apply the last delta
        }
        if (BallManager.Instance != null)
        {
            BallManager.Instance.SpeedRampDelay =
                BallManager.Instance.BaseSpeedRampDelay + SpeedRampDelayBonus;
        }
    }
    public BallInstance GetBallInstanceForLaunch(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= _ballInstances.Count) return null;
        return _ballInstances[slotIndex];
    }
    void ResetRunStats()
    {
        TotalKills         = 0;
        TotalBallsLaunched = 0;
        TotalBounces       = 0;
        TotalDamageDealt   = 0;
        TotalTimeElapsed   = 0f;
    }

    public void FullReset()
    {
        _ballInstances.Clear();
        GlobalUpgrades.Clear();
        RebuildGlobalStats();
        CurrentLevel       = 1;
        EssenceAccumulated = 0;
        MaxBallSlots       = StartingBallSlots;
        ResetRunStats();
        OnInventoryChanged?.Invoke();
    }
}
