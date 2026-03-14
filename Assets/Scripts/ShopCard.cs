using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopCard : MonoBehaviour
{
    #region Inspector

    [Header("UI References")]
    public TextMeshProUGUI CategoryLabel;
    public Image IconImage;
    public TextMeshProUGUI NameLabel;
    public TextMeshProUGUI DescLabel;
    public TextMeshProUGUI CostLabel;
    public Button BuyButton;
    public Image CardBackground;

    [Header("Style")]
    public Color AffordableColor = Color.white;
    public Color UnaffordableColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    #endregion

    public void Populate(
        string categoryText,
        Sprite icon,
        string upgradeName,
        string description,
        int cost,
        bool canAfford,
        Action onBuy)
    {
        if (CategoryLabel != null) CategoryLabel.text = categoryText;
        if (IconImage != null) { IconImage.sprite = icon; IconImage.gameObject.SetActive(icon != null); }
        if (NameLabel != null) NameLabel.text = upgradeName;
        if (DescLabel != null) DescLabel.text = description;
        if (CostLabel != null) CostLabel.text = $"{cost} Essence";

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