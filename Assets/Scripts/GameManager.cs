using UnityEngine;

public enum GameState { Idle, Wave, RoundActive, RoundEnd, Shop, Victory, GameOver }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Player Stats")]
    public int PlayerHealth = 5;
    public int MaxHealth = 5;
    public int Essence = 0;

    [Header("Wave Settings")]
    public int TotalWaves = 5;
    public int CurrentWave { get; private set; } = 0;

    private int _remainingEnemies = 0;

    public GameState State { get; private set; } = GameState.Idle;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        SubscribeToEvents();
        ChangeState(GameState.Idle);
    }

    void OnDestroy()
    {
        UnsubscribeFromEvents();
    }
    void SubscribeToEvents()
    {
        GameEvents.OnBallCountChanged    += HandleBallCountChanged;
        GameEvents.OnEnemyDied           += HandleEnemyDied;
        GameEvents.OnEnemyReachedPaddle  += HandleEnemyReachedPaddle;
        GameEvents.OnWaveStarted         += HandleWaveStarted;
    }

    void UnsubscribeFromEvents()
    {
        GameEvents.OnBallCountChanged    -= HandleBallCountChanged;
        GameEvents.OnEnemyDied           -= HandleEnemyDied;
        GameEvents.OnEnemyReachedPaddle  -= HandleEnemyReachedPaddle;
        GameEvents.OnWaveStarted         -= HandleWaveStarted;
        GameEvents.ClearAllListeners();
    }

    // ── Event Handlers ─────────────────────────────────────────────

    void HandleBallCountChanged(int remaining)
    {
        if (remaining == 0 && State == GameState.RoundActive)
            ChangeState(GameState.RoundEnd);
    }

    void HandleEnemyDied(Enemy enemy, int essenceReward)
    {
        AddEssence(essenceReward);
        _remainingEnemies = Mathf.Max(0, _remainingEnemies - 1);
    }

    void HandleEnemyReachedPaddle(Enemy enemy)
    {
        TakeDamage(1);
    }

    void HandleWaveStarted(int waveNumber)
    {
        // EnemyManager fires this after spawning; it also tells us the count
        // We'll track via EnemyCountSet event — see below
    }
    public void SetWaveEnemyCount(int count)
    {
        _remainingEnemies = count;
    }

    public bool AllEnemiesCleared() => _remainingEnemies <= 0;

    // ── State Machine ──────────────────────────────────────────────

    public void ChangeState(GameState newState)
    {
        State = newState;
        Debug.Log($"[GameManager] State → {newState}");

        switch (newState)
        {
            case GameState.Idle:
                break;

            case GameState.Wave:
                CurrentWave++;
                Debug.Log($"[GameManager] Starting Wave {CurrentWave}/{TotalWaves}");
                GameEvents.WaveStarted(CurrentWave);
                ChangeState(GameState.RoundActive);
                break;

            case GameState.RoundActive:
                GameEvents.RoundStarted();
                break;

            case GameState.RoundEnd:
                GameEvents.RoundEnded();

                if (AllEnemiesCleared())
                {
                    if (CurrentWave >= TotalWaves)
                        ChangeState(GameState.Victory);
                    else
                        ChangeState(GameState.Shop);
                }
                else
                {
                    ChangeState(GameState.RoundActive);
                }
                break;

            case GameState.Shop:
                GameEvents.WaveCleared();
                GameEvents.ShopOpened();
                break;

            case GameState.Victory:
                GameEvents.Victory();
                break;

            case GameState.GameOver:
                GameEvents.GameOver();
                break;
        }
    }

    // ── Player Stats ───────────────────────────────────────────────

    public void TakeDamage(int amount)
    {
        PlayerHealth = Mathf.Max(0, PlayerHealth - amount);
        GameEvents.PlayerHealthChanged(PlayerHealth, MaxHealth);
        Debug.Log($"[GameManager] Player took {amount} damage. HP: {PlayerHealth}/{MaxHealth}");

        if (PlayerHealth <= 0)
            ChangeState(GameState.GameOver);
    }

    public void AddEssence(int amount)
    {
        Essence += amount;
        GameEvents.EssenceChanged(Essence);
    }

    public bool SpendEssence(int amount)
    {
        if (Essence < amount) return false;
        Essence -= amount;
        GameEvents.EssenceChanged(Essence);
        return true;
    }

    // ── UI-Facing Entry Points ─────────────────────────────────────
    public void OnShopComplete()
    {
        if (State == GameState.Shop)
        {
            GameEvents.ShopClosed();
            ChangeState(GameState.Wave);
        }
    }
    public void StartGame()
    {
        if (State == GameState.Idle || State == GameState.GameOver || State == GameState.Victory)
        {
            ResetGame();
            GameEvents.GameStarted();
            ChangeState(GameState.Wave);
        }
    }

    void ResetGame()
    {
        GameEvents.ClearAllListeners();

        SubscribeToEvents();
        UIManager.Instance.SubscribeToEvents();
        BallManager.Instance.SubscribeToEvents();
        EnemyManager.Instance.SubscribeToEvents();

        CurrentWave = 0;
        _remainingEnemies = 0;
        PlayerHealth = MaxHealth;
        Essence = 0;

        GameEvents.PlayerHealthChanged(PlayerHealth, MaxHealth);
        GameEvents.EssenceChanged(Essence);
    }
}
