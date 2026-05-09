using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance { get; private set; }

    public GameObject FallbackEnemyPrefab;
    public Vector2 SpawnDistanceFromZero = new Vector2(18f, 0f);

    private List<Enemy> _activeEnemies = new List<Enemy>();
    private RoomData _roomData;
    private Coroutine _waveSpawnCoroutine;

    // Wave tracking
    private float _roomStartTime;
    private List<WaveSpawnTask> _waveSchedule;
    private int[] _waveTotalCount;
    private int[] _waveAliveCount;
    private bool[] _waveSpawned;
    private bool _allWavesSpawned = false;

    private struct WaveSpawnTask
    {
        public int WaveIndex;
        public float SpawnTime; // absolute time (Time.time) when to spawn
    }

    void Awake() => Instance = this;
    void Start() => SubscribeToEvents();
    void OnDestroy() => UnsubscribeFromEvents();
    private void Update() { foreach(Enemy enemy in _activeEnemies) { if(enemy != null) enemy.Tick(Time.deltaTime); } }

    #region Events
    void SubscribeToEvents()
    {
        GameEvents.OnRoomEntered += HandleEnterCombatRoom;
        GameEvents.OnRoomCleared += HandleRoomCleared;
        GameEvents.OnGameOver += HandleGameOver;
        GameEvents.OnVictory += HandleGameOver;
        GameEvents.OnEnemyDied += HandleEnemyDied;
    }

    void UnsubscribeFromEvents()
    {
        GameEvents.OnRoomEntered -= HandleEnterCombatRoom;
        GameEvents.OnRoomCleared -= HandleRoomCleared;
        GameEvents.OnGameOver -= HandleGameOver;
        GameEvents.OnVictory -= HandleGameOver;
        GameEvents.OnEnemyDied -= HandleEnemyDied;
    }

    void HandleEnterCombatRoom(RoomData roomData) => StartRoom(roomData);
    void HandleRoomCleared() => ClearAll();
    void HandleGameOver() => ClearAll();

    void HandleEnemyDied(Enemy enemy, int _)
    {
        int waveIdx = enemy.WaveIndex;
        _activeEnemies.Remove(enemy);
        if (waveIdx >= 0 && waveIdx < _waveAliveCount.Length)
        {
            _waveAliveCount[waveIdx]--;
            if (_waveAliveCount[waveIdx] <= 0)
                OnWaveCleared(waveIdx);
        }
        CheckRoomCompletion();
    }
    #endregion

    #region Wave Management
    public void StartRoom(RoomData roomData)
    {
        _roomData = roomData;
        _roomStartTime = Time.time;
        _allWavesSpawned = false;

        int waveCount = _roomData.Waves.Count;
        _waveTotalCount = new int[waveCount];
        _waveAliveCount = new int[waveCount];
        _waveSpawned = new bool[waveCount];

        BuildWaveSchedule();
        if (_waveSpawnCoroutine != null) StopCoroutine(_waveSpawnCoroutine);
        _waveSpawnCoroutine = StartCoroutine(SpawnWavesByTimer());
    }

    private void BuildWaveSchedule()
    {
        _waveSchedule = new List<WaveSpawnTask>();
        float cumulativeTime = _roomStartTime;
        for (int i = 0; i < _roomData.Waves.Count; i++)
        {
            float delay = _roomData.Waves[i].SpawnDelay;
            cumulativeTime += delay;
            _waveSchedule.Add(new WaveSpawnTask
            {
                WaveIndex = i,
                SpawnTime = cumulativeTime
            });

            // Pre‑calculate total enemies per wave
            _waveTotalCount[i] = _roomData.Waves[i].GetAllSpawnEntries().Count;
            _waveAliveCount[i] = 0;
            _waveSpawned[i] = false;
        }
    }

    private IEnumerator SpawnWavesByTimer()
    {
        int nextWaveIdx = 0;
        while (nextWaveIdx < _waveSchedule.Count)
        {
            float waitTime = _waveSchedule[nextWaveIdx].SpawnTime - Time.time;
            if (waitTime > 0)
                yield return new WaitForSeconds(waitTime);

            SpawnWave(_waveSchedule[nextWaveIdx].WaveIndex);
            nextWaveIdx++;

            if (nextWaveIdx >= _waveSchedule.Count)
            {
                _allWavesSpawned = true;
                CheckRoomCompletion();
            }
        }
    }

    private void SpawnWave(int waveIndex)
    {
        if (_waveSpawned[waveIndex]) return;
        _waveSpawned[waveIndex] = true;

        WaveData wave = _roomData.Waves[waveIndex];
        var entries = wave.GetAllSpawnEntries();

        _waveAliveCount[waveIndex] = entries.Count;

        foreach (var (pos, spawnInfo) in entries)
            SpawnEnemy(pos, spawnInfo, waveIndex);
    }

    private void SpawnEnemy(Vector2 position, EnemySpawnInfo spawnInfo, int waveIndex)
    {
        GameObject go = Instantiate(spawnInfo.Prefab, position + SpawnDistanceFromZero, Quaternion.identity);
        Enemy enemy = go.GetComponent<Enemy>();
        if (enemy != null)
        {
            int hp = ComputeHP(spawnInfo);
            int essence = WaveData.RollEssenceReward(spawnInfo.Tier);
            enemy.Initialize(hp, essence, spawnInfo.Damage, spawnInfo.MoveDistance, spawnInfo.MoveDelay, waveIndex);
            _activeEnemies.Add(enemy);
        }
    }

    private void OnWaveCleared(int waveIndex)
    {
        Debug.Log($"Wave {waveIndex + 1} cleared");
        GameEvents.WaveCleared();
    }

    private void CheckRoomCompletion()
    {
        if (!_allWavesSpawned) return;

        for (int i = 0; i < _waveAliveCount.Length; i++)
            if (_waveAliveCount[i] > 0) return;

        GameEvents.RoomCleared();
        ClearAll();
    }

    private int ComputeHP(EnemySpawnInfo spawnInfo)
    {
        float raw = spawnInfo.BaseHealth * _roomData.HealthMultiplier;
        return Mathf.Max(1, Mathf.RoundToInt(raw));
    }
    #endregion

    #region Fallback & Cleanup
    private WaveData GenerateFallbackWave(int waveNumber)
    {
        WaveData fb = ScriptableObject.CreateInstance<WaveData>();
        fb.SpawnDelay = 3f;
        fb.GridStart = new Vector2(5f, 0f);
        fb.GridSpacing = new Vector2(1.4f, 1.2f);
        fb.Grid = new EnemySpawnInfo[1, 3];
        for (int i = 0; i < 3; i++)
        {
            fb.Grid[0, i] = new EnemySpawnInfo
            {
                Prefab = FallbackEnemyPrefab,
                BaseHealth = 5,
                Tier = EnemyTier.Normal,
                Damage = 1,
                MoveDistance = 1f,
            };
        }
        return fb;
    }
    private void ClearAll()
    {
        foreach (Enemy e in _activeEnemies)
            if (e != null) Destroy(e.gameObject);
        _activeEnemies.Clear();
        if (_waveSpawnCoroutine != null) StopCoroutine(_waveSpawnCoroutine);
        _waveSpawnCoroutine = null;
        _allWavesSpawned = false;
    }
    #endregion

    #region Public API
    public bool AllEnemiesCleared()
    {
        if (!_allWavesSpawned) return false;
        for (int i = 0; i < _waveAliveCount.Length; i++)
            if (_waveAliveCount[i] > 0) return false;
        return true;
    }

    public int GetEnemiesLeftToSpawn()
    {
        int total = 0;
        for (int i = 0; i < _waveSpawned.Length; i++)
            if (!_waveSpawned[i])
                total += _waveTotalCount[i];
        return total;
    }

    public int GetEnemiesAlive() => _activeEnemies.Count;

    public int GetEnemiesKilled()
    {
        int totalAcrossAllWaves = 0;
        for (int i = 0; i < _waveTotalCount.Length; i++)
            totalAcrossAllWaves += _waveTotalCount[i];
        return totalAcrossAllWaves - GetEnemiesAlive() - GetEnemiesLeftToSpawn();
    }

    public int ActiveEnemyCount => _activeEnemies.Count;
    #endregion
}