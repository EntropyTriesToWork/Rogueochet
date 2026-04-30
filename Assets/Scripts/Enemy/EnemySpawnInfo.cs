using UnityEngine;
using Sirenix.OdinInspector;

[System.Serializable]
[CreateAssetMenu(fileName = "RoomData", menuName = "RoguelikePong/Enemy Data")]
public class EnemySpawnInfo : ScriptableObject
{
    [Tooltip("The enemy prefab to spawn.")]
    [Required]
    public GameObject Prefab;
    //[Tooltip("Relative spawn weight (used only when picking randomly from a pool).")]
    //[Min(0)]
    //public float Weight = 1f;
    [Tooltip("HP multiplier relative to wave's BaseHP.")]
    [Min(0.1f)]
    public int BaseHealth = 1;

    [Min(0)]
    public int Damage = 5;

    [Tooltip("Essence reward tier.")]
    public EnemyTier Tier = EnemyTier.Normal;

    [Tooltip("How far enemies move each advance.")]
    public float MoveDistance = 1f;

    // Optional: preview in inspector
    [ShowInInspector, ReadOnly]
    private string PreviewName => Prefab != null ? Prefab.name : "None";
}
public enum EnemyTier { Minion, Normal, Elite, Boss }