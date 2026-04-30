using System.Collections.Generic;
using UnityEngine;

public class PathManager : MonoBehaviour
{
    public static PathManager Instance { get; private set; }

    [Header("Room Pools")]
    public List<RoomData> CombatRooms;
    public List<RoomData> ShopRooms;
    public List<RoomData> EventRooms;   // future use

    [Header("Path Generation")]
    public int TotalRooms = 8;
    [Range(0f, 1f)] public float CombatChance = 0.7f;
    [Range(0f, 1f)] public float ShopChance = 0.2f;
    [Range(0f, 1f)] public float EventChance = 0.1f;

    [Header("Forced Rooms")]
    public int[] ForcedShopIndexes = new int[] { 2, 5 };
    public int[] ForcedEventIndexes = new int[] { };

    private List<RoomData> _rooms = new List<RoomData>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        GameEvents.OnGameStarted += GeneratePath;
    }
    private void OnDestroy()
    {
        GameEvents.OnGameStarted -= GeneratePath;
    }

    public void GeneratePath()
    {
        _rooms.Clear();

        for (int i = 0; i < TotalRooms; i++)
        {
            RoomType type = DetermineRoomType(i);
            RoomData room = GetRandomRoomOfType(type);
            if (room == null)
            {
                Debug.LogWarning($"[PathController] No RoomData of type {type} available. Using fallback combat room.");
                room = CreateFallbackRoom();
            }
            _rooms.Add(room);
        }

        Debug.Log($"[PathController] Generated {_rooms.Count} rooms.");
    }

    private RoomType DetermineRoomType(int index)
    {
        if (System.Array.IndexOf(ForcedShopIndexes, index) >= 0)
            return RoomType.Shop;
        if (System.Array.IndexOf(ForcedEventIndexes, index) >= 0)
            return RoomType.Event;

        float roll = Random.value;
        if (roll < CombatChance)
            return RoomType.Combat;
        else if (roll < CombatChance + ShopChance)
            return RoomType.Shop;
        else
            return RoomType.Event;
    }

    private RoomData GetRandomRoomOfType(RoomType type)
    {
        List<RoomData> pool = type switch
        {
            RoomType.Combat => CombatRooms,
            RoomType.Shop => ShopRooms,
            RoomType.Event => EventRooms,
            _ => null
        };

        if (pool == null || pool.Count == 0) return null;
        return pool[Random.Range(0, pool.Count)];
    }

    private RoomData CreateFallbackRoom()
    {
        RoomData fallback = ScriptableObject.CreateInstance<RoomData>();
        fallback.Type = RoomType.Combat;
        fallback.Waves = new List<WaveData>(); // you'd need a fallback WaveData too
        fallback.BaseHP = 3;
        fallback.HealthMultiplier = 1f;
        return fallback;
    }

    public RoomData GetRoomData(int roomIndex)
    {
        if (roomIndex < 0 || roomIndex >= _rooms.Count) return null;
        return _rooms[roomIndex];
    }

    public int GetWaveCountForRoom(int roomIndex)
    {
        var room = GetRoomData(roomIndex);
        return (room != null && room.Type == RoomType.Combat) ? room.Waves.Count : 0;
    }

    public List<RoomData> GetAllRooms() => _rooms;

    public void ResetAndGenerate() => GeneratePath();
}
public enum RoomType
{
    Combat,
    Shop,
    Event
}