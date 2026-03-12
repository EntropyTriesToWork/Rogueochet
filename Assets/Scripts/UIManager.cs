using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("HUD Elements")]
    public TextMeshProUGUI WaveLabel;
    public TextMeshProUGUI HealthLabel;
    public TextMeshProUGUI EssenceLabel;
    public TextMeshProUGUI StatusLabel;
    [Tooltip("Displays elapsed round time in MM:SS format. Goes red when speed ramp triggers.")]
    public TextMeshProUGUI TimerLabel;

    [Header("Screens")]
    public GameObject HUDScreen;
    public GameObject ShopScreen;
    public GameObject GameOverScreen;
    public GameObject VictoryScreen;
    public GameObject StartScreen;

    [Header("Shop")]
    public TextMeshProUGUI ShopEssenceLabel;
    public Button StartNextWaveButton;

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
        ShowStartScreen();
    }

    void OnDestroy()
    {
        UnsubscribeFromEvents();
    }
    public void SubscribeToEvents()
    {
        GameEvents.OnPlayerHealthChanged  += OnHealthChanged;
        GameEvents.OnEssenceChanged       += OnEssenceChanged;
        GameEvents.OnWaveStarted          += OnWaveStarted;
        GameEvents.OnRoundStarted         += OnRoundStarted;
        GameEvents.OnRoundEnded           += OnRoundEnded;
        GameEvents.OnShopOpened           += OnShopOpened;
        GameEvents.OnGameOver             += OnGameOver;
        GameEvents.OnVictory              += OnVictory;
        GameEvents.OnRoundTimerTick       += OnTimerTick;
        GameEvents.OnBallSpeedRampTriggered += OnSpeedRamp;
    }

    void UnsubscribeFromEvents()
    {
        GameEvents.OnPlayerHealthChanged  -= OnHealthChanged;
        GameEvents.OnEssenceChanged       -= OnEssenceChanged;
        GameEvents.OnWaveStarted          -= OnWaveStarted;
        GameEvents.OnRoundStarted         -= OnRoundStarted;
        GameEvents.OnRoundEnded           -= OnRoundEnded;
        GameEvents.OnShopOpened           -= OnShopOpened;
        GameEvents.OnGameOver             -= OnGameOver;
        GameEvents.OnVictory              -= OnVictory;
        GameEvents.OnRoundTimerTick       -= OnTimerTick;
        GameEvents.OnBallSpeedRampTriggered -= OnSpeedRamp;
    }

    // ── Event Handlers ─────────────────────────────────────────────

    void OnHealthChanged(int hp, int maxHP)
    {
        if (HealthLabel != null)
            HealthLabel.text = $"HP: {hp}/{maxHP}";
    }

    void OnEssenceChanged(int essence)
    {
        if (EssenceLabel != null)
            EssenceLabel.text = $"Essence: {essence}";

        if (ShopEssenceLabel != null)
            ShopEssenceLabel.text = $"Essence: {essence}";
    }

    void OnWaveStarted(int waveNumber)
    {
        SetScreen(HUDScreen);
        if (WaveLabel != null)
            WaveLabel.text = $"Wave {waveNumber}/{GameManager.Instance.TotalWaves}";
        SetStatus($"Wave {waveNumber}!");
    }

    void OnRoundStarted()
    {
        SetStatus("Click or Space to launch!");
        if (TimerLabel != null)
        {
            TimerLabel.text  = Utils.FormatTimeToMinutes(0f);
            TimerLabel.color = Color.white;
        }
    }

    void OnRoundEnded()
    {
        if (EnemyManager.Instance.ActiveEnemyCount > 0)
            SetStatus("Enemies advancing...");
    }

    void OnShopOpened()
    {
        SetScreen(ShopScreen);
    }

    void OnGameOver()
    {
        SetScreen(GameOverScreen);
    }

    void OnVictory()
    {
        SetScreen(VictoryScreen);
    }

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
    public void SetStatus(string message)
    {
        if (StatusLabel != null)
            StatusLabel.text = message;
    }

    void SetScreen(GameObject screenToShow)
    {
        if (HUDScreen      != null) HUDScreen.SetActive(screenToShow      == HUDScreen);
        if (ShopScreen     != null) ShopScreen.SetActive(screenToShow     == ShopScreen);
        if (GameOverScreen != null) GameOverScreen.SetActive(screenToShow == GameOverScreen);
        if (VictoryScreen  != null) VictoryScreen.SetActive(screenToShow  == VictoryScreen);
        if (StartScreen    != null) StartScreen.SetActive(screenToShow    == StartScreen);
    }

    void ShowStartScreen() => SetScreen(StartScreen);
}
