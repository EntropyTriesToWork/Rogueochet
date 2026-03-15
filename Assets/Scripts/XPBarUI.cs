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
    private Coroutine _popCoroutine;

    private Queue<EssencePacket> _pendingPackets = new Queue<EssencePacket>();

    struct EssencePacket
    {
        public int NewAccumulated;
        public int Threshold;
        public bool IsLevelUp;
    }

    #endregion

    #region Lifecycle

    void Awake()
    {
        PlayerInventory.OnEssenceGained += HandleEssenceGained;
        PlayerInventory.OnLevelUp += HandleLevelUp;
        GameEvents.OnShopOpened += OnShopOpened;
        GameEvents.OnShopClosed += OnShopClosed;
        GameEvents.OnWaveStarted += HandleWaveStarted;

        gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        PlayerInventory.OnEssenceGained -= HandleEssenceGained;
        PlayerInventory.OnLevelUp -= HandleLevelUp;
        GameEvents.OnShopOpened -= OnShopOpened;
        GameEvents.OnShopClosed -= OnShopClosed;
        GameEvents.OnWaveStarted -= HandleWaveStarted;
    }

    void Update()
    {
        if (!_animating) return;

        _displayedFill = Mathf.Lerp(_displayedFill, _targetFill, LerpSpeed * Time.unscaledDeltaTime);
        if (BarFill != null) BarFill.fillAmount = _displayedFill;

        if (Mathf.Abs(_displayedFill - _targetFill) < 0.005f)
        {
            _displayedFill = _targetFill;
            if (BarFill != null) BarFill.fillAmount = _targetFill;

            if (_pendingPackets.Count > 0)
                ProcessNextPacket();
            else
                _animating = false;
        }
    }

    #endregion

    #region Event Handlers

    void HandleEssenceGained(int amount, int newAccumulated, int threshold)
    {
        _pendingPackets.Enqueue(new EssencePacket
        {
            NewAccumulated = newAccumulated,
            Threshold = threshold,
            IsLevelUp = false,
        });
    }

    void HandleLevelUp(int newLevel)
    {
        _pendingPackets.Enqueue(new EssencePacket
        {
            NewAccumulated = 0,
            Threshold = 1,
            IsLevelUp = true,
        });
    }

    void OnShopOpened()
    {
        gameObject.SetActive(true);
        SyncLabelToCurrentLevel();

        if (!_animating && _pendingPackets.Count > 0)
        {
            _animating = true;
            ProcessNextPacket();
        }
    }

    void OnShopClosed()
    {
        gameObject.SetActive(false);
    }

    void HandleWaveStarted(int _)
    {
        _pendingPackets.Clear();
        _animating = false;
        _displayedFill = 0f;
        _targetFill = 0f;
        if (BarFill != null) BarFill.fillAmount = 0f;
    }

    #endregion

    #region Animation

    void ProcessNextPacket()
    {
        if (_pendingPackets.Count == 0) return;

        var packet = _pendingPackets.Dequeue();

        if (packet.IsLevelUp)
        {
            _displayedFill = 1f;
            _targetFill = 1f;
            if (BarFill != null) BarFill.fillAmount = 1f;

            if (_popCoroutine != null) StopCoroutine(_popCoroutine);
            _popCoroutine = StartCoroutine(PopThenReset());
            SyncLabelToCurrentLevel();
        }
        else
        {
            _targetFill = Mathf.Clamp01((float)packet.NewAccumulated / Mathf.Max(1, packet.Threshold));
        }
    }

    IEnumerator PopThenReset()
    {
        RectTransform rt = BarFill != null ? BarFill.GetComponent<RectTransform>() : GetComponent<RectTransform>();
        if (rt == null) yield break;

        float elapsed = 0f;
        while (elapsed < PopDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / PopDuration;
            float s = t < 0.5f
                ? Mathf.Lerp(1f, PopScale, t / 0.5f)
                : Mathf.Lerp(PopScale, 1f, (t - 0.5f) / 0.5f);
            rt.localScale = Vector3.one * s;
            yield return null;
        }

        rt.localScale = Vector3.one;
        _displayedFill = 0f;
        _targetFill = 0f;
        if (BarFill != null) BarFill.fillAmount = 0f;

        _popCoroutine = null;

        if (_pendingPackets.Count > 0)
            ProcessNextPacket();
        else
            _animating = false;
    }

    void SyncLabelToCurrentLevel()
    {
        if (LevelLabel == null) return;
        var inv = PlayerInventory.Instance;
        LevelLabel.text = inv != null ? $"Level {inv.CurrentLevel}" : "Level 1";
    }

    #endregion
}