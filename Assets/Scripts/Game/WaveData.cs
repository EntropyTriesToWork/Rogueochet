using UnityEngine;

public enum EnemyTier { Normal, Elite, Boss }

[System.Serializable]
public class EnemySpawnInfo
{
    [Tooltip("The enemy prefab to spawn.")]
    public GameObject Prefab;

    [Tooltip("Relative spawn weight. Higher = appears more often.")]
    public float Weight = 1f;

    [Tooltip("HP multiplier relative to the wave's BaseHP. Fast/small < 1.0 | Normal = 1.0 | Tanky > 1.0")]
    public float HPMultiplier = 1f;

    [Tooltip("Drives essence reward range on death.")]
    public EnemyTier Tier = EnemyTier.Normal;
}

[CreateAssetMenu(fileName = "WaveData", menuName = "RoguelikePong/Wave Data")]
public class WaveData : ScriptableObject
{
    [Header("Grid Shape")]
    [Tooltip("Number of rows in the enemy grid.")]
    public int Rows = 3;
    [Tooltip("Number of columns in the enemy grid.")]
    public int Columns = 5;
    [Tooltip("Chance (0-1) that a grid cell is left empty, creating gaps in the formation.")]
    [Range(0f, 0.9f)]
    public float EmptyCellChance = 0f;

    [Header("Spawn Settings")]
    [Tooltip("Total enemies in this wave. They trickle in as active enemies die. Defaults to Rows x Columns if 0.")]
    public int TotalEnemies = 0;
    [Tooltip("Max enemies alive on screen at once. 0 = no cap.")]
    public int MaxActiveEnemies = 0;

    [Header("Health")]
    [Tooltip("Base HP before per-entry and wave multipliers are applied.")]
    public int BaseHP = 3;
    [Tooltip("Wave-level HP multiplier. Increase each wave to scale difficulty.")]
    public float WaveHPMultiplier = 1f;

    [Header("Arena")]
    [Tooltip("Scale of the top and bottom walls. 1 = walls flush with screen edge (full play area). " +
             "Values below 1 push walls inward, shrinking the vertical play area.")]
    [Range(0.1f, 1f)]
    public float WallScale = 1f;

    [Header("Enemy Pool")]
    [Tooltip("Weighted list of enemy types. At least one entry with a valid Prefab is required.")]
    public EnemySpawnInfo[] EnemyPool;

    /// <summary>Total enemies to spawn this wave. Falls back to Rows x Columns if TotalEnemies is 0.</summary>
    public int ResolvedTotalEnemies => TotalEnemies > 0 ? TotalEnemies : Rows * Columns;

    /// <summary>Picks a random entry from EnemyPool using weighted selection.</summary>
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

        for (int i = EnemyPool.Length - 1; i >= 0; i--)
            if (EnemyPool[i].Prefab != null) return EnemyPool[i];

        return null;
    }

    /// <summary>Computes final HP for an entry: BaseHP x entry.HPMultiplier x WaveHPMultiplier.</summary>
    public int ComputeHP(EnemySpawnInfo entry)
    {
        float raw = BaseHP * entry.HPMultiplier * WaveHPMultiplier;
        return Mathf.Max(1, Mathf.RoundToInt(raw));
    }

    /// <summary>Rolls essence reward based on tier. Normal 3-5, Elite 10-15, Boss 90-100.</summary>
    public static int RollEssenceReward(EnemyTier tier)
    {
        return tier switch
        {
            EnemyTier.Normal => Random.Range(3, 6),
            EnemyTier.Elite  => Random.Range(10, 16),
            EnemyTier.Boss   => Random.Range(90, 101),
            _                => Random.Range(3, 6),
        };
    }
}
