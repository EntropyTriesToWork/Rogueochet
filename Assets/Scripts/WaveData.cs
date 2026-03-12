using UnityEngine;

public enum EnemyTier { Normal, Elite, Boss }

[System.Serializable]
public class EnemySpawnInfo
{
    [Tooltip("The enemy prefab to spawn.")]
    public GameObject Prefab;

    [Tooltip("Relative spawn weight. Higher = appears more often.")]
    public float Weight = 1f;

    [Tooltip("HP multiplier for this enemy type relative to the wave's BaseHP.\n" +
             "Fast/small enemies < 1.0  |  Normal ≈ 1.0  |  Big/tanky > 1.0")]
    public float HPMultiplier = 1f;

    [Tooltip("What tier this enemy is. Drives essence reward ranges.")]
    public EnemyTier Tier = EnemyTier.Normal;
}

[CreateAssetMenu(fileName = "WaveData", menuName = "RoguelikePong/Wave Data")]
public class WaveData : ScriptableObject
{
    [Header("Grid Shape")]
    [Tooltip("Number of rows in the enemy grid. Some cells may be left empty for spacing.")]
    public int Rows = 3;
    [Tooltip("Number of columns in the enemy grid.")]
    public int Columns = 5;
    [Tooltip("0–1 chance that any grid cell is left empty, creating gaps in the formation.")]
    [Range(0f, 0.9f)]
    public float EmptyCellChance = 0f;

    [Header("Spawn Caps")]
    [Tooltip("Maximum enemies alive on screen at once. 0 = no cap.")]
    public int MaxActiveEnemies = 0;

    [Tooltip("Total enemies spawned this round (across all rounds in the wave). " +
             "Defaults to Rows × Columns when 0.")]
    public int MaxSpawnedEnemies = 5;

    [Header("Health")]
    [Tooltip("Base HP all enemies in this wave start from before per-entry and global modifiers.")]
    public int BaseHP = 3;

    [Tooltip("Wave-level HP multiplier stacked on top of BaseHP and each entry's HPMultiplier. " +
             "Increase this each wave to scale difficulty.")]
    public float WaveHPMultiplier = 1f;

    [Header("Enemy Pool")]
    [Tooltip("Weighted list of enemy types that can appear in this wave. " +
             "At least one entry with a valid Prefab is required.")]
    public EnemySpawnInfo[] EnemyPool;

    public int ResolvedSpawnCount => MaxSpawnedEnemies > 0 ? MaxSpawnedEnemies : Rows * Columns;

    public EnemySpawnInfo PickRandom()
    {
        if (EnemyPool == null || EnemyPool.Length == 0) return null;

        float totalWeight = 0f;
        foreach (var e in EnemyPool)
            if (e.Prefab != null) totalWeight += Mathf.Max(0f, e.Weight);

        if (totalWeight <= 0f) return null;

        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;
        foreach (var e in EnemyPool)
        {
            if (e.Prefab == null) continue;
            cumulative += Mathf.Max(0f, e.Weight);
            if (roll <= cumulative) return e;
        }

        // Fallback: return last valid entry
        for (int i = EnemyPool.Length - 1; i >= 0; i--)
            if (EnemyPool[i].Prefab != null) return EnemyPool[i];

        return null;
    }

    public int ComputeHP(EnemySpawnInfo entry)
    {
        float raw = BaseHP * entry.HPMultiplier * WaveHPMultiplier;
        return Mathf.Max(1, Mathf.RoundToInt(raw));
    }
    public static int RollEssenceReward(EnemyTier tier)
    {
        return tier switch
        {
            EnemyTier.Normal => Random.Range(3, 6),       // 3 inclusive, 6 exclusive → 3-5
            EnemyTier.Elite => Random.Range(10, 16),     // 10-15
            EnemyTier.Boss => Random.Range(90, 101),    // 90-100
            _ => Random.Range(3, 6),
        };
    }
}