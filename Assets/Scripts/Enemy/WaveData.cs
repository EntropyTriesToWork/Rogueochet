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
        int cols = Grid.GetLength(0); 
        int rows = Grid.GetLength(1);

        float topRowY = GridStart.y;
        float bottomRowY = GridStart.y + (rows - 1) * GridSpacing.y;
        float centerY = (topRowY + bottomRowY) / 2f; //Calculate to get center
        float offsetY = -centerY;

        for (int c = 0; c < cols; c++)
        {
            for (int r = 0; r < rows; r++)
            {
                var spawn = Grid[c, r];
                if (spawn != null && spawn.Prefab != null)
                {
                    float yPos = GridStart.y + r * GridSpacing.y + offsetY; //Adding offset
                    Vector2 pos = new Vector2(GridStart.x - c * -GridSpacing.x, yPos);
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