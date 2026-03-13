using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance { get; private set; }

    [Header("Fallback")]
    [Tooltip("Used only by the procedural fallback wave when no WaveData assets are assigned.")]
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
    [Tooltip("How far enemies move LEFT each time they advance (one round ends with enemies alive).")]
    public float AdvanceDistance = 0.8f;

    [Header("Wave Data")]
    public WaveData[] Waves;

    private List<Enemy> _activeEnemies = new List<Enemy>();
    private Queue<ResolvedEnemy> _spawnQueue = new Queue<ResolvedEnemy>();

    private int _maxActive = 0;
    private int _nextCellIndex = 0;
    private float _totalAdvanceX = 0f;

    private int _remainingInWave = 0;

    private float _gridTopY;
    private int _waveRows;
    private int _waveColumns;
    private WaveData _currentWaveData;   // kept so AddEnemiesToWave can use same HP settings

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start() => SubscribeToEvents();
    void OnDestroy() => UnsubscribeFromEvents();

    public void SubscribeToEvents()
    {
        GameEvents.OnWaveStarted += HandleWaveStarted;
        GameEvents.OnRoundEnded += HandleRoundEnded;
        GameEvents.OnGameOver += HandleGameOver;
        GameEvents.OnVictory += HandleGameOver;
        GameEvents.OnEnemyDied += HandleEnemyDiedForQueue;
    }

    void UnsubscribeFromEvents()
    {
        GameEvents.OnWaveStarted -= HandleWaveStarted;
        GameEvents.OnRoundEnded -= HandleRoundEnded;
        GameEvents.OnGameOver -= HandleGameOver;
        GameEvents.OnVictory -= HandleGameOver;
        GameEvents.OnEnemyDied -= HandleEnemyDiedForQueue;
    }

    void HandleWaveStarted(int waveNumber) => SpawnWave(waveNumber);

    void HandleRoundEnded()
    {
        if (_activeEnemies.Count > 0)
            AdvanceEnemies();
    }

    void HandleGameOver() => ClearAll();

    void HandleEnemyDiedForQueue(Enemy enemy, int _)
    {
        OnEnemyDied(enemy);
        TrySpawnFromQueue();
    }

    void SpawnWave(int waveNumber)
    {
        ClearAll();
        _totalAdvanceX = 0f;

        WaveData data = GetWaveData(waveNumber);
        _currentWaveData = data;
        _maxActive = data.MaxActiveEnemies;
        _waveRows = data.Rows;
        _waveColumns = data.Columns;

        float gridHeight = (_waveRows - 1) * SpacingY;
        _gridTopY = StartY + gridHeight * 0.5f;

        int totalCells = _waveRows * _waveColumns;
        int spawnTarget = Mathf.Min(data.ResolvedSpawnCount, totalCells);

        List<int> cellOrder = BuildShuffledCells(totalCells, data.EmptyCellChance, spawnTarget);

        _spawnQueue.Clear();
        _nextCellIndex = 0;

        foreach (int cellIndex in cellOrder)
        {
            EnemySpawnInfo entry = data.PickRandom();
            if (entry == null)
            {
                Debug.LogWarning("[EnemyManager] PickRandom returned null — check pool weights/prefabs.");
                continue;
            }

            int col = cellIndex % _waveColumns;
            int row = cellIndex / _waveColumns;

            _spawnQueue.Enqueue(new ResolvedEnemy
            {
                Prefab = entry.Prefab,
                HP = data.ComputeHP(entry),
                EssenceReward = WaveData.RollEssenceReward(entry.Tier),
                Col = col,
                Row = row,
            });
        }
        _remainingInWave = _spawnQueue.Count;

        int initialCount = (_maxActive > 0) ? _maxActive : _remainingInWave;
        for (int i = 0; i < initialCount && _spawnQueue.Count > 0; i++)
            SpawnNextFromQueue();

        Debug.Log($"[EnemyManager] Wave {waveNumber}: {_remainingInWave} total enemies " +
                  $"({_activeEnemies.Count} active, {_spawnQueue.Count} queued) " +
                  $"| BaseHP={data.BaseHP} WaveMult={data.WaveHPMultiplier:F2}");

        GameManager.Instance.SetWaveEnemyCount(_remainingInWave);
    }
    List<int> BuildShuffledCells(int totalCells, float emptyCellChance, int spawnTarget)
    {
        // Build full index list
        List<int> allCells = new List<int>(totalCells);
        for (int i = 0; i < totalCells; i++)
            allCells.Add(i);

        // Fisher-Yates shuffle
        for (int i = allCells.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (allCells[i], allCells[j]) = (allCells[j], allCells[i]);
        }

        // Apply empty-cell chance and cap to spawnTarget
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

    void TrySpawnFromQueue()
    {
        if (_spawnQueue.Count == 0) return;
        bool underCap = _maxActive <= 0 || _activeEnemies.Count < _maxActive;
        if (underCap) SpawnNextFromQueue();
    }

    void SpawnNextFromQueue()
    {
        if (_spawnQueue.Count == 0) return;

        ResolvedEnemy resolved = _spawnQueue.Dequeue();

        Vector3 pos = new Vector3(
            StartX - resolved.Col * SpacingX + _totalAdvanceX,
            _gridTopY - resolved.Row * SpacingY,
            0f
        );

        GameObject go = Instantiate(resolved.Prefab, pos, Quaternion.identity);
        Enemy enemy = go.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.Initialize(resolved.HP, resolved.EssenceReward);
            _activeEnemies.Add(enemy);
        }
    }
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
    public void OnEnemyDied(Enemy enemy)
    {
        _activeEnemies.Remove(enemy);
        _remainingInWave = Mathf.Max(0, _remainingInWave - 1);
        Debug.Log($"[EnemyManager] Enemy removed. Remaining: {_remainingInWave} " +
                  $"(active: {_activeEnemies.Count}, queued: {_spawnQueue.Count})");
    }
    public bool AllEnemiesCleared() => _remainingInWave <= 0;

    public int ActiveEnemyCount => _activeEnemies.Count;
    public int QueuedEnemyCount => _spawnQueue.Count;
    public int RemainingInWave => _remainingInWave;

    public void AddEnemiesToWave(int count, WaveData overrideData = null)
    {
        if (count <= 0) return;

        WaveData data = overrideData != null ? overrideData : _currentWaveData;
        if (data == null)
        {
            Debug.LogWarning("[EnemyManager] AddEnemiesToWave called but no active WaveData.");
            return;
        }

        int added = 0;
        for (int i = 0; i < count; i++)
        {
            EnemySpawnInfo entry = data.PickRandom();
            if (entry == null) continue;

            // Wrap cell placement around the grid dimensions
            int cellIndex = (_nextCellIndex + _spawnQueue.Count + i) % (_waveRows * _waveColumns);
            int col = cellIndex % _waveColumns;
            int row = cellIndex / _waveColumns;

            _spawnQueue.Enqueue(new ResolvedEnemy
            {
                Prefab = entry.Prefab,
                HP = data.ComputeHP(entry),
                EssenceReward = WaveData.RollEssenceReward(entry.Tier),
                Col = col,
                Row = row,
            });
            added++;
        }

        _remainingInWave += added;
        Debug.Log($"[EnemyManager] Injected {added} enemies mid-wave. " +
                  $"Remaining: {_remainingInWave}");
        TrySpawnFromQueue();
    }
    void ClearAll()
    {
        foreach (Enemy e in _activeEnemies)
            if (e != null) Destroy(e.gameObject);
        _activeEnemies.Clear();
        _spawnQueue.Clear();
        _nextCellIndex = 0;
        _remainingInWave = 0;
    }

    WaveData GetWaveData(int waveNumber)
    {
        if (Waves != null && Waves.Length > 0)
        {
            int index = Mathf.Clamp(waveNumber - 1, 0, Waves.Length - 1);
            return Waves[index];
        }
        Debug.LogWarning("[EnemyManager] No WaveData assigned — using fallback.");
        return GenerateFallbackWave(waveNumber);
    }

    WaveData GenerateFallbackWave(int waveNumber)
    {
        WaveData fb = ScriptableObject.CreateInstance<WaveData>();
        fb.Rows = 3;
        fb.Columns = 4;
        fb.MaxActiveEnemies = 5;
        fb.MaxSpawnedEnemies = 12;
        fb.BaseHP = waveNumber + 1;
        fb.WaveHPMultiplier = 1f + (waveNumber - 1) * 0.2f;   // +20% per wave
        fb.EmptyCellChance = 0f;
        fb.EnemyPool = new EnemySpawnInfo[]
        {
            new EnemySpawnInfo
            {
                Prefab        = FallbackEnemyPrefab,
                Weight        = 1f,
                HPMultiplier  = 1f,
                Tier          = EnemyTier.Normal,
            }
        };
        return fb;
    }
}
internal struct ResolvedEnemy
{
    public GameObject Prefab;
    public int HP;
    public int EssenceReward;
    public int Col;
    public int Row;
}