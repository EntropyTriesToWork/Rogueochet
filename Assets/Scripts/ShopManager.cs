using UnityEngine;

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void BuyPaddleSpeed(int cost, float speedIncrease = 1.5f)
    {
        if (!GameManager.Instance.SpendEssence(cost)) return;
        PaddleController.Instance.MoveSpeed += speedIncrease;
        Debug.Log($"[Shop] Paddle speed upgraded to {PaddleController.Instance.MoveSpeed}");
    }

    public void BuyBallSpeed(int cost)
    {
        if (!GameManager.Instance.SpendEssence(cost)) return;
        Debug.Log("[Shop] Bought Ball Speed upgrade");
    }

    public void BuyExtraBall(int cost)
    {
        if (!GameManager.Instance.SpendEssence(cost)) return;
        Debug.Log("[Shop] Bought Extra Ball upgrade");
    }

    public void BuyHealOne(int cost)
    {
        if (!GameManager.Instance.SpendEssence(cost)) return;

        int newHP = Mathf.Min(GameManager.Instance.PlayerHealth + 1, GameManager.Instance.MaxHealth);
        Debug.Log($"[Shop] Healed 1 HP. Now at {newHP}/{GameManager.Instance.MaxHealth}");
    }

    public void BuyBallDamage(int cost)
    {
        if (!GameManager.Instance.SpendEssence(cost)) return;

        Debug.Log("[Shop] Bought Ball Damage upgrade");
    }
}