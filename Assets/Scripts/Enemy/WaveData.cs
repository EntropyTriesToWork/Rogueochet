using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WaveData", menuName = "RoguelikePong/Wave Data")]
public class WaveData : SerializedScriptableObject
{
    [BoxGroup("Spawn Timing")] [Tooltip("Seconds before the enemy spawns.")] public float SpawnDelay = 0.5f;

    [BoxGroup("Grid Layout")] public Vector2 GridStart = new Vector2(5f, 0f);
    [BoxGroup("Grid Layout")] public Vector2 GridSpacing = new Vector2(1.4f, 1.2f);
    [BoxGroup("Grid Layout")] public float WallSize = 1f;
    [BoxGroup("Grid Layout")][Button] public void ResetGrid() { Grid = new EnemySpawnInfo[3, 7]; }

    [Tooltip("Define enemy placement on a grid. Use +/- buttons to add rows/columns.")]
    [TableMatrix(HorizontalTitle = "Enemy Grid", SquareCells = true)]
    public EnemySpawnInfo[,] Grid = new EnemySpawnInfo[3, 7];

    public List<(Vector2 position, EnemySpawnInfo enemy)> GetAllSpawnEntries()
    {
        var list = new List<(Vector2, EnemySpawnInfo)>();
        for (int row = 0; row < Grid.GetLength(0); row++)
        {
            for (int col = 0; col < Grid.GetLength(1); col++)
            {
                var spawn = Grid[row, col];
                if (spawn != null && spawn.Prefab != null)
                {
                    Vector2 pos = new Vector2(
                        GridStart.x - col * GridSpacing.x,
                        GridStart.y + row * GridSpacing.y
                    );
                    list.Add((pos, spawn));
                }
            }
        }
        return list;
    }

    public int ResolvedTotalEnemies => GetAllSpawnEntries().Count;

    public static int RollEssenceReward(EnemyTier tier)
    {
        return tier switch
        {
            EnemyTier.Minion => Random.Range(1, 3),
            EnemyTier.Normal => Random.Range(3, 6),
            EnemyTier.Elite => Random.Range(8, 13),
            EnemyTier.Boss => Random.Range(80, 101),
            _ => Random.Range(3, 6) // fallback
        };
    }
}