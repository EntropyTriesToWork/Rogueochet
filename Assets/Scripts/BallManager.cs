using System.Collections.Generic;
using UnityEngine;

public class BallManager : MonoBehaviour
{
    public static BallManager Instance { get; private set; }

    [Header("Ball Settings")]
    public GameObject BallPrefab;
    public Transform BallSpawnPoint;

    [Header("Speed Ramp")]
    [Tooltip("Seconds of real time after the first ball is launched before timeScale ramps up.")]
    public float SpeedRampDelay = 10f;
    [Tooltip("Target Time.timeScale once the ramp triggers.")]
    public float RampTargetTimeScale = 5f;
    [Tooltip("How quickly timeScale moves toward RampTargetTimeScale (units per unscaled second).")]
    public float RampAcceleration = 1f;

    private List<Ball> _activeBalls = new List<Ball>();
    private bool _launchEnabled = false;
    private bool _ballInPlay = false;
    private float _roundTimer = 0f;      // unscaled time since first launch this round
    private bool _rampFired = false;
    private bool _rampActive = false;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start() => SubscribeToEvents();
    void OnDestroy() => UnsubscribeFromEvents();

    // ── Event Wiring ───────────────────────────────────────────────

    public void SubscribeToEvents()
    {
        GameEvents.OnRoundStarted += HandleRoundStarted;
        GameEvents.OnRoundEnded   += HandleRoundEnded;
        GameEvents.OnGameOver     += HandleGameOver;
        GameEvents.OnVictory      += HandleGameOver;
        GameEvents.OnWaveStarted  += HandleWaveStarted;
    }

    void UnsubscribeFromEvents()
    {
        GameEvents.OnRoundStarted -= HandleRoundStarted;
        GameEvents.OnRoundEnded   -= HandleRoundEnded;
        GameEvents.OnGameOver     -= HandleGameOver;
        GameEvents.OnVictory      -= HandleGameOver;
        GameEvents.OnWaveStarted  -= HandleWaveStarted;
    }
    private void HandleWaveStarted(int obj)
    {
        _roundTimer = 0;
    }

    void HandleRoundStarted()
    {
        _launchEnabled = true;
        _rampFired = false;
        _rampActive = false;
        if (_activeBalls.Count == 0)
            _ballInPlay = false;
    }

    void HandleRoundEnded()
    {
        _launchEnabled = false;
        _rampFired = false;
        _rampActive = false;
        ResetTimeScale();
    }

    void HandleGameOver()
    {
        DestroyAllBalls();
        ResetTimeScale();
    }
    void Update()
    {
        if (_ballInPlay)
        {
            _roundTimer += Time.unscaledDeltaTime;
            GameEvents.RoundTimerTick(_roundTimer);
        }

        if (_launchEnabled && !_ballInPlay)
        {
            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
                LaunchBall();
        }

        if (_ballInPlay)
        {
            if (!_rampFired)
            {
                if (Time.time - _timeSinceLastBall >= SpeedRampDelay)
                {
                    _rampFired = true;
                    _rampActive = true;
                    GameEvents.BallSpeedRampTriggered();
                    Debug.Log($"[BallManager] Speed ramp triggered at {_roundTimer:F1}s → target timeScale {RampTargetTimeScale}");
                }
            }
            if (_rampActive && Time.timeScale < RampTargetTimeScale)
            {
                Time.timeScale = Mathf.MoveTowards(
                    Time.timeScale,
                    RampTargetTimeScale,
                    RampAcceleration * Time.unscaledDeltaTime
                );
                Time.fixedDeltaTime = 0.02f * Time.timeScale; // keep physics stable
            }
        }
    }
    float _timeSinceLastBall = 0f;
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
            // Direction only — each Ball uses its own InitialSpeed
            float angle = Random.Range(-45f, 45f);
            Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
            ball.Launch(dir);

            RegisterBall(ball);
            _ballInPlay = true;

            GameEvents.BallLaunched(ball);
            GameEvents.BallCountChanged(_activeBalls.Count);

            _timeSinceLastBall = Time.time;
        }
    }

    public void SpawnExtraBall(Vector3 position, Vector2 direction)
    {
        if (BallPrefab == null) return;

        GameObject go = Instantiate(BallPrefab, position, Quaternion.identity);
        Ball ball = go.GetComponent<Ball>();
        if (ball != null)
        {
            ball.Launch(direction);
            RegisterBall(ball);
            GameEvents.BallLaunched(ball);
            GameEvents.BallCountChanged(_activeBalls.Count);
        }
    }
    public void RegisterBall(Ball ball)
    {
        if (!_activeBalls.Contains(ball))
            _activeBalls.Add(ball);
    }
    public void OnBallLost(Ball ball)
    {
        RemoveBall(ball);
        GameEvents.BallLost(ball);
        Debug.Log($"[BallManager] Ball lost. Remaining: {_activeBalls.Count}");
    }
    public void OnBallDestroyed(Ball ball)
    {
        RemoveBall(ball);
        Debug.Log($"[BallManager] Ball destroyed (durability=0). Remaining: {_activeBalls.Count}");
    }

    void RemoveBall(Ball ball)
    {
        _activeBalls.Remove(ball);
        Destroy(ball.gameObject);

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
        _rampActive = false;

        GameEvents.BallCountChanged(0);
    }

    void ResetTimeScale()
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
    }

    public int   ActiveBallCount => _activeBalls.Count;
    public float RoundTimer      => _roundTimer;
}
