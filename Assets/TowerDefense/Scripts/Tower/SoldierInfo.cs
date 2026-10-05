// AI-assisted code start
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 병사(병영·영웅)의 정보 바 표시 (IInfoSource 구현). Soldier.cs와 같은 클래스(partial)라 private 상태도 바로 읽는다.
/// 클릭은 InfoSelector가 병사의 트리거 Collider2D(+ InfoClickArea)로 찾는다.
/// </summary>
public partial class Soldier : IInfoSource {
    // 4. 메서드 (public)
    public bool IsInfoValid => this != null && IsAlive;
    public string InfoName => data == null ? name : string.IsNullOrEmpty(data.displayName) ? data.soldierName : data.displayName;
    public Sprite InfoPortrait => data != null && data.portrait != null ? data.portrait : body != null ? body.sprite : null;
    public bool HasInfoHealth => true;
    public float InfoHp => Mathf.Max(0f, hp);
    public float InfoMaxHp => data != null ? data.maxHp : 0f;
    public string InfoDescription => data != null ? data.description : string.Empty;
    public Vector2 InfoFootPosition => transform.position;

    public void FillInfoRows(List<InfoRow> rows) {
        rows.Clear();
        if (data == null) {
            return;
        }
        rows.Add(new InfoRow("근접 공격", $"{data.damage:0.#} ({data.attackInterval:0.##}초마다)"));
        if (owner != null) {
            rows.Add(new InfoRow("레벨", $"Lv{owner.Level}"));
        }
    }
}
// AI-assisted code end
