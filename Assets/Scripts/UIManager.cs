using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    #region Inspector

    [Header("HUD — always visible")]
    public TextMeshProUGUI WaveLabel;
    public TextMeshProUGUI HealthLabel;
    public TextMeshProUGUI EssenceLabel;
    public TextMeshProUGUI TimerLabel;
    public TextMeshProUGUI StatusLabel;

    [Header("Wave Banner")]
    [Tooltip("CanvasGroup on an overlay panel containing WaveBannerLabel.")]
    public CanvasGroup WaveBannerGroup;
    public TextMeshProUGUI WaveBannerLabel;
    [Tooltip("Seconds the banner holds at full opacity before fading.")]
    public float BannerHoldTime = 0.6f;
    [Tooltip("Seconds the banner takes to fade out.")]
    public float BannerFadeTime = 1.4f;

    [Header("Overlay Panels")]
    [Tooltip("Shown during the shop phase. Sits on top of the HUD.")]
    public GameObject ShopPanel;
    public GameObject GameOverPanel;
    public GameObject VictoryPanel;
    public GameObject StartPanel;

    [Header("Shop")]
    public Button StartNextWaveButton;
    [Tooltip("Seconds after wave end before the shop panel appears.")]
    public float ShopOpenDelay = 1f;
    [Tooltip("CanvasGroup on ShopPanel used for the fade-in.")]
    public CanvasGroup ShopPanelGroup;
    public float ShopFadeInTime = 0.3f;

    #endregion

    #region Private State

    private Coroutine _bannerCoroutine;
    private Coroutine _shopCoroutine;

    #endregion

    #region Lifecycle

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (StartNextWaveButton != null)
            StartNextWaveButton.onClick.AddListener(() => GameManager.Instance.OnShopComplete());
    }

    void Start()
    {
        SubscribeToEvents();
        SetOverlay(StartPanel);
    }

    void OnDestroy() => UnsubscribeFromEvents();

    #endregion

    #region Events

    public void SubscribeToEvents()
    {
        GameEvents.OnPlayerHealthChanged += OnHealthChanged;
        GameEvents.OnEssenceChanged += OnEssenceChanged;
        GameEvents.OnWaveStarted += OnWaveStarted;
        GameEvents.OnRoundStarted += OnRoundStarted;
        GameEvents.OnRoundEnded += OnRoundEnded;
        GameEvents.OnShopOpened += OnShopOpened;
        GameEvents.OnShopClosed += OnShopClosed;
        GameEvents.OnGameOver += OnGameOver;
        GameEvents.OnVictory += OnVictory;
        GameEvents.OnRoundTimerTick += OnTimerTick;
        GameEvents.OnBallSpeedRampTriggered += OnSpeedRamp;
    }

    void UnsubscribeFromEvents()
    {
        GameEvents.OnPlayerHealthChanged -= OnHealthChanged;
        GameEvents.OnEssenceChanged -= OnEssenceChanged;
        GameEvents.OnWaveStarted -= OnWaveStarted;
        GameEvents.OnRoundStarted -= OnRoundStarted;
        GameEvents.OnRoundEnded -= OnRoundEnded;
        GameEvents.OnShopOpened -= OnShopOpened;
        GameEvents.OnShopClosed -= OnShopClosed;
        GameEvents.OnGameOver -= OnGameOver;
        GameEvents.OnVictory -= OnVictory;
        GameEvents.OnRoundTimerTick -= OnTimerTick;
        GameEvents.OnBallSpeedRampTriggered -= OnSpeedRamp;
    }

    #endregion

    #region Event Handlers

    void OnHealthChanged(int hp, int maxHP)
    {
        if (HealthLabel != null)
            HealthLabel.text = $"{hp}/{maxHP}";
    }

    void OnEssenceChanged(int essence)
    {
        if (EssenceLabel != null)
            EssenceLabel.text = $"{essence}";
    }

    void OnWaveStarted(int waveNumber)
    {
        SetOverlay(null);

        if (WaveLabel != null)
            WaveLabel.text = $"Wave {waveNumber}/{GameManager.Instance.TotalWaves}";

        if (TimerLabel != null)
        {
            TimerLabel.text = Utils.FormatTimeToMinutes(0f);
            TimerLabel.color = Color.white;
        }

        if (_bannerCoroutine != null) StopCoroutine(_bannerCoroutine);
        _bannerCoroutine = StartCoroutine(ShowWaveBanner(waveNumber));
    }

    void OnRoundStarted()
    {
        SetStatus("Click or Space to launch!");
    }

    void OnRoundEnded()
    {
        // Timer display freezes — BallManager stops ticking OnRoundTimerTick when _ballInPlay = false
        if (EnemyManager.Instance.ActiveEnemyCount > 0)
            SetStatus("Enemies advancing...");
    }

    void OnShopOpened()
    {
        if (_shopCoroutine != null) StopCoroutine(_shopCoroutine);
        _shopCoroutine = StartCoroutine(OpenShopDelayed());
    }

    void OnShopClosed() => SetOverlay(null);

    void OnGameOver() => SetOverlay(GameOverPanel);
    void OnVictory() => SetOverlay(VictoryPanel);

    void OnTimerTick(float elapsed)
    {
        if (TimerLabel != null)
            TimerLabel.text = Utils.FormatTimeToMinutes(elapsed);
    }

    void OnSpeedRamp()
    {
        if (TimerLabel != null)
            TimerLabel.color = Color.red;
        SetStatus("SPEED RAMP!");
    }

    #endregion

    #region Wave Banner

    IEnumerator ShowWaveBanner(int waveNumber)
    {
        if (WaveBannerGroup == null || WaveBannerLabel == null) yield break;

        WaveBannerLabel.text = $"Wave {waveNumber}";
        WaveBannerGroup.alpha = 1f;
        WaveBannerGroup.gameObject.SetActive(true);

        yield return new WaitForSecondsRealtime(BannerHoldTime);

        float elapsed = 0f;
        while (elapsed < BannerFadeTime)
        {
            elapsed += Time.unscaledDeltaTime;
            WaveBannerGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / BannerFadeTime);
            yield return null;
        }

        WaveBannerGroup.alpha = 0f;
        WaveBannerGroup.gameObject.SetActive(false);
        _bannerCoroutine = null;
    }

    #endregion

    #region Shop Animation

    IEnumerator OpenShopDelayed()
    {
        yield return new WaitForSecondsRealtime(ShopOpenDelay);

        SetOverlay(ShopPanel);

        if (ShopPanelGroup != null)
        {
            ShopPanelGroup.alpha = 0f;
            float elapsed = 0f;
            while (elapsed < ShopFadeInTime)
            {
                elapsed += Time.unscaledDeltaTime;
                ShopPanelGroup.alpha = Mathf.Clamp01(elapsed / ShopFadeInTime);
                yield return null;
            }
            ShopPanelGroup.alpha = 1f;
        }
        if (ShopManager.Instance != null)
            ShopManager.Instance.GenerateOfferings();

        _shopCoroutine = null;
    }

    #endregion

    #region Helpers

    public void SetStatus(string message)
    {
        if (StatusLabel != null)
            StatusLabel.text = message;
    }

    void SetOverlay(GameObject panel)
    {
        if (ShopPanel != null) ShopPanel.SetActive(ShopPanel == panel);
        if (GameOverPanel != null) GameOverPanel.SetActive(GameOverPanel == panel);
        if (VictoryPanel != null) VictoryPanel.SetActive(VictoryPanel == panel);
        if (StartPanel != null) StartPanel.SetActive(StartPanel == panel);
    }

    #endregion
}