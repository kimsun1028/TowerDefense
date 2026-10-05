using UnityEngine;

public abstract partial class Tower : MonoBehaviour
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

    // 선택 시 보이는 사거리 타원 (2.5D: 세로는 Util.VerticalRatio 비율)
    private const int RANGE_SORTING_ORDER = -5;                 // 길(-10) 위, 유닛(0) 아래
    private static readonly Color RANGE_COLOR = new Color(0.5f, 0.5f, 0.5f, 0.25f);
    private SpriteRenderer rangeIndicator;

    // 타워별 사거리 반경 (공격 타워 = 공격 사거리, 병영 = 집결지 이동 범위)
    protected virtual float RangeRadius => 0f;

    public void ShowRange(bool visible)
    {
        if (visible && RangeRadius <= 0f) return;   // 사거리 없는 타워(병영)는 표시 안 함
        if (visible && rangeIndicator == null) CreateRangeIndicator();
        if (rangeIndicator == null) return;
        rangeIndicator.gameObject.SetActive(visible);
        if (visible) RefreshRange();
    }

    private void RefreshRange()
    {
        if (rangeIndicator == null) return;
        float diameter = RangeRadius * 2f;
        Vector2 spriteSize = rangeIndicator.sprite.bounds.size;
        rangeIndicator.transform.localScale = new Vector3(diameter / spriteSize.x, diameter * Util.VerticalRatio / spriteSize.y, 1f);
    }

    private void CreateRangeIndicator()
    {
        var go = new GameObject("RangeIndicator");
        go.transform.SetParent(transform, false);
        rangeIndicator = go.AddComponent<SpriteRenderer>();
        rangeIndicator.sprite = CreateCircleSprite();
        rangeIndicator.color = RANGE_COLOR;
        rangeIndicator.sortingOrder = RANGE_SORTING_ORDER;
    }

    // 사거리 표시용 채워진 원 (한 번 만들어 모든 타워가 공유)
    private static Sprite circleSprite;
    private static Sprite CreateCircleSprite()
    {
        if (circleSprite != null) return circleSprite;
        const int size = 128;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float r = size * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                float a = Mathf.Clamp01(r - d);   // 가장자리 1px 안티앨리어싱
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();
        circleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return circleSprite;
    }

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
            RefreshRange();   // 사거리가 늘었으면 표시도 갱신
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
