using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class PlayerInventory : MonoBehaviour
{
    public static PlayerInventory Instance { get; private set; }

    [Header("Leveling")]
    public int EssencePerLevel = 10;
    public int StartingBallSlots = 3;
    public int MaxPossibleBallSlots = 5;

    [Header("Starting Ball")]
    public GameObject DefaultBallPrefab;
    public string DefaultBallName = "Standard Ball";

    [Header("Reference to run stats")]
    [SerializeField] private GameStats _stats;   // All buffs are stored here

    // Ball slots
    public int MaxBallSlots { get; private set; }
    public int UsedBallSlots => _ballInstances.Count;
    public IReadOnlyList<BallInstance> BallInstances => _ballInstances;
    private List<BallInstance> _ballInstances = new List<BallInstance>();

    // Leveling (run‑local)
    public int CurrentLevel { get; private set; } = 1;
    public int EssenceAccumulated { get; private set; } = 0;
    public int EssenceToNextLevel => CurrentLevel * EssencePerLevel;

    public GameStats Stats => _stats;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        MaxBallSlots = Mathf.Min(StartingBallSlots, MaxPossibleBallSlots);
        AddDefaultBall();
        SubscribeToEvents();
    }

    void OnDestroy() => UnsubscribeFromEvents();

    void SubscribeToEvents()
    {
        GameEvents.OnEnemyDied += HandleEnemyDied;
        GameEvents.OnEnemyDamaged += HandleEnemyDamaged;
        GameEvents.OnBallLaunched += HandleBallLaunched;
    }

    void UnsubscribeFromEvents()
    {
        GameEvents.OnEnemyDied -= HandleEnemyDied;
        GameEvents.OnEnemyDamaged -= HandleEnemyDamaged;
        GameEvents.OnBallLaunched -= HandleBallLaunched;
    }

    void HandleEnemyDied(Enemy _, int essenceReward)
    {
        if (_stats != null) _stats.TotalKills++;
        int awarded = Mathf.RoundToInt(essenceReward * _stats.EssenceGainMultiplier);
        AddLevelingEssence(awarded);
    }

    void HandleEnemyDamaged(Enemy enemy, int damage, int __)
    {
        if (_stats != null) _stats.TotalDamageDealt += damage;
        if (_stats != null) _stats.TotalBounces++;

        if (_stats.EssenceOnHitChance > 0 && Random.value < _stats.EssenceOnHitChance)
            AddLevelingEssence(Mathf.RoundToInt(1 * _stats.EssenceGainMultiplier));

        if (_stats.BallLifeSteal > 0 && Random.value < _stats.BallLifeSteal)
            GameManager.Instance?.Heal(Mathf.RoundToInt(_stats.BallLifeSteal));
    }

    void HandleBallLaunched(Ball _)
    {
        if (_stats != null) _stats.TotalBallsLaunched++;
    }

    void AddLevelingEssence(int amount)
    {
        EssenceAccumulated += amount;
        if (_stats != null) _stats.TotalEssenceGained += amount;
    }

    // --- Level‑up logic (called by GameManager) ---
    public bool TryGetLevelUpRewards(out List<UpgradeData> rewards)
    {
        rewards = null;
        if (EssenceAccumulated < EssenceToNextLevel) return false;

        rewards = new List<UpgradeData>();
        for (int i = 0; i < 3; i++)
            rewards.Add(GenerateRandomStatBuff());
        return true;
    }

    public void ApplyLevelUpReward(UpgradeData selectedReward)
    {
        // Consume essence and increase level
        EssenceAccumulated -= EssenceToNextLevel;
        CurrentLevel++;

        // Apply the stat buff to GameStats
        ApplyStatBuffToStats(selectedReward);

        Debug.Log($"[PlayerInventory] Level up to {CurrentLevel}, gained {selectedReward.UpgradeName}");
        GameEvents.LevelUp(CurrentLevel);
    }

    UpgradeData GenerateRandomStatBuff()
    {
        var possibleBuffs = new List<StatBuffOption>
        {
            new("Damage +10%", UpgradeEffect.GlobalDamagePercent, 10f),
            new("Speed +10%", UpgradeEffect.GlobalSpeedPercent, 10f),
            new("Durability +2", UpgradeEffect.GlobalDurabilityFlat, 2f),
            new("Critical Chance +5%", UpgradeEffect.CriticalChancePercent, 5f),
            new("Critical Damage +15%", UpgradeEffect.CriticalDamagePercent, 15f),
            new("Life Steal 3%", UpgradeEffect.BallLifeSteal, 3f),
            new("Pierce Chance +10%", UpgradeEffect.BallPierceChance, 10f),
            new("Extra Bounces +1", UpgradeEffect.ExtraBounces, 1f),
            new("Essence on Hit 5%", UpgradeEffect.EssenceOnHitChance, 5f),
            new("Paddle Speed +0.5", UpgradeEffect.PaddleSpeedFlat, 0.5f),
            new("Paddle Size +0.2", UpgradeEffect.PaddleSizeFlat, 0.2f),
            new("Speed Ramp Delay +1s", UpgradeEffect.SpeedRampDelay, 1f),
            new("Essence Gain +15%", UpgradeEffect.EssenceGainPercent, 15f)
        };

        var selected = possibleBuffs[Random.Range(0, possibleBuffs.Count)];
        UpgradeData upgrade = ScriptableObject.CreateInstance<UpgradeData>();
        upgrade.UpgradeName = selected.Name;
        upgrade.Effect = selected.Effect;
        upgrade.Value = selected.Value;
        upgrade.IsGlobal = true;
        return upgrade;
    }
    void ApplyStatBuffToStats(UpgradeData upgrade)
    {
        if (_stats == null) return;

        switch (upgrade.Effect)
        {
            case UpgradeEffect.GlobalDamagePercent:
                _stats.GlobalDamageMultiplier += upgrade.Value / 100f;
                break;
            case UpgradeEffect.GlobalSpeedPercent:
                _stats.GlobalSpeedMultiplier += upgrade.Value / 100f;
                break;
            case UpgradeEffect.GlobalDurabilityFlat:
                _stats.GlobalDurabilityBonus += Mathf.RoundToInt(upgrade.Value);
                break;
            case UpgradeEffect.EssenceGainPercent:
                _stats.EssenceGainMultiplier += upgrade.Value / 100f;
                break;
            case UpgradeEffect.PlayerMaxHPFlat:
                _stats.MaxHPBonus += Mathf.RoundToInt(upgrade.Value);
                break;
            case UpgradeEffect.PaddleSpeedFlat:
                _stats.PaddleSpeedBonus += upgrade.Value;
                break;
            case UpgradeEffect.PaddleSizeFlat:
                _stats.PaddleSizeBonus += upgrade.Value;
                break;
            case UpgradeEffect.SpeedRampDelay:
                _stats.SpeedRampDelayBonus += upgrade.Value;
                break;
            case UpgradeEffect.CriticalChancePercent:
                _stats.CriticalChanceBonus += upgrade.Value / 100f;
                break;
            case UpgradeEffect.CriticalDamagePercent:
                _stats.CriticalDamageBonus += upgrade.Value / 100f;
                break;
            case UpgradeEffect.BallLifeSteal:
                _stats.BallLifeSteal += upgrade.Value / 100f;
                break;
            case UpgradeEffect.BallPierceChance:
                _stats.BallPierceChance += upgrade.Value / 100f;
                break;
            case UpgradeEffect.ExtraBounces:
                _stats.ExtraBounces += Mathf.RoundToInt(upgrade.Value);
                break;
            case UpgradeEffect.EssenceOnHitChance:
                _stats.EssenceOnHitChance += upgrade.Value / 100f;
                break;
        }
    }
    #region Ball Management
    public bool IncreaseMaxBallSlots(int increaseAmount)
    {
        int newMax = Mathf.Min(MaxBallSlots + increaseAmount, MaxPossibleBallSlots);
        if (newMax > MaxBallSlots)
        {
            MaxBallSlots = newMax;
            GameEvents.InventoryChanged();
            return true;
        }
        return false;
    }

    public bool CanAddBall() => UsedBallSlots < MaxBallSlots;

    public bool AddBall(UpgradeData ballUpgrade)
    {
        if (!CanAddBall()) return false;
        _ballInstances.Add(new BallInstance(ballUpgrade.BallTypeName, ballUpgrade.BallPrefab));
        GameEvents.InventoryChanged();
        return true;
    }

    public bool DiscardBall(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= _ballInstances.Count) return false;
        _ballInstances.RemoveAt(slotIndex);
        GameEvents.InventoryChanged();
        return true;
    }

    public BallInstance GetBallInstanceForLaunch(int slotIndex) =>
        (slotIndex >= 0 && slotIndex < _ballInstances.Count) ? _ballInstances[slotIndex] : null;

    public void ApplyToBall(Ball ball)
    {
        if (_stats == null) return;
        ball.Damage = Mathf.Max(1, Mathf.RoundToInt(ball.Damage * _stats.GlobalDamageMultiplier));
        ball.InitialSpeed *= _stats.GlobalSpeedMultiplier;
        ball.MaxDurability = Mathf.Max(1, ball.MaxDurability + _stats.GlobalDurabilityBonus);
    }
    #endregion
    public void FullReset() //Resets all the important information.
    {
        _stats.ResetRun(); //resets all run time buffs. 
        _ballInstances.Clear();
        CurrentLevel = 1;
        EssenceAccumulated = 0;
        MaxBallSlots = Mathf.Min(StartingBallSlots, MaxPossibleBallSlots);
        AddDefaultBall();
        GameEvents.InventoryChanged();
    }

    void AddDefaultBall()
    {
        _ballInstances.Add(new BallInstance(DefaultBallName, DefaultBallPrefab));
    }
}

public class StatBuffOption
{
    public string Name;
    public UpgradeEffect Effect;
    public float Value;
    public StatBuffOption(string name, UpgradeEffect effect, float value)
    {
        Name = name; Effect = effect; Value = value;
    }
}