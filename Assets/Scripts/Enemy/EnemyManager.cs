using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance { get; private set; }

    [Header("Fallback")]
    public GameObject FallbackEnemyPrefab;

    private List<Enemy> _activeEnemies = new List<Enemy>();
    private Queue<(Vector2 position, EnemySpawnInfo enemy)> _spawnQueue = new Queue<(Vector2, EnemySpawnInfo)>();
    private RoomData _roomData;
    private Coroutine _spawnCoroutine;
    private int _remainingInWave = 0;
    private int _wavesCompletedInRoom = 0;

    public int CurrentWaveNumber => _wavesCompletedInRoom + 1;
    public float TotalWavesInRoom => _roomData.Waves.Count;
    public WaveData CurrentWave => _roomData.Waves[CurrentWaveNumber - 1]; // zero‑based index
    public float NextWaveDelay => _nextWaveDelay;
    float _nextWaveDelay = 1f;

    void Awake() => Instance = this;
    void Start() => SubscribeToEvents();
    void OnDestroy() => UnsubscribeFromEvents();

    #region Events
    void SubscribeToEvents()
    {
        GameEvents.OnRoomEntered += HandleEnterCombatRoom;
        GameEvents.OnWaveStarted += HandleWaveStarted;
        GameEvents.OnRoomCleared += HandleRoomCleared;
        GameEvents.OnGameOver += HandleGameOver;
        GameEvents.OnVictory += HandleGameOver;
        GameEvents.OnEnemyDied += HandleEnemyDied;
    }

    void UnsubscribeFromEvents()
    {
        GameEvents.OnRoomEntered -= HandleEnterCombatRoom;
        GameEvents.OnWaveStarted -= HandleWaveStarted;
        GameEvents.OnRoomCleared -= HandleRoomCleared;
        GameEvents.OnGameOver -= HandleGameOver;
        GameEvents.OnVictory -= HandleGameOver;
        GameEvents.OnEnemyDied -= HandleEnemyDied;
    }

    void HandleEnterCombatRoom(RoomData roomData) => StartRoom(roomData);
    void HandleWaveStarted(int waveNumber) => StartWave(waveNumber);
    void HandleRoomCleared() => _wavesCompletedInRoom = 0;
    void HandleGameOver() => ClearAll();

    void HandleEnemyDied(Enemy enemy, int _)
    {
        _activeEnemies.Remove(enemy);
        _remainingInWave--;

        if (_remainingInWave <= 0 && _spawnQueue.Count == 0)
            OnWaveCleared();
    }

    void OnWaveCleared()
    {
        _wavesCompletedInRoom++;
        GameEvents.WaveCleared();
        if (_wavesCompletedInRoom >= TotalWavesInRoom)
        {
            GameEvents.RoomCleared();
        }
        else
        {
            StartCoroutine(StartNextWaveAfterDelay(1f));
        }
    }
    IEnumerator StartNextWaveAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        GameEvents.WaveStarted(_wavesCompletedInRoom + 1);
    }
    #endregion

    #region Wave Management
    public void StartRoom(RoomData roomData)
    {
        _roomData = roomData;
        _wavesCompletedInRoom = 0;
        StartWave(0);
    }
    public void StartWave(int waveIndex)
    {
        if (_roomData == null || waveIndex >= _roomData.Waves.Count)
        {
            GameEvents.RoomCleared();
            return;
        }
        WaveData wave = _roomData.Waves[waveIndex];
        var entries = wave.GetAllSpawnEntries(); // returns List<(Vector2, EnemySpawnInfo)>
        foreach (var entry in entries)
            _spawnQueue.Enqueue(entry);

        _remainingInWave = _spawnQueue.Count;
        SpawnAllEnemies();
    }
    private void SpawnAllEnemies()
    {
        while (_spawnQueue.Count > 0)
        {
            var (pos, spawnInfo) = _spawnQueue.Dequeue();
            SpawnEnemy(pos, spawnInfo);
        }
    }
    private void SpawnEnemy(Vector2 position, EnemySpawnInfo spawnInfo)
    {
        GameObject go = Instantiate(spawnInfo.Prefab, position, Quaternion.identity);
        Enemy enemy = go.GetComponent<Enemy>();
        if (enemy != null)
        {
            int hp = ComputeHP(spawnInfo);
            int essence = WaveData.RollEssenceReward(spawnInfo.Tier);
            enemy.Initialize(hp, essence, spawnInfo.Damage, spawnInfo.MoveDistance, CurrentWaveNumber);
            _activeEnemies.Add(enemy); //Enemies will be in charge of their own movements (they will be staggered and won't move synchronously) 
        }
    }

    private int ComputeHP(EnemySpawnInfo spawnInfo)
    {
        // Use enemy's own BaseHealth multiplied by room's health multiplier
        float raw = spawnInfo.BaseHealth * _roomData.HealthMultiplier;
        return Mathf.Max(1, Mathf.RoundToInt(raw));
    }
    #endregion

    #region Fallback & Cleanup
    private WaveData GenerateFallbackWave(int waveNumber)
    {
        WaveData fb = ScriptableObject.CreateInstance<WaveData>();
        fb.Grid = new EnemySpawnInfo[3, 7];
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
        fb.InitialSpawnDelay = 0f;
        fb.GridStart = new Vector2(5f, 0f);
        fb.GridSpacing = new Vector2(1.4f, 1.2f);
        return fb;
    }

    private void ClearAll()
    {
        foreach (Enemy e in _activeEnemies)
            if (e != null) Destroy(e.gameObject);
        _activeEnemies.Clear();
        _spawnQueue.Clear();
        _remainingInWave = 0;
        if (_spawnCoroutine != null) StopCoroutine(_spawnCoroutine);
    }
    #endregion

    #region Public API
    public bool AllEnemiesCleared() => _remainingInWave <= 0 && _spawnQueue.Count == 0;
    public int ActiveEnemyCount => _activeEnemies.Count;
    public int RemainingInWave => _remainingInWave;
    #endregion
}