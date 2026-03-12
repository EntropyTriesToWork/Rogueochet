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

    void OnDestroy() => UnsubscribeFromEvents();

    void SubscribeToEvents()
    {
        GameEvents.OnBallCountChanged += HandleBallCountChanged;
        GameEvents.OnEnemyDied += HandleEnemyDied;
        GameEvents.OnEnemyReachedPaddle += HandleEnemyReachedPaddle;
        GameEvents.OnWaveStarted += HandleWaveStarted;
    }

    void UnsubscribeFromEvents()
    {
        GameEvents.OnBallCountChanged -= HandleBallCountChanged;
        GameEvents.OnEnemyDied -= HandleEnemyDied;
        GameEvents.OnEnemyReachedPaddle -= HandleEnemyReachedPaddle;
        GameEvents.OnWaveStarted -= HandleWaveStarted;
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
        float mult = PlayerInventory.Instance != null
            ? PlayerInventory.Instance.EssenceGainMultiplier : 1f;
        int awarded = Mathf.Max(1, Mathf.RoundToInt(essenceReward * mult));
        AddEssence(awarded);

        if (AllEnemiesCleared() && State == GameState.RoundActive)
            ChangeState(GameState.RoundEnd);
    }

    void HandleEnemyReachedPaddle(Enemy enemy) => TakeDamage(1);

    void HandleWaveStarted(int waveNumber) { }

    public bool AllEnemiesCleared() => EnemyManager.Instance.AllEnemiesCleared();

    public void SetWaveEnemyCount(int count) { }

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
                    if (CurrentWave >= TotalWaves) ChangeState(GameState.Victory);
                    else ChangeState(GameState.Shop);
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
    public void TakeDamage(int amount)
    {
        int effectiveMax = MaxHealth + (PlayerInventory.Instance?.MaxHPBonus ?? 0);
        PlayerHealth = Mathf.Clamp(PlayerHealth - amount, 0, effectiveMax);
        GameEvents.PlayerHealthChanged(PlayerHealth, effectiveMax);
        Debug.Log($"[GameManager] Player {(amount > 0 ? "took" : "healed")} {Mathf.Abs(amount)}. HP: {PlayerHealth}/{effectiveMax}");

        if (PlayerHealth <= 0)
            ChangeState(GameState.GameOver);
    }

    public void Heal(int amount) => TakeDamage(-amount);

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

        if (PlayerInventory.Instance != null)
            PlayerInventory.Instance.FullReset();

        CurrentWave = 0;
        PlayerHealth = MaxHealth;
        Essence = 0;

        GameEvents.PlayerHealthChanged(PlayerHealth, MaxHealth);
        GameEvents.EssenceChanged(Essence);
    }
}