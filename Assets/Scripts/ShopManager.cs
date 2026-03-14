using System.Collections.Generic;
using UnityEngine;

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance { get; private set; }

    [Header("Upgrade Pool")]
    [Tooltip("All BallDirect upgrades available to appear in the shop.")]
    public UpgradeData[] BallDirectPool;
    [Tooltip("All Global upgrades available to appear in the shop.")]
    public UpgradeData[] GlobalPool;
    [Tooltip("All ball upgrades (NewBall category) available.")]
    public UpgradeData[] NewBallPool;

    [Header("Shop Settings")]
    [Tooltip("Total card slots shown each visit.")]
    public int CardCount = 3;
    [Tooltip("Weight for BallDirect cards (relative).")]
    public float WeightBallDirect = 60f;
    [Tooltip("Weight for Global cards (relative).")]
    public float WeightGlobal = 30f;
    [Tooltip("Weight for NewBall cards (relative).")]
    public float WeightNewBall = 10f;

    public IReadOnlyList<ShopOffering> CurrentOfferings => _offerings;
    private List<ShopOffering> _offerings = new List<ShopOffering>();
    private int _targetBallSlot = 0;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        GameEvents.OnShopClosed += ClearOfferings;
    }

    void OnDestroy()
    {
        GameEvents.OnShopClosed -= ClearOfferings;
    }
    public void GenerateOfferings()
    {
        _offerings.Clear();

        var inv = PlayerInventory.Instance;
        _targetBallSlot = (inv != null && inv.UsedBallSlots > 0)
            ? Random.Range(0, inv.UsedBallSlots)
            : 0;

        bool hasFreeSlot = inv != null && inv.CanAddBall();
        bool newBallUsed = false;

        for (int i = 0; i < CardCount; i++)
        {
            UpgradeCategory category = PickCategory(hasFreeSlot, newBallUsed);
            UpgradeData data = PickFromPool(category);

            if (data == null) continue;

            if (category == UpgradeCategory.NewBall) newBallUsed = true;

            _offerings.Add(new ShopOffering
            {
                Upgrade = data,
                Category = category,
                TargetBallSlot = (category == UpgradeCategory.BallDirect) ? _targetBallSlot : -1,
            });
        }

        Debug.Log($"[ShopManager] Generated {_offerings.Count} offerings for shop.");
        GameEvents.ShopOfferingsChanged();
    }

    UpgradeCategory PickCategory(bool hasFreeSlot, bool newBallAlreadyOffered)
    {
        float wDirect = (BallDirectPool != null && BallDirectPool.Length > 0) ? WeightBallDirect : 0f;
        float wGlobal = (GlobalPool != null && GlobalPool.Length > 0) ? WeightGlobal : 0f;
        float wBall = (!newBallAlreadyOffered && hasFreeSlot && NewBallPool != null && NewBallPool.Length > 0)
                        ? WeightNewBall : 0f;

        float total = wDirect + wGlobal + wBall;
        if (total <= 0f) return UpgradeCategory.Global;

        float roll = Random.Range(0f, total);
        if (roll < wDirect) return UpgradeCategory.BallDirect;
        if (roll < wDirect + wGlobal) return UpgradeCategory.Global;
        return UpgradeCategory.NewBall;
    }

    UpgradeData PickFromPool(UpgradeCategory category)
    {
        UpgradeData[] pool = category switch
        {
            UpgradeCategory.BallDirect => BallDirectPool,
            UpgradeCategory.Global => GlobalPool,
            UpgradeCategory.NewBall => NewBallPool,
            _ => null
        };

        if (pool == null || pool.Length == 0) return null;
        return pool[Random.Range(0, pool.Length)];
    }

    void ClearOfferings() => _offerings.Clear();

    public bool PurchaseOffering(int index, int ballSlotOverride = -1)
    {
        if (index < 0 || index >= _offerings.Count) return false;

        ShopOffering offering = _offerings[index];

        var inv = PlayerInventory.Instance;
        if (inv == null)
        {
            Debug.LogWarning("[ShopManager] PlayerInventory.Instance is null — cannot purchase.");
            return false;
        }

        if (!GameManager.Instance.SpendEssence(offering.Upgrade.Cost))
        {
            Debug.Log("[ShopManager] Not enough essence.");
            return false;
        }

        switch (offering.Category)
        {
            case UpgradeCategory.BallDirect:
                int slot = ballSlotOverride >= 0 ? ballSlotOverride : offering.TargetBallSlot;
                inv.ApplyDirectUpgrade(offering.Upgrade, slot);
                break;

            case UpgradeCategory.Global:
                inv.ApplyGlobalUpgrade(offering.Upgrade);
                break;

            case UpgradeCategory.NewBall:
                if (!inv.AddBall(offering.Upgrade))
                {
                    GameManager.Instance.AddEssence(offering.Upgrade.Cost);
                    return false;
                }
                break;
        }

        _offerings.RemoveAt(index);
        GameEvents.ShopOfferingsChanged();

        Debug.Log($"[ShopManager] Purchased '{offering.Upgrade.UpgradeName}'.");
        return true;
    }
    public void BuyPaddleSpeed(int cost, float speedIncrease = 1.5f)
    {
        if (!GameManager.Instance.SpendEssence(cost)) return;
        PaddleController.Instance.MoveSpeed += speedIncrease;
    }

    public void BuyHealOne(int cost)
    {
        if (!GameManager.Instance.SpendEssence(cost)) return;
        GameManager.Instance.TakeDamage(-1);   // negative damage = heal
    }
}
[System.Serializable]
public class ShopOffering
{
    public UpgradeData Upgrade;
    public UpgradeCategory Category;
    public int TargetBallSlot;   // -1 for Global / NewBall
}