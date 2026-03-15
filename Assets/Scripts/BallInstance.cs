using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BallInstance
{
    public string     BallTypeName  = "Standard Ball";
    public GameObject BallPrefab;
    public Sprite     PreviewSprite { get; private set; }

    public List<UpgradeData> DirectUpgrades = new List<UpgradeData>();

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

        if (prefab != null)
        {
            var sr = prefab.GetComponent<SpriteRenderer>()
                  ?? prefab.GetComponentInChildren<SpriteRenderer>();
            PreviewSprite = sr != null ? sr.sprite : null;
        }

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
                case UpgradeEffect.BallDamageFlat:            ComputedDamageBonus      += Mathf.RoundToInt(u.Value); break;
                case UpgradeEffect.BallDamagePercent:         ComputedDamageMultiplier *= 1f + u.Value / 100f;       break;
                case UpgradeEffect.BallSpeedFlat:             ComputedSpeedBonus       += u.Value;                   break;
                case UpgradeEffect.BallSpeedPercent:          ComputedSpeedMultiplier  *= 1f + u.Value / 100f;       break;
                case UpgradeEffect.BallDurabilityFlat:        ComputedDurabilityBonus  += Mathf.RoundToInt(u.Value); break;
                case UpgradeEffect.BallSizeIncrease:          ComputedSizeMultiplier   *= 1f + u.Value / 100f;       break;
                case UpgradeEffect.BallPaddleDeflectionRange: ComputedDeflectionBonus  += u.Value;                   break;
                case UpgradeEffect.BallBounceBack:            HasBounceBack             = true;                      break;
            }
        }
    }

    /// <summary>Apply this instance's computed stats to a freshly spawned Ball component.</summary>
    public void ApplyToBall(Ball ball)
    {
        ball.Damage        = Mathf.Max(1, Mathf.RoundToInt((ball.Damage + ComputedDamageBonus) * ComputedDamageMultiplier));
        ball.InitialSpeed  = (ball.InitialSpeed + ComputedSpeedBonus) * ComputedSpeedMultiplier;
        ball.MaxDurability = Mathf.Max(1, ball.MaxDurability + ComputedDurabilityBonus);

        if (!Mathf.Approximately(ComputedSizeMultiplier, 1f))
            ball.transform.localScale *= ComputedSizeMultiplier;

        ball.PaddleDeflectionRandomRange += ComputedDeflectionBonus;
    }

    /// <summary>Builds a readable stat summary string for tooltip display.</summary>
    public string GetStatSummary()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine(BallTypeName);
        sb.AppendLine($"DMG:  +{ComputedDamageBonus} x{ComputedDamageMultiplier:F2}");
        sb.AppendLine($"SPD:  +{ComputedSpeedBonus:F1} x{ComputedSpeedMultiplier:F2}");
        sb.AppendLine($"DUR:  +{ComputedDurabilityBonus}");
        sb.AppendLine($"SIZE: x{ComputedSizeMultiplier:F2}");
        if (HasBounceBack) sb.AppendLine("Bounce Back");
        if (DirectUpgrades.Count > 0)
        {
            sb.AppendLine("Upgrades:");
            foreach (var u in DirectUpgrades)
                sb.AppendLine($"  {u.UpgradeName}");
        }
        return sb.ToString().TrimEnd();
    }
}
