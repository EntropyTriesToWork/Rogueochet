using System;

public static class GameEvents
{
    public static event Action OnGameStarted;
    public static event Action OnGameOver;
    public static event Action OnVictory;

    public static void GameStarted()  => OnGameStarted?.Invoke();
    public static void GameOver()     => OnGameOver?.Invoke();
    public static void Victory()      => OnVictory?.Invoke();

    public static event Action<int> OnWaveStarted;
    public static event Action OnWaveCleared;
    public static event Action OnRoundStarted;
    public static event Action OnRoundEnded;

    public static void WaveStarted(int waveNumber) => OnWaveStarted?.Invoke(waveNumber);
    public static void WaveCleared()               => OnWaveCleared?.Invoke();
    public static void RoundStarted()              => OnRoundStarted?.Invoke();
    public static void RoundEnded()                => OnRoundEnded?.Invoke();

    public static event Action OnShopOpened;
    public static event Action OnShopClosed;
    public static event Action OnShopOfferingsChanged;   // ← NEW: cards refreshed

    public static void ShopOpened()            => OnShopOpened?.Invoke();
    public static void ShopClosed()            => OnShopClosed?.Invoke();
    public static void ShopOfferingsChanged()  => OnShopOfferingsChanged?.Invoke();

    public static event Action<int, int> OnPlayerHealthChanged;
    public static event Action<int>      OnEssenceChanged;

    public static void PlayerHealthChanged(int currentHP, int maxHP) => OnPlayerHealthChanged?.Invoke(currentHP, maxHP);
    public static void EssenceChanged(int currentEssence)            => OnEssenceChanged?.Invoke(currentEssence);

    public static event Action<Ball>      OnBallLost;
    public static event Action<Ball>      OnBallLaunched;
    public static event Action<int>       OnBallCountChanged;
    public static event Action            OnBallSpeedRampTriggered;
    public static event Action<float>     OnRoundTimerTick;
    public static event Action<Ball, int, int> OnBallDurabilityChanged;

    public static void BallLost(Ball ball)                               => OnBallLost?.Invoke(ball);
    public static void BallLaunched(Ball ball)                           => OnBallLaunched?.Invoke(ball);
    public static void BallCountChanged(int remaining)                   => OnBallCountChanged?.Invoke(remaining);
    public static void BallSpeedRampTriggered()                          => OnBallSpeedRampTriggered?.Invoke();
    public static void RoundTimerTick(float elapsed)                     => OnRoundTimerTick?.Invoke(elapsed);
    public static void BallDurabilityChanged(Ball ball, int cur, int max)=> OnBallDurabilityChanged?.Invoke(ball, cur, max);

    public static event Action<Enemy, int>      OnEnemyDied;
    public static event Action<Enemy, int, int> OnEnemyDamaged;
    public static event Action<Enemy>           OnEnemyReachedPaddle;
    public static event Action                  OnEnemiesDropped;

    public static void EnemyDied(Enemy enemy, int essenceAwarded)           => OnEnemyDied?.Invoke(enemy, essenceAwarded);
    public static void EnemyDamaged(Enemy enemy, int damage, int remaining) => OnEnemyDamaged?.Invoke(enemy, damage, remaining);
    public static void EnemyReachedPaddle(Enemy enemy)                      => OnEnemyReachedPaddle?.Invoke(enemy);
    public static void EnemiesDropped()                                      => OnEnemiesDropped?.Invoke();

    public static event Action<int> OnPlayerLevelUp;   // ← NEW: new level value
    public static void PlayerLevelUp(int newLevel) => OnPlayerLevelUp?.Invoke(newLevel);
    public static event Action<float> OnPaddleUpdated;
    public static void PaddleUpdated(float newHalfSize) { OnPaddleUpdated.Invoke(newHalfSize); }

    public static void ClearAllListeners()
    {
        OnGameStarted            = null;
        OnGameOver               = null;
        OnVictory                = null;
        OnWaveStarted            = null;
        OnWaveCleared            = null;
        OnRoundStarted           = null;
        OnRoundEnded             = null;
        OnShopOpened             = null;
        OnShopClosed             = null;
        OnShopOfferingsChanged   = null;
        OnPlayerHealthChanged    = null;
        OnEssenceChanged         = null;
        OnBallLost               = null;
        OnBallLaunched           = null;
        OnBallCountChanged       = null;
        OnBallSpeedRampTriggered = null;
        OnRoundTimerTick         = null;
        OnBallDurabilityChanged  = null;
        OnEnemyDied              = null;
        OnEnemyDamaged           = null;
        OnEnemyReachedPaddle     = null;
        OnEnemiesDropped         = null;
        OnPlayerLevelUp          = null;
        OnPaddleUpdated = null;
    }
}
