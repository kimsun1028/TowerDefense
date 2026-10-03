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

    protected Animator animator;   // Visual 자식의 Animator (없으면 null)

    protected virtual void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        ApplyLevelAnimator();
    }

    public void Init(TowerSlot slot)
    {
        this.slot = slot;
        totalSpent = Data.costs[0];
    }

    // 현재 레벨의 외형으로 교체
    private void ApplyLevelAnimator()
    {
        RuntimeAnimatorController[] list = Data.animators;
        if (animator == null || list == null || level - 1 >= list.Length || list[level - 1] == null) return;
        animator.runtimeAnimatorController = list[level - 1];
    }

    // 파라미터가 있을 때만 트리거 (레벨마다 컨트롤러 구성이 달라도 경고 없이)
    protected void PlayTrigger(string paramName)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return;
        foreach (AnimatorControllerParameter p in animator.parameters)
        {
            if (p.name == paramName)
            {
                animator.SetTrigger(paramName);
                return;
            }
        }
    }

    public void Upgrade()
    {
        // TODO: CanUpgrade 확인 → GameManager.Instance.TrySpendGold(UpgradeCost)
        //       → level++, totalSpent += 비용 → OnLevelChanged()
        if (CanUpgrade && GameManager.Instance.TrySpendGold(UpgradeCost))
        {
            totalSpent += UpgradeCost;
            level++;
            ApplyLevelAnimator();
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
