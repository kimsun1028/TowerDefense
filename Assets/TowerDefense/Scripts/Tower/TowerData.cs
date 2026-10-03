using UnityEngine;

public abstract class TowerData : ScriptableObject
{
    public string towerName;
    public abstract int MaxLevel { get; }
}
