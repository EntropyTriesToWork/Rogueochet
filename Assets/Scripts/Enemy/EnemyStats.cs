using UnityEngine;
public static class EnemyStats
{
    public static int FlatHPBonus = 0;
    public static float HPMultiplier = 1f;
    public static int ComputeMaxHP(int baseHP)
    {
        return Mathf.Max(1, Mathf.RoundToInt((baseHP + FlatHPBonus) * HPMultiplier));
    }
    public static void Reset()
    {
        FlatHPBonus   = 0;
        HPMultiplier  = 1f;
    }
}
