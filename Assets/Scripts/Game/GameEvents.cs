using System;

public static class GameEvents
{
    #region Game 
    public static event Action OnGameStarted;
    public static event Action OnGameOver;
    public static event Action OnVictory;

    public static void GameStarted() => OnGameStarted?.Invoke();
    public static void GameOver() => OnGameOver?.Invoke();
    public static void Victory() => OnVictory?.Invoke();

    public static event Action<int> OnWaveStarted;
    public static event Action<float> OnWaveSetup;
    public static event Action OnWaveCleared;
    public static event Action<RoomData> OnRoomEntered;
    public static event Action OnRoomCleared;

    public static void WaveStarted(int waveNumber) => OnWaveStarted?.Invoke(waveNumber);
    public static void WaveSetup(float wallScale) => OnWaveSetup?.Invoke(wallScale);
    public static void WaveCleared() => OnWaveCleared?.Invoke();
    public static void RoomEntered(RoomData roomData) => OnRoomEntered?.Invoke(roomData);
    public static void RoomCleared() => OnRoomCleared?.Invoke();
    #endregion

    #region Shop
    public static event Action OnShopOpened;
    public static event Action OnShopClosed;
    public static event Action OnShopOfferingsChanged;

    public static void ShopOpened() => OnShopOpened?.Invoke();
    public static void ShopClosed() => OnShopClosed?.Invoke();
    public static void ShopOfferingsChanged() => OnShopOfferingsChanged?.Invoke();
    #endregion

    #region Player
    public static event Action<int, int> OnPlayerHealthChanged;
    public static event Action<int> OnEssenceChanged;
    public static event Action<int> OnLevelUp;


    public static void PlayerHealthChanged(int currentHP, int maxHP) => OnPlayerHealthChanged?.Invoke(currentHP, maxHP);
    public static void EssenceChanged(int currentEssence) => OnEssenceChanged?.Invoke(currentEssence);
    public static void LevelUp(int newLevel) => OnLevelUp?.Invoke(newLevel);
    #endregion

    #region Ball
    public static event Action<Ball> OnBallLost;
    public static event Action<Ball> OnBallLaunched;
    public static event Action<int> OnBallCountChanged;
    public static event Action OnBallSpeedRampTriggered;
    public static event Action<float> OnRoundTimerTick;
    public static event Action<Ball, int, int> OnBallDurabilityChanged;
    public static event Action OnReloadTriggered;
    public static event Action OnReloadCompleted;
    public static event Action<int> OnSelectedBallChanged; // int = slot index

    public static void BallLost(Ball ball) => OnBallLost?.Invoke(ball);
    public static void BallLaunched(Ball ball) => OnBallLaunched?.Invoke(ball);
    public static void BallCountChanged(int remaining) => OnBallCountChanged?.Invoke(remaining);
    public static void BallSpeedRampTriggered() => OnBallSpeedRampTriggered?.Invoke();
    public static void RoundTimerTick(float elapsed) => OnRoundTimerTick?.Invoke(elapsed);
    public static void BallDurabilityChanged(Ball ball, int cur, int max)=> OnBallDurabilityChanged?.Invoke(ball, cur, max);
    public static void ReloadTriggered() => OnReloadTriggered?.Invoke();
    public static void ReloadCompleted() => OnReloadCompleted?.Invoke();
    public static void SelectedBallChanged(int slotIndex) => OnSelectedBallChanged?.Invoke(slotIndex);
    #endregion

    #region Enemy
    public static event Action<Enemy, int> OnEnemyDied;
    public static event Action<Enemy, int, int> OnEnemyDamaged;
    public static event Action<Enemy> OnEnemyReachedPaddle;
    public static event Action<Enemy> OnEnemySpawned;
    public static event Action OnAllEnemiesSpawned;

    public static void EnemyDied(Enemy enemy, int essenceAwarded)  => OnEnemyDied?.Invoke(enemy, essenceAwarded);
    public static void EnemyDamaged(Enemy enemy, int damage, int remaining) => OnEnemyDamaged?.Invoke(enemy, damage, remaining);
    public static void EnemyReachedPaddle(Enemy enemy) => OnEnemyReachedPaddle?.Invoke(enemy);
    public static void EnemySpawn(Enemy enemy) => OnEnemySpawned?.Invoke(enemy);
    public static void AllEnemiesSpawned() => OnAllEnemiesSpawned?.Invoke();
    #endregion

    #region Inventory
    public static event Action OnInventoryChanged;
    public static void InventoryChanged() => OnInventoryChanged?.Invoke();
    #endregion

    public static void ClearAllListeners()
    {
        // Game
        OnGameStarted = null;
        OnGameOver = null;
        OnVictory = null;
        OnWaveStarted = null;
        OnWaveSetup = null;
        OnWaveCleared = null;
        OnRoomEntered = null;
        OnRoomCleared = null;

        // Shop
        OnShopOpened = null;
        OnShopClosed = null;
        OnShopOfferingsChanged = null;

        // Player
        OnPlayerHealthChanged = null;
        OnEssenceChanged = null;
        OnLevelUp = null;

        // Ball
        OnBallLost = null;
        OnBallLaunched = null;
        OnBallCountChanged = null;
        OnBallSpeedRampTriggered = null;
        OnRoundTimerTick = null;
        OnBallDurabilityChanged = null;
        OnReloadTriggered = null;
        OnSelectedBallChanged = null;

        // Enemy
        OnEnemyDied = null;
        OnEnemyDamaged = null;
        OnEnemyReachedPaddle = null;
        OnAllEnemiesSpawned = null;

        // Inventory
        OnInventoryChanged = null;
    }
}
