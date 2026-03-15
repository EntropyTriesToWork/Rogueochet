using System.Collections.Generic;
using UnityEngine;

public class BallManager : MonoBehaviour
{
    public static BallManager Instance { get; private set; }

    [Header("Ball Settings")]
    [Tooltip("Default prefab used when PlayerInventory has no balls assigned.")]
    public GameObject BallPrefab;
    public Transform BallSpawnPoint;

    [Header("Speed Ramp")]
    [Tooltip("Seconds of real time after the first ball is launched before timeScale ramps up.")]
    public float SpeedRampDelay = 10f;
    [Tooltip("Target Time.timeScale once the ramp triggers.")]
    public float RampTargetTimeScale = 5f;
    [Tooltip("How quickly timeScale moves toward RampTargetTimeScale (units per unscaled second).")]
    public float RampAcceleration = 1f;

    [Header("Break All (Right-Click Hold)")]
    [Tooltip("Seconds the player must hold right-click to break all active balls.")]
    public float BreakHoldDuration = 1f;
    [Tooltip("Radial fill UI Image shown while holding. Assign an Image with Fill Method = Radial 360.")]
    public UnityEngine.UI.Image BreakChargeIndicator;

    [HideInInspector] public float BaseSpeedRampDelay;

    private List<Ball> _activeBalls = new List<Ball>();
    private bool  _launchEnabled    = false;
    private bool  _ballInPlay       = false;
    private float _roundTimer       = 0f;
    private bool  _rampFired        = false;
    private bool  _rampActive       = false;
    private float _timeSinceLastBall = 0f;

