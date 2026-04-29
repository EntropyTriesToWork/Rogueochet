using UnityEngine;

public class BallData : ScriptableObject
{
    [Header("Main Stats")]
    public int health;
    public float speed;
    public int durability;

    public GameObject ballPrefab;
}
