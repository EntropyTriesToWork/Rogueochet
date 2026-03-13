using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance { get; private set; }

    #region Inspector

    [Header("Fallback")]
    [Tooltip("Used only when no WaveData assets are assigned.")]
    public GameObject FallbackEnemyPrefab;

    [Header("Grid Layout")]
    [Tooltip("X position of the rightmost column.")]
    public float StartX = 5f;
    [Tooltip("Vertical centre of the enemy grid.")]
    public float StartY = 0f;
    [Tooltip("Horizontal spacing between columns.")]
    public float SpacingX = 1.4f;
    [Tooltip("Vertical spacing between rows.")]
    public float SpacingY = 1.2f;
    [Tooltip("How far enemies move left each time they advance.")]
    public float AdvanceDistance = 0.8f;

    [Header("Wave Data")]
    public WaveData[] Waves;

    #endregion

    #region Private State

    private List<Enemy>          _activeEnemies   = new List<Enemy>();
    private Queue<ResolvedEnemy> _spawnQueue      = new Queue<ResolvedEnemy>();

    private int   _maxActive      = 0;
    private int   _nextCellIndex  = 0;
    private float _totalAdvanceX  = 0f;
    private int   _remainingInWave = 0;

    private float    _gridTopY;
    private int      _waveRows;
    private int      _waveColumns;
    private WaveData _currentWaveData;
    private Coroutine _popInCoroutine;

    public float CurrentWallScale { get; private set; } = 1f;

    #endregion

    #region Lifecycle

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()     => SubscribeToEvents();
    void OnDestroy() => UnsubscribeFromEvents();

    #endregion

    #region Events

    public void SubscribeToEvents()
    {
        GameEvents.OnWaveStarted += HandleWaveStarted;
        GameEvents.OnRoundEnded  += HandleRoundEnded;
        GameEvents.OnGameOver    += HandleGameOver;
        GameEvents.OnVictory     += HandleGameOver;
        GameEvents.OnEnemyDied   += HandleEnemyDied;
    }

    void UnsubscribeFromEvents()
    {
        GameEvents.OnWaveStarted -= HandleWaveStarted;
        GameEvents.OnRoundEnded  -= HandleRoundEnded;
        GameEvents.OnGameOver    -= HandleGameOver;
        GameEvents.OnVictory     -= HandleGameOver;
        GameEvents.OnEnemyDied   -= HandleEnemyDied;
    }

    void HandleWaveStarted(int waveNumber) => SpawnWave(waveNumber);
    void HandleRoundEnded() { if (_activeEnemies.Count > 0) AdvanceEnemies(); }
    void HandleGameOver() => ClearAll();

    void HandleEnemyDied(Enemy enemy, int _)
    {
        RemoveEnemy(enemy);
        TrySpawnFromQueue();
    }

    #endregion

    #region Wave Generation

    void SpawnWave(int waveNumber)
    {
        ClearAll();
        _totalAdvanceX = 0f;

        WaveData data    = GetWaveData(waveNumber);
        _currentWaveData = data;
        _maxActive       = data.MaxActiveEnemies;
        _waveRows        = data.Rows;
        _waveColumns     = data.Columns;

        CurrentWallScale = data.WallScale;
        GameEvents.WaveSetup(data.WallScale);

        float gridHeight = (_waveRows - 1) * SpacingY;
        _gridTopY        = StartY + gridHeight * 0.5f;

        int totalCells  = _waveRows * _waveColumns;
        int spawnTarget = Mathf.Min(data.ResolvedTotalEnemies, totalCells);

        List<int> cellOrder = BuildShuffledCells(totalCells, data.EmptyCellChance, spawnTarget);

        foreach (int cellIndex in cellOrder)
        {
            EnemySpawnInfo entry = data.PickRandom();
            if (entry == null)
            {
                Debug.LogWarning("[EnemyManager] PickRandom returned null — check pool weights/prefabs.");
                continue;
            }

            _spawnQueue.Enqueue(new ResolvedEnemy
            {
                Prefab        = entry.Prefab,
                HP            = data.ComputeHP(entry),
                EssenceReward = WaveData.RollEssenceReward(entry.Tier),
                Col           = cellIndex % _waveColumns,
                Row           = cellIndex / _waveColumns,
            });
        }

        _remainingInWave = _spawnQueue.Count;

        CurrentWallScale = data.WallScale;

        if (_popInCoroutine != null) StopCoroutine(_popInCoroutine);
        int initialCount = (_maxActive > 0) ? _maxActive : _remainingInWave;
        _popInCoroutine = StartCoroutine(PopInInitialEnemies(initialCount));

        Debug.Log($"[EnemyManager] Wave {waveNumber}: {_remainingInWave} total " +
                  $"({_activeEnemies.Count} active, {_spawnQueue.Count} queued) " +
                  $"BaseHP={data.BaseHP} WaveMult={data.WaveHPMultiplier:F2}");

        GameManager.Instance.SetWaveEnemyCount(_remainingInWave);
    }
    IEnumerator PopInInitialEnemies(int count)
    {
        for (int i = 0; i < count && _spawnQueue.Count > 0; i++)
        {
            SpawnNextFromQueue();
            yield return new WaitForSecondsRealtime(0.1f);
        }
        _popInCoroutine = null;
    }
    List<int> BuildShuffledCells(int totalCells, float emptyCellChance, int spawnTarget)
    {
        List<int> allCells = new List<int>(totalCells);
        for (int i = 0; i < totalCells; i++)
            allCells.Add(i);

        for (int i = allCells.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (allCells[i], allCells[j]) = (allCells[j], allCells[i]);
        }

        List<int> selected = new List<int>();
        foreach (int cell in allCells)
        {
            if (selected.Count >= spawnTarget) break;
            if (emptyCellChance > 0f && Random.value < emptyCellChance) continue;
            selected.Add(cell);
        }

        selected.Sort();
        return selected;
    }

    #endregion

    #region Spawning

    void TrySpawnFromQueue()
    {
        if (_spawnQueue.Count == 0) return;
        if (_maxActive <= 0 || _activeEnemies.Count < _maxActive)
            SpawnNextFromQueue();
    }

    void SpawnNextFromQueue()
    {
        if (_spawnQueue.Count == 0) return;
        InstantiateEnemy(_spawnQueue.Dequeue());
    }

    /// <summary>
    /// Spawns enemies from the pool directly, bypassing the active cap.
    /// These enemies are added to _remainingInWave and tracked normally.
    /// The cap will resume blocking queue spawns until active count drops below it.
    /// </summary>
    public void ForceSpawnEnemy(int count, WaveData overrideData = null)
    {
        if (count <= 0) return;

        WaveData data = overrideData != null ? overrideData : _currentWaveData;
        if (data == null)
        {
            Debug.LogWarning("[EnemyManager] ForceSpawnEnemy called but no active WaveData.");
            return;
        }

        int gridTotal = _waveRows * _waveColumns;
        for (int i = 0; i < count; i++)
        {
            EnemySpawnInfo entry = data.PickRandom();
            if (entry == null) continue;

            int cellIndex = (_nextCellIndex + i) % Mathf.Max(1, gridTotal);

            InstantiateEnemy(new ResolvedEnemy
            {
                Prefab        = entry.Prefab,
                HP            = data.ComputeHP(entry),
                EssenceReward = WaveData.RollEssenceReward(entry.Tier),
                Col           = cellIndex % _waveColumns,
                Row           = cellIndex / _waveColumns,
            });

            _remainingInWave++;
        }

        Debug.Log($"[EnemyManager] Force-spawned {count} enemies. Remaining: {_remainingInWave}");
    }

    /// <summary>
    /// Adds enemies to the back of the spawn queue. They obey the active cap and
    /// trickle in as slots open. Intended for enemy abilities that summon reinforcements.
    /// </summary>
    public void AddEnemiesToWave(int count, WaveData overrideData = null)
    {
        if (count <= 0) return;

        WaveData data = overrideData != null ? overrideData : _currentWaveData;
        if (data == null)
        {
            Debug.LogWarning("[EnemyManager] AddEnemiesToWave called but no active WaveData.");
            return;
        }

        int gridTotal = Mathf.Max(1, _waveRows * _waveColumns);
        int added = 0;

        for (int i = 0; i < count; i++)
        {
            EnemySpawnInfo entry = data.PickRandom();
            if (entry == null) continue;

            int cellIndex = (_nextCellIndex + _spawnQueue.Count + i) % gridTotal;

            _spawnQueue.Enqueue(new ResolvedEnemy
            {
                Prefab        = entry.Prefab,
                HP            = data.ComputeHP(entry),
                EssenceReward = WaveData.RollEssenceReward(entry.Tier),
                Col           = cellIndex % _waveColumns,
                Row           = cellIndex / _waveColumns,
            });
            added++;
        }

        _remainingInWave += added;
        Debug.Log($"[EnemyManager] Queued {added} reinforcements. Remaining: {_remainingInWave}");
        TrySpawnFromQueue();
    }

    void InstantiateEnemy(ResolvedEnemy resolved)
    {
        Vector3 pos = new Vector3(
            StartX - resolved.Col * SpacingX + _totalAdvanceX,
            _gridTopY - resolved.Row * SpacingY,
            0f
        );

        GameObject go    = Instantiate(resolved.Prefab, pos, Quaternion.identity);
        Enemy      enemy = go.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.Initialize(resolved.HP, resolved.EssenceReward);
            _activeEnemies.Add(enemy);
        }
    }

    #endregion

    #region Advancement

    void AdvanceEnemies()
    {
        _totalAdvanceX -= AdvanceDistance;
        foreach (Enemy e in _activeEnemies)
        {
            if (e == null) continue;
            Vector3 p = e.transform.position;
            p.x -= AdvanceDistance;
            e.transform.position = p;
        }
        Debug.Log($"[EnemyManager] Enemies advanced. Total offset: {_totalAdvanceX:F1}");
        GameEvents.EnemiesDropped();
    }

    #endregion

    #region Enemy Removal

    /// <summary>Called by HandleEnemyDied (kill) and Enemy.OnReachedPaddle. Decrements wave counter.</summary>
    public void OnEnemyDied(Enemy enemy) => RemoveEnemy(enemy);

    void RemoveEnemy(Enemy enemy)
    {
        _activeEnemies.Remove(enemy);
        _remainingInWave = Mathf.Max(0, _remainingInWave - 1);
        Debug.Log($"[EnemyManager] Enemy removed. Remaining: {_remainingInWave} (active: {_activeEnemies.Count}, queued: {_spawnQueue.Count})");
    }

    #endregion

    #region Public API

    /// <summary>True when all enemies in the wave have been defeated.</summary>
    public bool AllEnemiesCleared() => _remainingInWave <= 0;

    public int ActiveEnemyCount => _activeEnemies.Count;
    public int QueuedEnemyCount => _spawnQueue.Count;
    public int RemainingInWave  => _remainingInWave;

    #endregion

    #region Helpers

    void ClearAll()
    {
        foreach (Enemy e in _activeEnemies)
            if (e != null) Destroy(e.gameObject);
        _activeEnemies.Clear();
        _spawnQueue.Clear();
        _nextCellIndex   = 0;
        _remainingInWave = 0;
    }

    WaveData GetWaveData(int waveNumber)
    {
        if (Waves != null && Waves.Length > 0)
            return Waves[Mathf.Clamp(waveNumber - 1, 0, Waves.Length - 1)];

        Debug.LogWarning("[EnemyManager] No WaveData assigned — using fallback.");
        return GenerateFallbackWave(waveNumber);
    }

    WaveData GenerateFallbackWave(int waveNumber)
    {
        WaveData fb          = ScriptableObject.CreateInstance<WaveData>();
        fb.Rows              = 3;
        fb.Columns           = 4;
        fb.MaxActiveEnemies  = 5;
        fb.TotalEnemies      = 12;
        fb.BaseHP            = waveNumber + 1;
        fb.WaveHPMultiplier  = 1f + (waveNumber - 1) * 0.2f;
        fb.EmptyCellChance   = 0f;
        fb.EnemyPool         = new EnemySpawnInfo[]
        {
            new EnemySpawnInfo { Prefab = FallbackEnemyPrefab, Weight = 1f, HPMultiplier = 1f, Tier = EnemyTier.Normal }
        };
        return fb;
    }

    #endregion
}

internal struct ResolvedEnemy
{
    public GameObject Prefab;
    public int        HP;
    public int        EssenceReward;
    public int        Col;
    public int        Row;
}
