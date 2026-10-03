using UnityEngine;

public abstract class Tower : MonoBehaviour
{
    protected int level = 1;

    protected int totalSpent;

    protected TowerSlot slot;

    private const float SELL_REFUND_RATIO = 0.7f;

    // UI 연동 API (약속문 기준)
    public abstract TowerData Data { get; }
    public string DisplayName => Data.towerName;
    public int Level => level;
    public int MaxLevel => Data.MaxLevel;

    public bool CanUpgrade => level < MaxLevel && UpgradeCost <= GameManager.Instance.Gold;     // TODO: level < MaxLevel 이고 골드가 충분한지
    public int UpgradeCost => level < MaxLevel? Data.costs[level] : 0;         // TODO: Data.costs[level] (다음 레벨 비용)
    public int SellPrice => Mathf.FloorToInt(totalSpent*SELL_REFUND_RATIO);        // TODO: totalSpent * 환급 비율

    public virtual bool HasRallyPoint => false;

    public void Init(TowerSlot slot)
    {
        this.slot = slot;
        totalSpent = Data.costs[0];
    }

    public void Upgrade()
    {
        // TODO: CanUpgrade 확인 → GameManager.Instance.TrySpendGold(UpgradeCost)
        //       → level++, totalSpent += 비용 → OnLevelChanged()
        if (CanUpgrade && GameManager.Instance.TrySpendGold(UpgradeCost))
        {
            totalSpent += UpgradeCost;
            level++;
            OnLevelChanged();
        }
    }

    public void Sell()
    {
        GameManager.Instance.AddGold(SellPrice);
        slot.ClearOccupant();
        Destroy(gameObject);
    }

    public virtual void BeginRallyPoint()
    {
    }

    // 레벨이 바뀐 직후 호출 (타워별로 스탯 갱신)
    protected virtual void OnLevelChanged()
    {
    }
}
