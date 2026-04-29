using UnityEngine;

[CreateAssetMenu(menuName = "RoguelikePong/Game Stats")]
public class GameStats : ScriptableObject
{
    [Header("Run Stats")]
    public int TotalKills = 0;
    public int TotalBallsLaunched = 0;
    public int TotalBounces = 0;
    public int TotalDamageDealt = 0;
    public float TotalGameTime = 0f;
    public int TotalEssenceGained = 0;
    public int TotalEssenceSpent = 0;
    public int TotalHealthLost = 0;
    public int TotalHealthGained = 0;

    [Header("Global Buffs")]
    public float GlobalDamageMultiplier = 1f;
    public float GlobalSpeedMultiplier = 1f;
    public int GlobalDurabilityBonus = 0;
    public float EssenceGainMultiplier = 1f;
    public int MaxHPBonus = 0;
    public float PaddleSpeedBonus = 0f;
    public float PaddleSizeBonus = 0f;
    public float SpeedRampDelayBonus = 0f;
    public float CriticalChanceBonus = 0f;
    public float CriticalDamageBonus = 0f;
    public float BallLifeSteal = 0f;
    public float BallPierceChance = 0f;
    public int ExtraBounces = 0;
    public float EssenceOnHitChance = 0f;

    [Header("Lifetime Stats")]
    public int LifetimeKills = 0;
    public int LifetimeBallsLaunched = 0;
    public int LifetimeBounces = 0;
    public int LifetimeDamageDealt = 0;
    public float LifetimeGameTime = 0f;
    public int LifetimeEssenceGained = 0;
    public int LifetimeEssenceSpent = 0;
    public int LifetimeHealthLost = 0;
    public int LifetimeHealthGained = 0;
    public int LifetimeWins = 0;
    public int LifetimeLosses = 0;
    public int LifetimeRuns = 0;

    public void ResetRun()
    {
        TotalKills = 0;
        TotalBallsLaunched = 0;
        TotalBounces = 0;
        TotalDamageDealt = 0;
        TotalGameTime = 0f;
        TotalEssenceGained = 0;
        TotalEssenceSpent = 0;
        TotalHealthLost = 0;
        TotalHealthGained = 0;

        GlobalDamageMultiplier = 1f;
        GlobalSpeedMultiplier = 1f;
        GlobalDurabilityBonus = 0;
        EssenceGainMultiplier = 1f;
        MaxHPBonus = 0;
        PaddleSpeedBonus = 0f;
        PaddleSizeBonus = 0f;
        SpeedRampDelayBonus = 0f;
        CriticalChanceBonus = 0f;
        CriticalDamageBonus = 0f;
        BallLifeSteal = 0f;
        BallPierceChance = 0f;
        ExtraBounces = 0;
        EssenceOnHitChance = 0f;
    }
}