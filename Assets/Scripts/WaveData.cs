using UnityEngine;

[System.Serializable]
public class EnemySpawnInfo
{
    [Tooltip("The enemy prefab to spawn. Each prefab type is its own enemy variant.")]
    public GameObject Prefab;
    [Tooltip("Base HP before global EnemyStats modifiers are applied.")]
    public int BaseHP = 2;
    public int EssenceReward = 1;
}

[CreateAssetMenu(fileName = "WaveData", menuName = "RoguelikePong/Wave Data")]
public class WaveData : ScriptableObject
{
    [Header("Grid Dimensions")]
    public int Rows = 2;
    public int Columns = 5;

    [Header("Spawn Cap")]
    [Tooltip("Max enemies alive at once. When one dies the next queued enemy spawns. 0 = no cap.")]
    public int MaxActiveEnemies = 0;

    [Header("Enemies")]
    [Tooltip("Filled left-to-right, top-to-bottom. Should equal Rows * Columns.")]
    public EnemySpawnInfo[] Enemies;

    private void OnValidate()
    {
        int total = Rows * Columns;
        if (Enemies == null || Enemies.Length != total)
        {
            EnemySpawnInfo[] resized = new EnemySpawnInfo[total];
            for (int i = 0; i < total; i++)
            {
                resized[i] = (Enemies != null && i < Enemies.Length)
                    ? Enemies[i]
                    : new EnemySpawnInfo();
            }
            Enemies = resized;
        }
    }
}

