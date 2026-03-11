using UnityEngine;

public enum EnemyShape { Square, Triangle, Circle }

[System.Serializable]
public class EnemySpawnInfo
{
    public EnemyShape Shape = EnemyShape.Square;
    public int HP = 2;
    public int EssenceReward = 1;
}

[CreateAssetMenu(fileName = "WaveData", menuName = "RoguelikePong/Wave Data")]
public class WaveData : ScriptableObject
{
    [Header("Grid Dimensions")]
    public int Rows = 2;
    public int Columns = 5;

    [Header("Spawn Cap")]
    [Tooltip("Maximum number of enemies alive at once. When one dies, the next queued enemy spawns. Set to 0 to spawn all at once.")]
    public int MaxActiveEnemies = 0;

    [Header("Enemies")]
    [Tooltip("Fill left-to-right, top-to-bottom. Should equal Rows * Columns.")]
    public EnemySpawnInfo[] Enemies;

    private void OnValidate()
    {
        int total = Rows * Columns;
        if (Enemies == null || Enemies.Length != total)
        {
            EnemySpawnInfo[] newEnemies = new EnemySpawnInfo[total];
            for (int i = 0; i < total; i++)
            {
                newEnemies[i] = (Enemies != null && i < Enemies.Length)
                    ? Enemies[i]
                    : new EnemySpawnInfo();
            }
            Enemies = newEnemies;
        }
    }
}