    private float _breakHoldTimer  = 0f;
    private bool  _breakHoldActive = false;
    private int   _nextLaunchSlot  = 0;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        BaseSpeedRampDelay = SpeedRampDelay;
    }

    void Start() => SubscribeToEvents();
    void OnDestroy() => UnsubscribeFromEvents();

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

    void HandleWaveStarted(int _) => _roundTimer = 0f;

    void HandleRoundStarted()
    {
        _launchEnabled    = true;
        _rampFired        = false;
        _rampActive       = false;
        _nextLaunchSlot   = 0;
        if (_activeBalls.Count == 0)
            _ballInPlay = false;
    }

    void HandleRoundEnded()
    {
        _launchEnabled = false;
        _rampFired     = false;
        _rampActive    = false;
        ResetTimeScale();
    }

    void HandleGameOver()
    {
        DestroyAllBalls();
        ResetTimeScale();
    }

    // ── Update ─────────────────────────────────────────────────────

    void Update()
    {
        if (_ballInPlay)
        {
            _roundTimer += Time.unscaledDeltaTime;
            GameEvents.RoundTimerTick(_roundTimer);
        }

        if (_launchEnabled)
        {
            var inv = PlayerInventory.Instance;
            bool allLaunched = inv != null
                ? _nextLaunchSlot >= inv.UsedBallSlots
                : _ballInPlay;

            if (!allLaunched && (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space)))
                LaunchNext();
        }

        HandleBreakHold();

        if (_ballInPlay)
        {
            if (!_rampFired && Time.time - _timeSinceLastBall >= SpeedRampDelay)
            {
                _rampFired  = true;
                _rampActive = true;
                GameEvents.BallSpeedRampTriggered();
                Debug.Log($"[BallManager] Speed ramp triggered → target timeScale {RampTargetTimeScale}");
            }

            if (_rampActive && Time.timeScale < RampTargetTimeScale)
            {
                Time.timeScale      = Mathf.MoveTowards(Time.timeScale, RampTargetTimeScale, RampAcceleration * Time.unscaledDeltaTime);
                Time.fixedDeltaTime = 0.02f * Time.timeScale;
            }
        }
    }

    // ── Break All ──────────────────────────────────────────────────

    void HandleBreakHold()
    {
        if (!_ballInPlay)
        {
            CancelBreakHold();
            return;
        }

        if (Input.GetMouseButton(1))
        {
            _breakHoldActive = true;
            _breakHoldTimer += Time.unscaledDeltaTime;

            if (BreakChargeIndicator != null)
            {
                BreakChargeIndicator.gameObject.SetActive(true);
                BreakChargeIndicator.fillAmount = Mathf.Clamp01(_breakHoldTimer / BreakHoldDuration);
            }

            if (_breakHoldTimer >= BreakHoldDuration)
            {
                DestroyAllBalls();
                CancelBreakHold();
            }
        }
        else if (_breakHoldActive)
        {
            CancelBreakHold();
        }
    }

    void CancelBreakHold()
    {
        _breakHoldTimer  = 0f;
        _breakHoldActive = false;
        if (BreakChargeIndicator != null)
        {
            BreakChargeIndicator.fillAmount = 0f;
            BreakChargeIndicator.gameObject.SetActive(false);
        }
    }

    // ── Launch ─────────────────────────────────────────────────────

    /// <summary>
    /// Launches one ball from the next available inventory slot.
    /// Subsequent presses cycle through remaining slots until all balls are in play.
    /// </summary>
    public void LaunchNext()
    {
        var inv = PlayerInventory.Instance;

        if (inv != null && inv.UsedBallSlots > 0)
        {
            if (_nextLaunchSlot < inv.UsedBallSlots)
            {
                LaunchFromSlot(_nextLaunchSlot);
                _nextLaunchSlot++;
            }
        }
        else
        {
            LaunchBall();
        }
    }

    void LaunchFromSlot(int slotIndex)
    {
        var inv      = PlayerInventory.Instance;
        var instance = inv?.GetBallInstanceForLaunch(slotIndex);

        GameObject prefab = (instance?.BallPrefab != null) ? instance.BallPrefab : BallPrefab;
        if (prefab == null) { Debug.LogWarning("[BallManager] No prefab for slot " + slotIndex); return; }

        Vector3 spawnPos = BallSpawnPoint != null
            ? BallSpawnPoint.position + Vector3.up * (slotIndex * 0.3f)   // slight vertical offset per ball
            : Vector3.zero;

        GameObject go   = Instantiate(prefab, spawnPos, Quaternion.identity);
        Ball       ball = go.GetComponent<Ball>();
        if (ball == null) return;

        // Apply per-ball stats
        instance?.ApplyToBall(ball);

        // Apply global multipliers on top
        if (inv != null)
        {
            ball.Damage        = Mathf.Max(1, Mathf.RoundToInt(ball.Damage * inv.GlobalDamageMultiplier));
            ball.InitialSpeed  *= inv.GlobalSpeedMultiplier;
            ball.MaxDurability  = Mathf.Max(1, ball.MaxDurability + inv.GlobalDurabilityBonus);
        }

        // Launch with a slightly different angle per slot
        float baseAngle = Random.Range(-30f, 30f);
        float slotOffset = (slotIndex - (inv.UsedBallSlots - 1) * 0.5f) * 15f;
        float angle = baseAngle + slotOffset;
        Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
        ball.Launch(dir);

        RegisterBall(ball);
        _ballInPlay = true;
        _timeSinceLastBall = Time.time;

        GameEvents.BallLaunched(ball);
        GameEvents.BallCountChanged(_activeBalls.Count);
    }

    /// <summary>Legacy single-ball launch used as fallback and by SpawnExtraBall.</summary>
    public void LaunchBall()
    {
        if (BallPrefab == null) { Debug.LogWarning("[BallManager] BallPrefab not assigned!"); return; }

        Vector3    spawnPos = BallSpawnPoint != null ? BallSpawnPoint.position : Vector3.zero;
        GameObject go       = Instantiate(BallPrefab, spawnPos, Quaternion.identity);
        Ball       ball     = go.GetComponent<Ball>();

        if (ball != null)
        {
            float   angle = Random.Range(-45f, 45f);
            Vector2 dir   = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
            ball.Launch(dir);

            RegisterBall(ball);
            _ballInPlay        = true;
            _timeSinceLastBall = Time.time;

            GameEvents.BallLaunched(ball);
            GameEvents.BallCountChanged(_activeBalls.Count);
        }
    }

    public void SpawnExtraBall(Vector3 position, Vector2 direction)
    {
        if (BallPrefab == null) return;

        GameObject go   = Instantiate(BallPrefab, position, Quaternion.identity);
        Ball       ball = go.GetComponent<Ball>();
        if (ball != null)
        {
            ball.Launch(direction);
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
        GameEvents.BallCountChanged(_activeBalls.Count);
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
        _rampFired  = false;
        _rampActive = false;
        GameEvents.BallCountChanged(0);
    }

    void ResetTimeScale()
    {
        Time.timeScale      = 1f;
        Time.fixedDeltaTime = 0.02f;
    }

    public int   ActiveBallCount => _activeBalls.Count;
    public float RoundTimer      => _roundTimer;
}
