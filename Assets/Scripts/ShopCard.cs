using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// A single purchasable card in the shop.
/// Attach to the ShopCard prefab root.
/// </summary>
public class ShopCard : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI CategoryLabel;
    public Image           IconImage;
    public TextMeshProUGUI NameLabel;
    public TextMeshProUGUI DescLabel;
    public TextMeshProUGUI CostLabel;
    public TextMeshProUGUI TargetLabel;
    public Button          BuyButton;
    public Image           CardBackground;   // tinted when can't afford

    [Header("Style")]
    public Color AffordableColor   = Color.white;
    public Color UnaffordableColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    public void Populate(
        string categoryBadge,
        Sprite icon,
        string upgradeName,
        string description,
        int    cost,
        string targetHint,
        bool   canAfford,
        Action onBuy)
    {
        if (CategoryLabel != null) CategoryLabel.text = categoryBadge;
        if (IconImage     != null) { IconImage.sprite = icon; IconImage.gameObject.SetActive(icon != null); }
        if (NameLabel     != null) NameLabel.text     = upgradeName;
        if (DescLabel     != null) DescLabel.text     = description;
        if (CostLabel     != null) CostLabel.text     = $"{cost} Essence";
        if (TargetLabel   != null) { TargetLabel.text = targetHint; TargetLabel.gameObject.SetActive(!string.IsNullOrEmpty(targetHint)); }

        if (CardBackground != null)
            CardBackground.color = canAfford ? AffordableColor : UnaffordableColor;

        if (BuyButton != null)
        {
            BuyButton.interactable = canAfford;
            BuyButton.onClick.RemoveAllListeners();
            BuyButton.onClick.AddListener(() => onBuy?.Invoke());
        }
    }
}
