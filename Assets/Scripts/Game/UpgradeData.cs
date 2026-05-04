using UnityEngine;

public enum UpgradeCategory
{
    BallDirect,
    Global,
    NewBall,
}
public enum UpgradeEffect
{
    // ── Ball-Direct ────────────────────────────────────────────────
    BallDamageFlat,
    BallDamagePercent,
    BallSpeedFlat,
    BallSpeedPercent,
    BallDurabilityFlat,
    BallSizeIncrease,
    BallPaddleDeflectionRange,
    BallBounceBack,

    GlobalDamagePercent,
    GlobalSpeedPercent,
    GlobalDurabilityFlat,
    EssenceGainPercent,
    PlayerMaxHPFlat,
    PaddleSpeedFlat,
    PaddleSizeFlat,
    SpeedRampDelay,
    CriticalChancePercent,
    CriticalDamagePercent,
    BallLifeSteal,
    BallPierceChance,
    ExtraBounces,
    EssenceOnHitChance,
    None,
}
[CreateAssetMenu(fileName = "UpgradeData", menuName = "RoguelikePong/Upgrade Data")]
public class UpgradeData : ScriptableObject
{
    [Header("Identity")]
    public string UpgradeName = "Unnamed Upgrade";
    [TextArea(2, 4)]
    public string Description = "";
    public Sprite Icon;

    [Header("Shop")]
    public UpgradeCategory Category = UpgradeCategory.BallDirect;
    public int Cost = 3;

    [Header("Effect")]
    public UpgradeEffect Effect = UpgradeEffect.BallDamageFlat;
    public float Value = 1f;          // meaning depends on Effect
    public bool IsGlobal = true;

    [Header("New Ball Only")]
    [Tooltip("Prefab to use when this upgrade grants a new ball type.")]
    public GameObject BallPrefab;
    [Tooltip("Display name for this ball type.")]
    public string BallTypeName = "Standard Ball";
}
