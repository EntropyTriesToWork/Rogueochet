using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GameState { Idle, Wave, RoundActive, RoundEnd, Shop, LevelUp, Victory, GameOver }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Player Stats")]
    [SerializeField] int _baseMaxHealth = 20;
    public int PlayerHealth { get; private set; }
    public int MaxHealth { get; private set; }
    public int Essence = 0;

    [Header("Wave Settings")]
    public int TotalWaves = 5;
    public int CurrentWave { get; private set; } = 0;

    [Header("Run Statistics")]
    [SerializeField] private GameStats _stats;

    private bool _waitingForLevelUp = false;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }
    void Start()
    {
        ResetGame();
        SubscribeToEvents();
        ChangeState(GameState.Idle);
    }
    #region Events
    void OnDestroy() => UnsubscribeFromEvents();

    void SubscribeToEvents()
    {
        GameEvents.OnBallCountChanged += HandleBallCountChanged;
        GameEvents.OnEnemyDied += HandleEnemyDied;
        GameEvents.OnEnemyReachedPaddle += HandleEnemyReachedPaddle;
    }
    void UnsubscribeFromEvents()
    {
        GameEvents.OnBallCountChanged -= HandleBallCountChanged;
        GameEvents.OnEnemyDied -= HandleEnemyDied;
        GameEvents.OnEnemyReachedPaddle -= HandleEnemyReachedPaddle;
    }
    void HandleBallCountChanged(int remaining)
    {
        if (remaining == 0 && State == GameState.RoundActive)
            ChangeState(GameState.RoundEnd);
    }

    void HandleEnemyDied(Enemy enemy, int essenceReward)
    {
        int awarded = Mathf.Max(1, Mathf.RoundToInt(essenceReward * _stats.EssenceGainMultiplier));
        AddEssence(awarded);

        if (AllEnemiesCleared() && State == GameState.RoundActive)
            ChangeState(GameState.RoundEnd);
    }
    void HandleEnemyReachedPaddle(Enemy enemy) => TakeDamage(1);

    private IEnumerator HandleLevelUpBeforeShop()
    {
        var inv = PlayerInventory.Instance;
        if (inv == null) yield break;
        Debug.Log("Leveling up before shop!");
        while (inv.TryGetLevelUpRewards(out List<UpgradeData> rewards)) // Keep trying while there are pending level‑ups
        {
            _waitingForLevelUp = true;
            // Show UI and wait for player to choose
            ShopManager.Instance.ShowLevelUpChoices(rewards, () =>
            {
                _waitingForLevelUp = false;
            });
            yield return new WaitUntil(() => !_waitingForLevelUp);
        }
        // After all level‑ups are done, proceed to shop or victory
        if (CurrentWave >= TotalWaves)
            ChangeState(GameState.Victory);
        else
            ChangeState(GameState.Shop);
    }
    #endregion

    #region Public Methods
    public void ChangeState(GameState newState)
    {
        State = newState;
        Debug.Log($"[GameManager] State → {newState}");

        switch (newState)
        {
            case GameState.Idle: break;
            case GameState.Wave:
                CurrentWave++;
                GameEvents.WaveStarted(CurrentWave);
                DelayedAction(1f, () => ChangeState(GameState.RoundActive));
                break;
            case GameState.RoundActive:
                GameEvents.RoundStarted();
                break;
            case GameState.RoundEnd:
                GameEvents.RoundEnded();
                if (AllEnemiesCleared())
                {
                    // Before going to shop, handle any pending level‑ups
                    StartCoroutine(HandleLevelUpBeforeShop());
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
            case GameState.LevelUp:
                // This state is not strictly needed; we use coroutine + UI.
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
        PlayerHealth = Mathf.Clamp(PlayerHealth - amount, 0, MaxHealth);
        GameEvents.PlayerHealthChanged(PlayerHealth, MaxHealth);
        if (PlayerHealth <= 0) ChangeState(GameState.GameOver);
    }
    public void Heal(int amount) => TakeDamage(-amount);
    public void AddEssence(int amount)
    {
        Essence += amount;
        if (_stats != null) _stats.TotalEssenceGained += amount;
        GameEvents.EssenceChanged(Essence);
    }
    public void ChangeMaxHP(int amount)
    {
        _stats.MaxHPBonus += amount;
        MaxHealth += amount;
        if (MaxHealth <= 0) MaxHealth = 1;
        GameEvents.PlayerHealthChanged(PlayerHealth, MaxHealth);
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
    public void ResetGame()
    {
        if (PlayerInventory.Instance != null)
            PlayerInventory.Instance.FullReset();

        // Reset run stats in GameStats asset
        if (_stats != null)
        {
            _stats.TotalKills = 0;
            _stats.TotalBallsLaunched = 0;
            _stats.TotalBounces = 0;
            _stats.TotalDamageDealt = 0;
            _stats.TotalGameTime = 0f;
            _stats.TotalEssenceGained = 0;
            _stats.TotalEssenceSpent = 0;
            _stats.TotalHealthLost = 0;
            _stats.TotalHealthGained = 0;
        }

        EnemyStats.Reset();
        CurrentWave = 0;
        Heal(MaxHealth);
        Essence = 0;
        GameEvents.EssenceChanged(Essence);
    }
    public void DelayedAction(float duration, Action onComplete) => StartCoroutine(DoDelayedAction(duration, onComplete));
    private IEnumerator DoDelayedAction(float duration, Action onComplete)
    {
        yield return new WaitForSeconds(duration);
        onComplete?.Invoke();
    }
    #endregion

    public GameState State { get; private set; } = GameState.Idle;
    public bool AllEnemiesCleared() => EnemyManager.Instance?.AllEnemiesCleared() ?? true;
}