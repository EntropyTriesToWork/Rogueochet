using System.Collections.Generic;
using UnityEngine;

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance { get; private set; }

    #region Inspector

    [Header("Upgrade Pools")]
    public UpgradeData[] BallDirectPool;
    public UpgradeData[] GlobalPool;
    public UpgradeData[] NewBallPool;

    [Header("Shop Settings")]
    public int CardCount = 3;
    [Tooltip("Weight for BallDirect cards (relative).")]
    public float WeightBallDirect = 60f;
    [Tooltip("Weight for Global cards (relative).")]
    public float WeightGlobal = 30f;
    [Tooltip("Weight for NewBall cards (relative). At most one NewBall card appears per visit.")]
    public float WeightNewBall = 10f;

    [Header("Reroll")]
    [Tooltip("Essence cost of the first reroll each wave.")]
    public int RerollBaseCost = 1;
    [Tooltip("Added to the reroll cost after each reroll this wave.")]
    public int RerollCostIncrease = 1;

    #endregion

    #region State

    public IReadOnlyList<ShopOffering> CurrentOfferings => _offerings;
    private List<ShopOffering> _offerings = new List<ShopOffering>();

    public int RerollCost { get; private set; }
    public int RerollCount { get; private set; }

    #endregion

    #region Lifecycle

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        RerollCost = RerollBaseCost;
        GameEvents.OnShopClosed += ClearOfferings;
        GameEvents.OnWaveCleared += ResetReroll;
    }

    void OnDestroy()
    {
        GameEvents.OnShopClosed -= ClearOfferings;
        GameEvents.OnWaveCleared -= ResetReroll;
    }

    #endregion

    #region Generation
    public void GenerateOfferings()
    {
        _offerings.Clear();

        var inv = PlayerInventory.Instance;
        if (inv == null)
        {
            Debug.LogWarning("[ShopManager] PlayerInventory not found — shop will be empty.");
            GameEvents.ShopOfferingsChanged();
            return;
        }

        // Inline slot check — never rely on CanAddBall() which could have edge cases.
        bool hasFreeSlot = inv.UsedBallSlots < inv.MaxBallSlots;
        bool newBallUsed = false;

        Debug.Log($"[ShopManager] Generating: UsedSlots={inv.UsedBallSlots} MaxSlots={inv.MaxBallSlots} hasFreeSlot={hasFreeSlot}");

        for (int i = 0; i < CardCount; i++)
        {
            UpgradeCategory category = PickCategory(hasFreeSlot, newBallUsed);
            UpgradeData data = PickFromPool(category);

            if (data == null) continue;

            if (category == UpgradeCategory.NewBall) newBallUsed = true;

            int targetSlot = (category == UpgradeCategory.BallDirect && inv.UsedBallSlots > 0)
                ? Random.Range(0, inv.UsedBallSlots)
                : -1;

            _offerings.Add(new ShopOffering { Upgrade = data, Category = category, TargetBallSlot = targetSlot });
        }

        Debug.Log($"[ShopManager] Generated {_offerings.Count} offerings.");
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

    #endregion

    #region Reroll
    public bool Reroll()
    {
        if (!GameManager.Instance.SpendEssence(RerollCost))
        {
            Debug.Log("[ShopManager] Not enough essence to reroll.");
            return false;
        }

        RerollCount++;
        RerollCost = RerollBaseCost + RerollCount * RerollCostIncrease;
        GenerateOfferings();
        return true;
    }

    void ResetReroll()
    {
        RerollCount = 0;
        RerollCost = RerollBaseCost;
    }

    #endregion

    #region Purchasing

    public bool PurchaseOffering(int index)
    {
        if (index < 0 || index >= _offerings.Count) return false;

        ShopOffering offering = _offerings[index];

        var inv = PlayerInventory.Instance;
        if (inv == null)
        {
            Debug.LogWarning("[ShopManager] PlayerInventory.Instance is null.");
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
                int slot = offering.TargetBallSlot >= 0 ? offering.TargetBallSlot
                         : (inv.UsedBallSlots > 0 ? Random.Range(0, inv.UsedBallSlots) : 0);
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

    #endregion
}

[System.Serializable]
public class ShopOffering
{
    public UpgradeData Upgrade;
    public UpgradeCategory Category;
    public int TargetBallSlot = -1;
}