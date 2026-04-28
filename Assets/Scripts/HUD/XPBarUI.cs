using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class XPBarUI : MonoBehaviour
{
    #region Inspector

    [Header("References")]
    [Tooltip("Image with Fill Method set to Filled.")]
    public Image BarFill;
    public TextMeshProUGUI LevelLabel;
    [Tooltip("CanvasGroup on this GameObject used to show/hide without deactivating it.")]
    public CanvasGroup Group;

    [Header("Animation")]
    [Tooltip("Lerp speed toward the target fill. Higher = snappier.")]
    public float LerpSpeed = 3f;
    [Tooltip("Scale the bar punches to on level-up.")]
    public float PopScale = 1.25f;
    public float PopDuration = 0.3f;

    #endregion

    #region Private State

    private float _displayedFill = 0f;
    private float _targetFill = 0f;
    private bool _animating = false;
    private bool _destroyed = false;
    private Coroutine _popCoroutine;

    private int _levelUpsThisWave = 0;
    private int _essenceAtWaveStart = 0;

    #endregion

    #region Lifecycle

    void Awake()
    {
        PlayerInventory.OnLevelUp += HandleLevelUp;
        GameEvents.OnShopOpened += OnShopOpened;
        GameEvents.OnShopClosed += OnShopClosed;
        GameEvents.OnWaveStarted += HandleWaveStarted;
        SetVisible(false);
    }

    void OnDestroy()
    {
        _destroyed = true;
        StopAllCoroutines();

        PlayerInventory.OnLevelUp -= HandleLevelUp;
        GameEvents.OnShopOpened -= OnShopOpened;
        GameEvents.OnShopClosed -= OnShopClosed;
        GameEvents.OnWaveStarted -= HandleWaveStarted;
    }

    void Update()
    {
        if (_destroyed || !_animating) return;

        _displayedFill = Mathf.Lerp(_displayedFill, _targetFill, LerpSpeed * Time.unscaledDeltaTime);

        if (BarFill != null) BarFill.fillAmount = _displayedFill;

        if (Mathf.Abs(_displayedFill - _targetFill) < 0.005f)
        {
            _displayedFill = _targetFill;
            if (BarFill != null) BarFill.fillAmount = _targetFill;
            _animating = false;
        }
    }

    #endregion

    #region Event Handlers

    void HandleLevelUp(int _) => _levelUpsThisWave++;

    void OnShopOpened()
    {
        SetVisible(true);
        SyncLabelToCurrentLevel();

        var inv = PlayerInventory.Instance;
        if (inv == null) return;

        if (_levelUpsThisWave > 0)
        {
            if (_popCoroutine != null) StopCoroutine(_popCoroutine);
            _popCoroutine = StartCoroutine(AnimateLevelUps(_levelUpsThisWave, inv));
        }
        else
        {
            float target = (float)inv.EssenceAccumulated / Mathf.Max(1, inv.EssenceToNextLevel);
            _targetFill = target;
            _animating = true;
        }
    }

    void OnShopClosed()
    {
        SetVisible(false);
    }

    void HandleWaveStarted(int _)
    {
        _levelUpsThisWave = 0;
        _essenceAtWaveStart = PlayerInventory.Instance != null ? PlayerInventory.Instance.EssenceAccumulated : 0;
        _animating = false;
        _displayedFill = 0f;
        _targetFill = 0f;
        if (BarFill != null) BarFill.fillAmount = 0f;
    }

    #endregion

    #region Animation
    IEnumerator AnimateLevelUps(int count, PlayerInventory inv)
    {
        for (int i = 0; i < count; i++)
        {
            if (_destroyed) yield break;

            _targetFill = 1f;
            _animating = true;
            yield return new WaitUntil(() => !_animating || _destroyed);

            if (_destroyed) yield break;

            if (_popCoroutine != null) StopCoroutine(_popCoroutine);
            yield return StartCoroutine(PopBar());

            if (_destroyed) yield break;

            _displayedFill = 0f;
            _targetFill = 0f;
            if (BarFill != null) BarFill.fillAmount = 0f;

            SyncLabelToCurrentLevel();
        }

        if (_destroyed) yield break;

        float leftover = (float)inv.EssenceAccumulated / Mathf.Max(1, inv.EssenceToNextLevel);
        _targetFill = leftover;
        _animating = true;
        _popCoroutine = null;
    }

    IEnumerator PopBar()
    {
        RectTransform rt = BarFill != null ? BarFill.GetComponent<RectTransform>()
                                           : GetComponent<RectTransform>();
        if (rt == null) yield break;

        float elapsed = 0f;
        while (elapsed < PopDuration)
        {
            if (_destroyed) yield break;
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / PopDuration;
            float s = t < 0.5f
                ? Mathf.Lerp(1f, PopScale, t / 0.5f)
                : Mathf.Lerp(PopScale, 1f, (t - 0.5f) / 0.5f);
            rt.localScale = Vector3.one * s;
            yield return null;
        }

        if (!_destroyed) rt.localScale = Vector3.one;
    }

    void SyncLabelToCurrentLevel()
    {
        if (LevelLabel == null) return;
        var inv = PlayerInventory.Instance;
        LevelLabel.text = inv != null ? $"Level {inv.CurrentLevel}" : "Level 1";
    }

    void SetVisible(bool visible)
    {
        if (Group == null) Group = GetComponent<CanvasGroup>();
        if (Group == null) Group = gameObject.AddComponent<CanvasGroup>();

        Group.alpha = visible ? 1f : 0f;
        Group.blocksRaycasts = visible;
        Group.interactable = visible;
    }

    #endregion
}