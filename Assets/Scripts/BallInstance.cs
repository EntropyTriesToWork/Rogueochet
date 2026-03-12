using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents one ball slot the player owns. Tracks:
///  - which ball prefab/type it uses
///  - all BallDirect upgrades applied to it
///  - computed stats (applied to the Ball component at launch)
/// </summary>
[System.Serializable]
public class BallInstance
{
    public string BallTypeName = "Standard Ball";
    public GameObject BallPrefab;

    // Per-ball upgrades stacked on this slot
    public List<UpgradeData> DirectUpgrades = new List<UpgradeData>();

    // ── Computed Stats (rebuilt whenever upgrades change) ──────────
    public float ComputedDamageMultiplier  { get; private set; } = 1f;
    public int   ComputedDamageBonus       { get; private set; } = 0;
    public float ComputedSpeedMultiplier   { get; private set; } = 1f;
    public float ComputedSpeedBonus        { get; private set; } = 0f;
    public int   ComputedDurabilityBonus   { get; private set; } = 0;
    public float ComputedSizeMultiplier    { get; private set; } = 1f;
    public float ComputedDeflectionBonus   { get; private set; } = 0f;
    public bool  HasBounceBack             { get; private set; } = false;

    public BallInstance(string typeName, GameObject prefab)
    {
        BallTypeName = typeName;
        BallPrefab   = prefab;
        RebuildStats();
    }

    public void AddUpgrade(UpgradeData upgrade)
    {
        DirectUpgrades.Add(upgrade);
        RebuildStats();
    }

    public void RebuildStats()
    {
        ComputedDamageMultiplier = 1f;
        ComputedDamageBonus      = 0;
        ComputedSpeedMultiplier  = 1f;
        ComputedSpeedBonus       = 0f;
        ComputedDurabilityBonus  = 0;
        ComputedSizeMultiplier   = 1f;
        ComputedDeflectionBonus  = 0f;
        HasBounceBack            = false;

        foreach (var u in DirectUpgrades)
        {
            switch (u.Effect)
            {
                case UpgradeEffect.BallDamageFlat:           ComputedDamageBonus      += Mathf.RoundToInt(u.Value); break;
                case UpgradeEffect.BallDamagePercent:        ComputedDamageMultiplier *= 1f + u.Value / 100f;       break;
                case UpgradeEffect.BallSpeedFlat:            ComputedSpeedBonus       += u.Value;                   break;
                case UpgradeEffect.BallSpeedPercent:         ComputedSpeedMultiplier  *= 1f + u.Value / 100f;       break;
                case UpgradeEffect.BallDurabilityFlat:       ComputedDurabilityBonus  += Mathf.RoundToInt(u.Value); break;
                case UpgradeEffect.BallSizeIncrease:         ComputedSizeMultiplier   *= 1f + u.Value / 100f;       break;
                case UpgradeEffect.BallPaddleDeflectionRange:ComputedDeflectionBonus  += u.Value;                   break;
                case UpgradeEffect.BallBounceBack:           HasBounceBack            = true;                       break;
            }
        }
    }

    /// <summary>Apply this instance's computed stats to a freshly spawned Ball component.</summary>
    public void ApplyToBall(Ball ball)
    {
        // Damage
        int baseDamage = ball.Damage;
        ball.Damage = Mathf.Max(1, Mathf.RoundToInt((baseDamage + ComputedDamageBonus) * ComputedDamageMultiplier));

        // Speed
        ball.InitialSpeed = (ball.InitialSpeed + ComputedSpeedBonus) * ComputedSpeedMultiplier;

        // Durability
        ball.MaxDurability = Mathf.Max(1, ball.MaxDurability + ComputedDurabilityBonus);

        // Size
        if (!Mathf.Approximately(ComputedSizeMultiplier, 1f))
            ball.transform.localScale *= ComputedSizeMultiplier;

        // Deflection
        ball.PaddleDeflectionRandomRange += ComputedDeflectionBonus;
    }
}
