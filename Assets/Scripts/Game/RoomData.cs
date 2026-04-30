using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "RoomData", menuName = "RoguelikePong/Room Data")]
public class RoomData : ScriptableObject
{
    public RoomType Type;

    [ShowIf("Type", RoomType.Combat)] public List<WaveData> Waves;
    [ShowIf("Type", RoomType.Combat)] public int BaseHP = 3;
    [ShowIf("Type", RoomType.Combat)] public float HealthMultiplier = 1f;
    [Tooltip("Seconds between enemy advances (moving left).")]
    [ShowIf("Type", RoomType.Combat)] public float AdvanceInterval = 4f;

    [ShowIf("Type", RoomType.Shop)] public int ShopCardCount = 3;
}