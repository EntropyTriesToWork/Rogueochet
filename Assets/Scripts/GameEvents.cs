using System;

/// <summary>
/// Central static event bus. Any class can publish or subscribe without
/// needing a direct reference to the sender.
///
/// Usage:
///   Subscribe:   GameEvents.OnBallLost += HandleBallLost;
///   Unsubscribe: GameEvents.OnBallLost -= HandleBallLost;
///   Invoke:      GameEvents.BallLost(ball);
/// </summary>
public static class GameEvents
{
    // ── Game State ─────────────────────────────────────────────────
    public static event Action OnGameStarted;
    public static event Action OnGameOver;
    public static event Action OnVictory;

    public static void GameStarted()  => OnGameStarted?.Invoke();
    public static void GameOver()     => OnGameOver?.Invoke();
    public static void Victory()      => OnVictory?.Invoke();

    // ── Waves & Rounds ─────────────────────────────────────────────
    /// <param name="waveNumber">The wave that just started (1-based).</param>
    public static event Action<int> OnWaveStarted;

    /// <summary>All enemies in the wave have been cleared.</summary>
    public static event Action OnWaveCleared;

    /// <summary>A round started — player can now launch ball(s).</summary>
    public static event Action OnRoundStarted;

    /// <summary>All balls are gone; round is over.</summary>
    public static event Action OnRoundEnded;

    public static void WaveStarted(int waveNumber) => OnWaveStarted?.Invoke(waveNumber);
    public static void WaveCleared()               => OnWaveCleared?.Invoke();
    public static void RoundStarted()              => OnRoundStarted?.Invoke();
    public static void RoundEnded()                => OnRoundEnded?.Invoke();

    // ── Shop ───────────────────────────────────────────────────────
    public static event Action OnShopOpened;
    public static event Action OnShopClosed;

    public static void ShopOpened() => OnShopOpened?.Invoke();
    public static void ShopClosed() => OnShopClosed?.Invoke();

    // ── Player ─────────────────────────────────────────────────────
    /// <param name="currentHP">Player's HP after the change.</param>
    /// <param name="maxHP">Player's max HP.</param>
    public static event Action<int, int> OnPlayerHealthChanged;

    /// <param name="currentEssence">Essence total after the change.</param>
    public static event Action<int> OnEssenceChanged;

    public static void PlayerHealthChanged(int currentHP, int maxHP) => OnPlayerHealthChanged?.Invoke(currentHP, maxHP);
    public static void EssenceChanged(int currentEssence)            => OnEssenceChanged?.Invoke(currentEssence);

    // ── Ball ───────────────────────────────────────────────────────
    /// <param name="ball">The ball that was lost.</param>
    public static event Action<Ball> OnBallLost;

    /// <param name="ball">The ball that was launched.</param>
    public static event Action<Ball> OnBallLaunched;

    /// <param name="remainingBalls">How many balls are still active.</param>
    public static event Action<int> OnBallCountChanged;

    /// <summary>Fired when the round timer triggers max speed on all balls.</summary>
    public static event Action OnBallSpeedRampTriggered;

    /// <summary>Fired each frame while the round timer is running. Provides elapsed seconds.</summary>
    public static event Action<float> OnRoundTimerTick;

    /// <summary>Fired when a ball's HP changes. Provides current and max HP.</summary>
    public static event Action<Ball, int, int> OnBallHPChanged;

    public static void BallLost(Ball ball)                          => OnBallLost?.Invoke(ball);
    public static void BallLaunched(Ball ball)                      => OnBallLaunched?.Invoke(ball);
    public static void BallCountChanged(int remaining)              => OnBallCountChanged?.Invoke(remaining);
    public static void BallSpeedRampTriggered()                     => OnBallSpeedRampTriggered?.Invoke();
    public static void RoundTimerTick(float elapsed)                => OnRoundTimerTick?.Invoke(elapsed);
    public static void BallHPChanged(Ball ball, int cur, int max)   => OnBallHPChanged?.Invoke(ball, cur, max);

    // ── Enemy ──────────────────────────────────────────────────────
    /// <param name="enemy">The enemy that died.</param>
    /// <param name="essenceAwarded">How much essence was given to the player.</param>
    public static event Action<Enemy, int> OnEnemyDied;

    /// <param name="enemy">The enemy that took damage.</param>
    /// <param name="damage">Amount of damage dealt.</param>
    /// <param name="remainingHP">HP after damage.</param>
    public static event Action<Enemy, int, int> OnEnemyDamaged;

    /// <param name="enemy">The enemy that reached the paddle.</param>
    public static event Action<Enemy> OnEnemyReachedPaddle;

    /// <summary>All enemies dropped down one row.</summary>
    public static event Action OnEnemiesDropped;

    public static void EnemyDied(Enemy enemy, int essenceAwarded)          => OnEnemyDied?.Invoke(enemy, essenceAwarded);
    public static void EnemyDamaged(Enemy enemy, int damage, int remaining) => OnEnemyDamaged?.Invoke(enemy, damage, remaining);
    public static void EnemyReachedPaddle(Enemy enemy)                     => OnEnemyReachedPaddle?.Invoke(enemy);
    public static void EnemiesDropped()                                     => OnEnemiesDropped?.Invoke();

    // ── Utility ────────────────────────────────────────────────────
    /// <summary>
    /// Clears all subscribers from every event. Call this on scene reload
    /// or when restarting the game to prevent stale references.
    /// </summary>
    public static void ClearAllListeners()
    {
        OnGameStarted           = null;
        OnGameOver              = null;
        OnVictory               = null;
        OnWaveStarted           = null;
        OnWaveCleared           = null;
        OnRoundStarted          = null;
        OnRoundEnded            = null;
        OnShopOpened            = null;
        OnShopClosed            = null;
        OnPlayerHealthChanged   = null;
        OnEssenceChanged        = null;
        OnBallLost              = null;
        OnBallLaunched          = null;
        OnBallCountChanged      = null;
        OnBallSpeedRampTriggered = null;
        OnRoundTimerTick        = null;
        OnBallHPChanged         = null;
        OnEnemyDied             = null;
        OnEnemyDamaged          = null;
        OnEnemyReachedPaddle    = null;
        OnEnemiesDropped        = null;
    }
}
