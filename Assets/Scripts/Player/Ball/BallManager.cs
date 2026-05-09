using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallManager : MonoBehaviour
{
    #region Variables and Properties
    public static BallManager Instance { get; private set; }

    [Header("Ball Settings")]
    [Tooltip("Default prefab used when PlayerInventory has no balls assigned.")]
    public GameObject BallPrefab;
    public Transform BallSpawnPoint;
    [SerializeField] GameStats _stats;

    [Header("Speed Ramp")]
    [Tooltip("Seconds of real time after the first ball is launched before timeScale ramps up.")]
    public float SpeedRampDelay = 10f;
    [Tooltip("Target Time.timeScale once the ramp triggers.")]
    public float RampTargetTimeScale = 5f;
    [Tooltip("How quickly timeScale moves toward RampTargetTimeScale (units per unscaled second).")]
    public float RampAcceleration = 1f;

    [Header("Break All (Right-Click Hold)")]
    public float BreakHoldDuration = 1f;
    public UnityEngine.UI.Image BreakChargeIndicator;

    [Header("Ball Selection & Reload")]
    public KeyCode ReloadKey = KeyCode.R;
    public KeyCode NextBallKey = KeyCode.Tab;
    public float BaseReloadSpeed = 2f;
    public bool AutoReload = true;
    public UnityEngine.UI.Image ReloadIndicatorFill;
    [SerializeField] private Canvas _reloadIndicatorCanvas;
    [SerializeField] private RectTransform _reloadIndicatorRect;

    public float BaseBarWidth = 48f;
    public float MinBarWidth = 24f;
    public float MinimumReloadTime = 0.2f;

    [HideInInspector] public float BaseSpeedRampDelay;

    [Header("Aiming Arrow")]
    [SerializeField] GameObject aimingArrow;

    private List<Ball> _activeBalls = new List<Ball>();
    private bool _launchEnabled = false;
    private bool _ballInPlay = false;
    private float _roundTimer = 0f;
    private bool _rampFired = false;
    private bool _rampActive = false;
    private float _timeSinceLastBall = 0f;

    private int _selectedBallSlot = 0;
    private bool _reloading = false;

    private float _breakHoldTimer = 0f;
    private bool _breakHoldActive = false;
    private int _nextLaunchSlot = 0;
    private Vector2 _aimingDir = Vector2.zero;
    private Vector3 _mousePos = Vector3.zero;
    private Camera _cam;

    public int GetSelectedSlot => _selectedBallSlot;
    public bool IsBallInPlay => _ballInPlay;
    public int GetLaunchedCount => _nextLaunchSlot;
    #endregion

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        BaseSpeedRampDelay = SpeedRampDelay;
        _cam = Camera.main;
    }

    #region Events
    void Start() => SubscribeToEvents();
    void OnDestroy() => UnsubscribeFromEvents();

    public void SubscribeToEvents()
    {
        GameEvents.OnRoomEntered += HandleRoomEntered;
        GameEvents.OnRoomCleared += HandleRoomCleared;
        GameEvents.OnGameOver += HandleGameOver;
        GameEvents.OnVictory += HandleGameOver;
        GameEvents.OnWaveStarted += HandleWaveStarted;
    }

    void UnsubscribeFromEvents()
    {
        GameEvents.OnRoomEntered -= HandleRoomEntered;
        GameEvents.OnRoomCleared -= HandleRoomCleared;
        GameEvents.OnGameOver -= HandleGameOver;
        GameEvents.OnVictory -= HandleGameOver;
        GameEvents.OnWaveStarted -= HandleWaveStarted;
    }

    void HandleWaveStarted(int _) => _roundTimer = 0f;

    void HandleRoomEntered(RoomData roomData)
    {
        _launchEnabled = true;
        _rampFired = false;
        _rampActive = false;
        _nextLaunchSlot = 0;
        if (_activeBalls.Count == 0) _ballInPlay = false;
    }

    void HandleRoomCleared()
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
    #endregion

    void Update()
    {
        if (GameManager.Instance.State != GameState.Combat || PauseManager.Instance.IsPaused) return;

        if (Input.GetMouseButton(0) || Input.GetKey(KeyCode.Space)) {
            aimingArrow.gameObject.SetActive(true);
            aimingArrow.transform.position = BallSpawnPoint.position;
            _mousePos = _cam.ScreenToWorldPoint(Input.mousePosition);
            _mousePos.z = 0f;
            _aimingDir = _mousePos - BallSpawnPoint.position;
            aimingArrow.transform.right = _aimingDir;
        }
        else if (_ballInPlay)
        {
            aimingArrow.gameObject.SetActive(false);
            _roundTimer += Time.unscaledDeltaTime;
            GameEvents.RoundTimerTick(_roundTimer);
        }
        else { aimingArrow.gameObject.SetActive(false); }

        if (_launchEnabled)
        {
            var inv = PlayerInventory.Instance;
            bool allLaunched = inv != null
                ? _nextLaunchSlot >= inv.UsedBallSlots
                : _ballInPlay;
            if (!allLaunched && (Input.GetMouseButtonUp(0) || Input.GetKeyUp(KeyCode.Space)))
                LaunchBall();
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
                Time.timeScale = Mathf.MoveTowards(Time.timeScale, RampTargetTimeScale, RampAcceleration * Time.unscaledDeltaTime);
                Time.fixedDeltaTime = 0.02f * Time.timeScale;
            }
        }
        else
        {
            if (Input.GetKeyDown(ReloadKey) && !_reloading) // Handle manual reload
            {
                TryReload();
            }
            else if (Input.GetKeyDown(NextBallKey)) // Handle ball selection cycling
            {
                CycleSelectedBall();
            }
            else if (AutoReload && _launchEnabled) // Check for auto-reload when all balls are launched
            {
                var inv = PlayerInventory.Instance;
                bool allLaunched = inv != null ? _nextLaunchSlot >= inv.UsedBallSlots : true;

                if (allLaunched && !_reloading && _nextLaunchSlot > 0)
                {
                    TryReload();
                }
            }
        }
    }

    #region Ball Controls
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

                Vector2 mousePos;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(_reloadIndicatorCanvas.transform as RectTransform, Input.mousePosition, _reloadIndicatorCanvas.worldCamera, out mousePos);
                BreakChargeIndicator.rectTransform.localPosition = mousePos + Vector2.right * 16f;
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
        _breakHoldTimer = 0f;
        _breakHoldActive = false;
        if (BreakChargeIndicator != null)
        {
            BreakChargeIndicator.fillAmount = 0f;
            BreakChargeIndicator.gameObject.SetActive(false);
        }
    }
    
    void CycleSelectedBall()
    {
        var inv = PlayerInventory.Instance;
        if (inv == null || inv.UsedBallSlots <= 1) return;

        _selectedBallSlot = (_selectedBallSlot + 1) % inv.UsedBallSlots;
        GameEvents.SelectedBallChanged(_selectedBallSlot);

        Debug.Log($"[BallManager] Selected ball changed to slot {_selectedBallSlot}");
    }
    public void LaunchSelectedBall() // Modify LaunchFromSlot to use selected ball instead of sequential
    {
        if (!_launchEnabled || _ballInPlay) return;

        var inv = PlayerInventory.Instance;
        if (inv != null && inv.UsedBallSlots > 0)
        {
            if (_selectedBallSlot < inv.UsedBallSlots)
            {
                LaunchFromSlot(_selectedBallSlot);
                // Move to next slot for next launch if we're launching sequentially
                // Or keep same slot if you want multi-shot of same ball type
                _nextLaunchSlot = _selectedBallSlot + 1;

                if (_nextLaunchSlot >= inv.UsedBallSlots)
                {
                    _ballInPlay = true;
                }
            }
        }
        else
        {
            LaunchBall();
        }
    }
    void LaunchFromSlot(int slotIndex)
    {
        var inv = PlayerInventory.Instance;
        var instance = inv?.GetBallInstanceForLaunch(slotIndex);

        GameObject prefab = (instance?.BallPrefab != null) ? instance.BallPrefab : BallPrefab;
        if (prefab == null) { Debug.LogWarning("[BallManager] No prefab for slot " + slotIndex); return; }

        Vector3 spawnPos = BallSpawnPoint != null ? BallSpawnPoint.position + Vector3.up * (slotIndex * 0.3f)   // slight vertical offset per ball
            : Vector3.zero;

        GameObject go   = Instantiate(prefab, spawnPos, Quaternion.identity);
        Ball ball = go.GetComponent<Ball>();
        if (ball == null) return;

        instance?.ApplyToBall(ball);

        if (inv != null)
        {
            ball.Damage = Mathf.Max(1, Mathf.RoundToInt(ball.Damage * _stats.GlobalDamageMultiplier));
            ball.InitialSpeed *= _stats.GlobalSpeedMultiplier;
            ball.MaxDurability = Mathf.Max(1, ball.MaxDurability + _stats.GlobalDurabilityBonus);
        }
        float baseAngle = Random.Range(-30f, 30f); //Add random angle
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
    public void LaunchBall()
    {
        if (BallPrefab == null) { Debug.LogWarning("[BallManager] BallPrefab not assigned!"); return; }

        Vector3 spawnPos = BallSpawnPoint != null ? BallSpawnPoint.position : Vector3.zero;
        GameObject go = Instantiate(BallPrefab, spawnPos, Quaternion.identity);
        Ball ball = go.GetComponent<Ball>();

        if (ball != null)
        {
            ball.Launch(_aimingDir);
            RegisterBall(ball);
            _ballInPlay        = true;
            _timeSinceLastBall = Time.time;

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
        GameEvents.BallCountChanged(_activeBalls.Count);
        if (_activeBalls.Count == 0) _ballInPlay = false;
    }
    public void DestroyAllBalls()
    {
        foreach (var ball in _activeBalls) if (ball != null) Destroy(ball.gameObject);

        _activeBalls.Clear();
        _ballInPlay = false;
        _roundTimer = 0f;
        _rampFired  = false;
        _rampActive = false;
        GameEvents.BallCountChanged(0);
    }
    #endregion

    #region Reload
    public void TryReload()
    {
        if (_reloading) return;
        _nextLaunchSlot = 0;
        _selectedBallSlot = 0;
        _ballInPlay = false;
        StartCoroutine(ReloadCoroutine(BaseReloadSpeed));

        GameEvents.ReloadTriggered();
        Debug.Log("[BallManager] Reload triggered");
    }
    public float GetReloadBarWidth(float reloadDuration)
    {
        if (reloadDuration <= 0.5f) return MinBarWidth;
        float extraTime = reloadDuration - 0.5f;
        float extraWidth = extraTime * BaseBarWidth; // e.g., each extra second adds BaseBarWidth
        return MinBarWidth + extraWidth;
    }
    IEnumerator ReloadCoroutine(float reloadTime)
    {
        _reloading = true;
        float barWidth = GetReloadBarWidth(reloadTime);
        ReloadIndicatorFill.fillAmount = 0f;
        _reloadIndicatorRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, barWidth);
        _reloadIndicatorRect.gameObject.SetActive(true);

        float elapsed = 0f;
        while (elapsed < reloadTime)
        {
            elapsed += Time.deltaTime;
            float fillAmount = elapsed / reloadTime;
            ReloadIndicatorFill.fillAmount = fillAmount;

            Vector2 mousePos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_reloadIndicatorCanvas.transform as RectTransform, Input.mousePosition, _reloadIndicatorCanvas.worldCamera, out mousePos);
            _reloadIndicatorRect.localPosition = mousePos + Vector2.up * -24f;

            yield return null;
        }
        GameEvents.SelectedBallChanged(0);
        GameEvents.ReloadCompleted();
        _reloading = false;
        _reloadIndicatorRect.gameObject.SetActive(false);
    }
    #endregion
    void ResetTimeScale()
    {
        Time.timeScale      = 1f;
        Time.fixedDeltaTime = 0.02f;
    }
    public int ActiveBallCount => _activeBalls.Count;
    public float RoundTimer => _roundTimer;
}
