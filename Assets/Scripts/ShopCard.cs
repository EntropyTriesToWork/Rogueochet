using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class ShopCard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    #region Inspector

    [Header("UI References")]
    public TextMeshProUGUI CategoryLabel;
    public Image           IconImage;
    public TextMeshProUGUI NameLabel;
    public TextMeshProUGUI DescLabel;
    public TextMeshProUGUI CostLabel;
    public Button          BuyButton;
    public Image           CardBackground;

    [Header("Style")]
    public Color AffordableColor   = Color.white;
    public Color UnaffordableColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    public float PopDuration = 0.2f;

    #endregion

    private Action _onHoverEnter;
    private Action _onHoverExit;

    public void Populate(
        string categoryText,
        Sprite icon,
        string upgradeName,
        string description,
        int    cost,
        bool   canAfford,
        Action onBuy,
        Action onHoverEnter = null,
        Action onHoverExit  = null)
    {
        _onHoverEnter = onHoverEnter;
        _onHoverExit  = onHoverExit;

        if (CategoryLabel != null) CategoryLabel.text = categoryText;
        if (IconImage     != null) { IconImage.sprite = icon; IconImage.gameObject.SetActive(icon != null); }
        if (NameLabel     != null) NameLabel.text     = upgradeName;
        if (DescLabel     != null) DescLabel.text     = description;
        if (CostLabel     != null) CostLabel.text     = cost.ToString();

        if (CardBackground != null)
            CardBackground.color = canAfford ? AffordableColor : UnaffordableColor;

        if (BuyButton != null)
        {
            BuyButton.interactable = canAfford;
            BuyButton.onClick.RemoveAllListeners();
            BuyButton.onClick.AddListener(() => onBuy?.Invoke());
        }
    }
    public void PlayPopIn()
    {
        StartCoroutine(PopIn());
    }

    IEnumerator PopIn()
    {
        RectTransform rt      = GetComponent<RectTransform>();
        float         elapsed = 0f;

        while (elapsed < PopDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / PopDuration);
            float s = Mathf.SmoothStep(0.75f, 1f, t);
            if (rt != null) rt.localScale = Vector3.one * s;
            yield return null;
        }

        if (rt != null) rt.localScale = Vector3.one;
    }

    public void OnPointerEnter(PointerEventData _) => _onHoverEnter?.Invoke();
    public void OnPointerExit(PointerEventData  _) => _onHoverExit?.Invoke();
}

