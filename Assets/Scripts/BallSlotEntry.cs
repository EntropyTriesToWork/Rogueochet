using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Single ball slot card in the Inventory UI.
/// Shows ball name, per-ball stats, list of direct upgrades, and a Discard button.
/// 
/// Prefab layout:
///   BallSlotEntry
///     ├─ BallNameLabel (TMP)
///     ├─ StatsLabel (TMP)
///     ├─ UpgradesLabel (TMP)
///     └─ DiscardButton (Button)
///         └─ Text (TMP) "Discard"
/// </summary>
public class BallSlotEntry : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI BallNameLabel;
    public TextMeshProUGUI StatsLabel;
    public TextMeshProUGUI UpgradesLabel;
    public Button          DiscardButton;
    public GameObject      EmptySlotIndicator;   // shown for empty slots

    public void Populate(BallInstance instance, int slotIndex, Action onDiscard)
    {
        if (EmptySlotIndicator != null) EmptySlotIndicator.SetActive(false);

        if (BallNameLabel != null)
            BallNameLabel.text = $"Slot {slotIndex + 1}: {instance.BallTypeName}";

        if (StatsLabel != null)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"DMG:  x{instance.ComputedDamageMultiplier:F2} +{instance.ComputedDamageBonus}");
            sb.AppendLine($"SPD:  x{instance.ComputedSpeedMultiplier:F2} +{instance.ComputedSpeedBonus:F1}");
            sb.AppendLine($"DUR:  +{instance.ComputedDurabilityBonus}");
            sb.AppendLine($"SIZE: x{instance.ComputedSizeMultiplier:F2}");
            if (instance.HasBounceBack) sb.AppendLine("BOUNCE BACK ✓");
            StatsLabel.text = sb.ToString();
        }

        if (UpgradesLabel != null)
        {
            if (instance.DirectUpgrades.Count == 0)
            {
                UpgradesLabel.text = "<i>No upgrades</i>";
            }
            else
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("<b>Upgrades:</b>");
                foreach (var u in instance.DirectUpgrades)
                    sb.AppendLine($"  • {u.UpgradeName}");
                UpgradesLabel.text = sb.ToString();
            }
        }

        if (DiscardButton != null)
        {
            DiscardButton.gameObject.SetActive(true);
            DiscardButton.onClick.RemoveAllListeners();
            DiscardButton.onClick.AddListener(() => onDiscard?.Invoke());
        }
    }

    public void PopulateEmpty()
    {
        if (BallNameLabel    != null) BallNameLabel.text    = "[ Empty Slot ]";
        if (StatsLabel       != null) StatsLabel.text       = "";
        if (UpgradesLabel    != null) UpgradesLabel.text    = "";
        if (DiscardButton    != null) DiscardButton.gameObject.SetActive(false);
        if (EmptySlotIndicator != null) EmptySlotIndicator.SetActive(true);
    }
}
