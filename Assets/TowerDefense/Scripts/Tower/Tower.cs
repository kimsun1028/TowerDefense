using UnityEngine;

public abstract class Tower : MonoBehaviour
{
    protected int level = 1;

    protected int totalSpent;

    protected TowerSlot slot;

    public void Init(TowerSlot slot)
    {
        this.slot = slot;
    }
}
