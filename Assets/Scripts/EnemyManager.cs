using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance { get; private set; }

    [Header("Enemy Prefabs")]
    public GameObject EnemySquarePrefab;
    public GameObject EnemyTrianglePrefab;
    public GameObject EnemyCirclePrefab;

    [Header("Grid Layout — Horizontal")]
    [Tooltip("X position of the rightmost column (right side of screen).")]
    public float StartX = 5f;
    [Tooltip("Vertical center of the enemy grid.")]
    public float StartY = 0f;
    [Tooltip("Horizontal spacing between columns.")]
    public float SpacingX = 1.4f;
    [Tooltip("Vertical spacing between rows.")]
    public float SpacingY = 1.2f;
    [Tooltip("How far enemies move LEFT each time they advance.")]
    public float AdvanceDistance = 0.8f;

    [Header("Wave Data")]
    public WaveData[] Waves;

    // Active and queued enemies
    private List<Enemy>          _activeEnemies = new List<Enemy>();
    private Queue<EnemySpawnInfo> _spawnQueue   = new Queue<EnemySpawnInfo>();
    private int _maxActive = 0;           // 0 = unlimited
    private int _totalWaveEnemies = 0;
    private int _nextSpawnCol = 0;        // tracks which column to spawn queued enemies into
    private float _totalAdvanceX = 0f;

    // Grid parameters saved at spawn time so queued enemies land on the same grid
    private float _gridTopY;
    private int   _waveRows;
    private int   _waveColumns;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()  => SubscribeToEvents();
    void OnDestroy() => UnsubscribeFromEvents();

    // ── Event Wiring ───────────────────────────────────────────────

    public void SubscribeToEvents()
    {
        GameEvents.OnWaveStarted += HandleWaveStarted;
        GameEvents.OnRoundEnded  += HandleRoundEnded;
        GameEvents.OnGameOver    += HandleGameOver;
        GameEvents.OnVictory     += HandleGameOver;
        GameEvents.OnEnemyDied   += HandleEnemyDiedForQueue;
    }

    void UnsubscribeFromEvents()
    {
        GameEvents.OnWaveStarted -= HandleWaveStarted;
        GameEvents.OnRoundEnded  -= HandleRoundEnded;
        GameEvents.OnGameOver    -= HandleGameOver;
        GameEvents.OnVictory     -= HandleGameOver;
        GameEvents.OnEnemyDied   -= HandleEnemyDiedForQueue;
    }

    void HandleWaveStarted(int waveNumber) => SpawnWave(waveNumber);

    void HandleRoundEnded()
    {
        if (_activeEnemies.Count > 0)
            AdvanceEnemies();
    }

    void HandleGameOver() => ClearAll();

    /// <summary>When an enemy dies, pull the next one from the queue if cap allows.</summary>
    void HandleEnemyDiedForQueue(Enemy enemy, int _)
    {
        TrySpawnFromQueue();
    }

    // ── Spawn ──────────────────────────────────────────────────────

    void SpawnWave(int waveNumber)
    {
        ClearAll();
        _totalAdvanceX = 0f;

        WaveData data = GetWaveData(waveNumber);
        _maxActive     = data.MaxActiveEnemies;
        _waveRows      = data.Rows;
        _waveColumns   = data.Columns;

        float gridHeight = (_waveRows - 1) * SpacingY;
        _gridTopY        = StartY + gridHeight * 0.5f;

        // Build the full spawn queue
        _spawnQueue.Clear();
        foreach (var info in data.Enemies)
            _spawnQueue.Enqueue(info);

        _totalWaveEnemies = _spawnQueue.Count;
        _nextSpawnCol = 0;

        // Spawn up to the cap immediately (or all if cap is 0)
        int initialCount = (_maxActive > 0) ? _maxActive : _totalWaveEnemies;
        for (int i = 0; i < initialCount && _spawnQueue.Count > 0; i++)
            SpawnNextFromQueue();

        Debug.Log($"[EnemyManager] Wave {waveNumber}: {_totalWaveEnemies} total, " +
                  $"{_activeEnemies.Count} active, {_spawnQueue.Count} queued.");

        GameManager.Instance.SetWaveEnemyCount(_totalWaveEnemies);
    }

    void TrySpawnFromQueue()
    {
        if (_spawnQueue.Count == 0) return;
        bool underCap = _maxActive <= 0 || _activeEnemies.Count < _maxActive;
        if (underCap)
            SpawnNextFromQueue();
    }

    void SpawnNextFromQueue()
    {
        if (_spawnQueue.Count == 0) return;

        EnemySpawnInfo info = _spawnQueue.Dequeue();

        // Place in the next available column slot, applying any advance offset
        int col = _nextSpawnCol % _waveColumns;
        int row = (_nextSpawnCol / _waveColumns) % _waveRows;
        _nextSpawnCol++;

        Vector3 pos = new Vector3(
            StartX - col * SpacingX + _totalAdvanceX,   // respect current advance
            _gridTopY - row * SpacingY,
            0f
        );

        GameObject prefab = GetPrefabForShape(info.Shape);
        if (prefab == null) return;

        GameObject go = Instantiate(prefab, pos, Quaternion.identity);
        Enemy enemy = go.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.Initialize(info.HP, info.EssenceReward);
            _activeEnemies.Add(enemy);
        }
    }

    // ── Advance ────────────────────────────────────────────────────

    void AdvanceEnemies()
    {
        _totalAdvanceX -= AdvanceDistance;

        foreach (Enemy e in _activeEnemies)
        {
            if (e == null) continue;
            Vector3 pos = e.transform.position;
            pos.x -= AdvanceDistance;
            e.transform.position = pos;
        }

        Debug.Log($"[EnemyManager] Advanced. Total offset: {_totalAdvanceX:F1}");
        GameEvents.EnemiesDropped();
    }

    // ── Enemy Removal ──────────────────────────────────────────────

    public void OnEnemyDied(Enemy enemy)
    {
        _activeEnemies.Remove(enemy);
        Debug.Log($"[EnemyManager] Removed enemy. Active: {_activeEnemies.Count}, Queued: {_spawnQueue.Count}");
    }

    public bool AllEnemiesCleared() => _activeEnemies.Count == 0 && _spawnQueue.Count == 0;
    public int  ActiveEnemyCount   => _activeEnemies.Count;
    public int  QueuedEnemyCount   => _spawnQueue.Count;

    // ── Helpers ────────────────────────────────────────────────────

    void ClearAll()
    {
        foreach (Enemy e in _activeEnemies)
            if (e != null) Destroy(e.gameObject);
        _activeEnemies.Clear();
        _spawnQueue.Clear();
        _nextSpawnCol = 0;
    }

    WaveData GetWaveData(int waveNumber)
    {
        if (Waves == null || Waves.Length == 0)
        {
            Debug.LogWarning("[EnemyManager] No WaveData assigned! Using fallback.");
            return GenerateFallbackWave(waveNumber);
        }
        int index = Mathf.Clamp(waveNumber - 1, 0, Waves.Length - 1);
        return Waves[index];
    }

    WaveData GenerateFallbackWave(int waveNumber)
    {
        WaveData fallback       = ScriptableObject.CreateInstance<WaveData>();
        fallback.Rows           = 3;
        fallback.Columns        = 4;
        fallback.MaxActiveEnemies = 5;

        int total = fallback.Rows * fallback.Columns;
        fallback.Enemies = new EnemySpawnInfo[total];
        for (int i = 0; i < total; i++)
        {
            fallback.Enemies[i] = new EnemySpawnInfo
            {
                HP            = waveNumber + 1,
                EssenceReward = waveNumber,
                Shape         = EnemyShape.Square
            };
        }
        return fallback;
    }

    GameObject GetPrefabForShape(EnemyShape shape)
    {
        return shape switch
        {
            EnemyShape.Square   => EnemySquarePrefab,
            EnemyShape.Triangle => EnemyTrianglePrefab,
            EnemyShape.Circle   => EnemyCirclePrefab,
            _                   => EnemySquarePrefab
        };
    }
}
