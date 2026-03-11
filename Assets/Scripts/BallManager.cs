using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages ball spawning, launching, lifetime tracking, and the speed ramp timer.
/// Communicates exclusively through GameEvents.
/// </summary>
public class BallManager : MonoBehaviour
{
    public static BallManager Instance { get; private set; }

    [Header("Ball Settings")]
    public GameObject BallPrefab;
    public Transform BallSpawnPoint;

    [Header("Speed Ramp")]
    [Tooltip("Seconds after launch before all balls start ramping to max speed.")]
    public float SpeedRampDelay = 10f;
    [Tooltip("Multiplier applied to LaunchSpeed to set each ball's MaxRampSpeed.")]
    public float SpeedRampMultiplier = 5f;

    private List<Ball> _activeBalls = new List<Ball>();
    private bool _launchEnabled = false;
    private bool _ballInPlay = false;
    private float _roundTimer = 0f;
    private bool _rampFired = false;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        SubscribeToEvents();
    }

    void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    // ── Event Wiring ───────────────────────────────────────────────

    public void SubscribeToEvents()
    {
        GameEvents.OnRoundStarted += HandleRoundStarted;
        GameEvents.OnRoundEnded   += HandleRoundEnded;
        GameEvents.OnGameOver     += HandleGameOver;
        GameEvents.OnVictory      += HandleGameOver;
    }

    void UnsubscribeFromEvents()
    {
        GameEvents.OnRoundStarted -= HandleRoundStarted;
        GameEvents.OnRoundEnded   -= HandleRoundEnded;
        GameEvents.OnGameOver     -= HandleGameOver;
        GameEvents.OnVictory      -= HandleGameOver;
    }

    void HandleRoundStarted()
    {
        _launchEnabled = true;
        _roundTimer = 0f;
        _rampFired = false;
        if (_activeBalls.Count == 0)
            _ballInPlay = false;
    }

    void HandleRoundEnded()
    {
        _launchEnabled = false;
        _roundTimer = 0f;
        _rampFired = false;
    }

    void HandleGameOver()
    {
        DestroyAllBalls();
    }

    // ── Update ─────────────────────────────────────────────────────

    void Update()
    {
        // Launch input
        if (_launchEnabled && !_ballInPlay)
        {
            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
                LaunchBall();
        }

        // Speed ramp timer — only ticks while balls are in play
        if (_ballInPlay && !_rampFired)
        {
            _roundTimer += Time.deltaTime;
            GameEvents.RoundTimerTick(_roundTimer);   // UIManager listens to display this

            if (_roundTimer >= SpeedRampDelay)
            {
                _rampFired = true;
                // Set target ramp speed on all active balls before firing the event
                foreach (Ball b in _activeBalls)
                    if (b != null) b.MaxRampSpeed = SpeedRampMultiplier;

                GameEvents.BallSpeedRampTriggered();
                Debug.Log($"[BallManager] Speed ramp triggered at {_roundTimer:F1}s → target {SpeedRampMultiplier} u/s");
            }
        }
    }

    // ── Launch ─────────────────────────────────────────────────────

    public void LaunchBall()
    {
        if (BallPrefab == null)
        {
            Debug.LogWarning("[BallManager] BallPrefab not assigned!");
            return;
        }

        Vector3 spawnPos = BallSpawnPoint != null ? BallSpawnPoint.position : Vector3.zero;
        GameObject go = Instantiate(BallPrefab, spawnPos, Quaternion.identity);
        Ball ball = go.GetComponent<Ball>();

        if (ball != null)
        {
            float angle = Random.Range(-45f, 45f);
            Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
            ball.MaxRampSpeed = SpeedRampMultiplier;
            ball.Launch(dir);

            RegisterBall(ball);
            _ballInPlay = true;

            GameEvents.BallLaunched(ball);
            GameEvents.BallCountChanged(_activeBalls.Count);
        }
    }

    /// <summary>Spawn an extra ball mid-round (e.g. from an upgrade).</summary>
    public void SpawnExtraBall(Vector3 position, Vector2 velocity)
    {
        if (BallPrefab == null) return;

        GameObject go = Instantiate(BallPrefab, position, Quaternion.identity);
        Ball ball = go.GetComponent<Ball>();
        if (ball != null)
        {
            ball.MaxRampSpeed = SpeedRampMultiplier;
            if (_rampFired) ball.ActivateRamp();
            ball.Launch(velocity);
            RegisterBall(ball);
            GameEvents.BallLaunched(ball);
            GameEvents.BallCountChanged(_activeBalls.Count);
        }
    }

    // ── Ball Lifecycle ─────────────────────────────────────────────

    public void RegisterBall(Ball ball)
    {
        if (!_activeBalls.Contains(ball))
            _activeBalls.Add(ball);
    }

    /// <summary>Ball passed the death line — counts as lost (round can end).</summary>
    public void OnBallLost(Ball ball)
    {
        RemoveBall(ball, fireCountChanged: true);
        GameEvents.BallLost(ball);
        Debug.Log($"[BallManager] Ball lost. Remaining: {_activeBalls.Count}");
    }

    /// <summary>Ball ran out of HP — destroyed mid-round.</summary>
    public void OnBallDestroyed(Ball ball)
    {
        RemoveBall(ball, fireCountChanged: true);
        Debug.Log($"[BallManager] Ball destroyed (HP=0). Remaining: {_activeBalls.Count}");
    }

    void RemoveBall(Ball ball, bool fireCountChanged)
    {
        _activeBalls.Remove(ball);
        Destroy(ball.gameObject);

        if (fireCountChanged)
            GameEvents.BallCountChanged(_activeBalls.Count);   // GameManager triggers RoundEnd at 0

        if (_activeBalls.Count == 0)
            _ballInPlay = false;
    }

    public void DestroyAllBalls()
    {
        foreach (var ball in _activeBalls)
            if (ball != null) Destroy(ball.gameObject);

        _activeBalls.Clear();
        _ballInPlay = false;
        _roundTimer = 0f;
        _rampFired = false;

        GameEvents.BallCountChanged(0);
    }

    public int ActiveBallCount => _activeBalls.Count;
    public float RoundTimer    => _roundTimer;
}
