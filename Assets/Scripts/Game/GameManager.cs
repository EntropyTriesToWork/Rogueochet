using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GameState { Idle, RoomTransition, Combat, Shop, Victory, GameOver }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Player Stats")]
    [SerializeField] int _baseMaxHealth = 20;
    public int PlayerHealth { get; private set; }
    public int MaxHealth { get; private set; }
    public int Essence = 0;

    [Header("Run Settings")]
    public int TotalRooms = 15; // number of rooms in this run
    [SerializeField] private GameStats _stats;

    // Room & wave tracking
    public int CurrentRoomIndex => _currentRoomIndex;
    private int _currentRoomIndex = -1;
    private int _currentWaveInRoom = 0;
    private int _totalWavesInCurrentRoom = 0;
    private bool _roomCompleted = false;

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
        GameEvents.OnEnemyDied += HandleEnemyDied;
        GameEvents.OnEnemyReachedPaddle += HandleEnemyReachedPaddle;
        GameEvents.OnWaveCleared += HandleWaveCleared;
    }

    void UnsubscribeFromEvents()
    {
        GameEvents.OnEnemyDied -= HandleEnemyDied;
        GameEvents.OnEnemyReachedPaddle -= HandleEnemyReachedPaddle;
        GameEvents.OnWaveCleared -= HandleWaveCleared;
    }
    void HandleEnemyDied(Enemy enemy, int essenceReward)
    {
        int awarded = Mathf.Max(1, Mathf.RoundToInt(essenceReward * _stats.EssenceGainMultiplier));
        AddEssence(awarded);

        if (AllEnemiesCleared() && State == GameState.Combat && _roomCompleted) CompleteRoom();
    }
    void HandleEnemyReachedPaddle(Enemy enemy) => TakeDamage(1);

    void HandleWaveCleared()
    {
        if (State != GameState.Combat) return;

        _currentWaveInRoom++;
        if (_currentWaveInRoom < _totalWavesInCurrentRoom)
        {
            GameEvents.WaveStarted(_currentWaveInRoom + 1);
        }
        else
        {
            _roomCompleted = true;
            CompleteRoom();
        }
    }
    #endregion

    #region Public Methods
    public void ChangeState(GameState newState)
    {
        State = newState;
        Debug.Log($"[GameManager] State → {newState}");

        switch (newState)
        {
            case GameState.Idle:
                break;
            case GameState.RoomTransition:
                EnterNextRoom();
                break;
            case GameState.Combat:
                StartCombatRoom();
                break;
            case GameState.Shop:
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

    void EnterNextRoom()
    {
        _currentRoomIndex++;

        if (_currentRoomIndex >= TotalRooms)
        {
            ChangeState(GameState.Victory);
            return;
        }
        RoomType nextType = PathManager.Instance?.GetRoomData(_currentRoomIndex).Type ?? RoomType.Combat;

        if (nextType == RoomType.Shop)
        {
            ChangeState(GameState.Shop); // Only open shop on certain rounds (controlled by path)
        }
        else // Combat
        {
            _roomCompleted = false;
            ChangeState(GameState.Combat);
        }
    }
    void StartCombatRoom()
    {
        RoomData roomData = PathManager.Instance.GetRoomData(_currentRoomIndex);
        GameEvents.RoomEntered(roomData);
        GameEvents.WaveSetup(roomData.Waves[0].WallSize);
    }
    void CompleteRoom()
    {
        GameEvents.RoomCleared();
        StartCoroutine(HandleLevelUps());
    }
    public void LeaveRoom()
    {
        if (State != GameState.Combat && State != GameState.Shop) return;
    }
    private IEnumerator HandleLevelUps()
    {
        var inv = PlayerInventory.Instance;
        if (inv == null) yield break;

        while (inv.TryGetLevelUpRewards(out List<UpgradeData> rewards))
        {
            _waitingForLevelUp = true;
            ShopManager.Instance.ShowLevelUpChoices(rewards, () => _waitingForLevelUp = false);
            yield return new WaitUntil(() => !_waitingForLevelUp);
        }
        ChangeState(GameState.RoomTransition);
    }
    public void OnShopComplete() //Leave the shop and go to next room
    {
        if (State == GameState.Shop)
        {
            GameEvents.ShopClosed();
            ChangeState(GameState.RoomTransition);
        }
    }
    #endregion

    #region Core Game Actions
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
    public void StartGame()
    {
        if (State == GameState.Idle || State == GameState.GameOver || State == GameState.Victory)
        {
            ResetGame();
            GameEvents.GameStarted();
            ChangeState(GameState.RoomTransition);
        }
    }
    public void ResetGame()
    {
        if (PlayerInventory.Instance != null) PlayerInventory.Instance.FullReset();

        // Reset ALL run stats AND buffs (everything in GameStats)
        if (_stats != null) _stats.ResetRun();   // we'll add this method to GameStats

        EnemyStats.Reset();
        _currentRoomIndex = -1;
        _currentWaveInRoom = 0;
        _totalWavesInCurrentRoom = 0;
        _roomCompleted = false;

        MaxHealth = _baseMaxHealth;
        PlayerHealth = MaxHealth;
        Essence = 0;

        GameEvents.PlayerHealthChanged(PlayerHealth, MaxHealth);
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