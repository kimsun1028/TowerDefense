// AI-assisted code start
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 적의 정보 바 표시 (IInfoSource 구현). Enemy.cs와 같은 클래스(partial)라 private 상태도 바로 읽는다.
/// 데이터(EnemyData)에서 자동으로 만들어지므로, 새 적은 데이터만 채우면 정보 바에도 바로 나온다.
/// </summary>
public partial class Enemy : IInfoSource {
    // 4. 메서드 (public)
    public bool IsInfoValid => this != null && !IsDead;
    public string InfoName => data ? data.DisplayName : name;
    public Sprite InfoPortrait => data ? data.Portrait : null;
    public bool HasInfoHealth => true;
    public float InfoHp => Mathf.Max(0f, Hp);
    public float InfoMaxHp => MaxHp;
    public string InfoDescription => data ? data.Description : string.Empty;
    public Vector2 InfoFootPosition => transform.position;

    public void FillInfoRows(List<InfoRow> rows) {
        rows.Clear();
        if (data == null) {
            return;
        }
        rows.Add(new InfoRow("근접 공격", FormatMelee()));
        if (data.HasRangedAttack) {
            rows.Add(new InfoRow("원거리 공격",
                $"{StatRating.FormatRange(data.RangedDamageMin, data.RangedDamageMax)} ({data.RangedInterval:0.##}초마다, 사거리 {data.RangedRange:0.#})"));
        }
        rows.Add(new InfoRow("방어력", StatRating.FormatResist(data.Armor)));
        rows.Add(new InfoRow("마법 저항", StatRating.FormatResist(data.MagicResist)));
        rows.Add(new InfoRow("속도", StatRating.FormatSpeed(data.Speed)));
        rows.Add(new InfoRow("라이프", $"-{data.LifeDamage}"));
        string traits = FormatTraits();
        if (traits.Length > 0) {
            rows.Add(new InfoRow("특성", traits));
        }
    }

    // 4. 메서드 (private)
    private string FormatMelee() {
        if (data.IsFlying || data.MeleeDamageMax <= 0) {
            return "없음";
        }
        string text = $"{StatRating.FormatRange(data.MeleeDamageMin, data.MeleeDamageMax)} ({data.MeleeInterval:0.##}초마다)";
        return data.MeleeTargetCount > 1 ? $"{text}, 범위 최대 {data.MeleeTargetCount}명" : text;
    }

    private string FormatTraits() {
        var traits = new List<string>();
        if (data.IsBoss) {
            traits.Add("보스");
        }
        if (data.IsFlying) {
            traits.Add("비행");
        }
        if (data.MeleeEvasion > 0f) {
            traits.Add($"근접 회피 {StatRating.FormatPercent(data.MeleeEvasion)}");
        }
        if ((data.Immunities & StatusEffect.Poison) != 0) {
            traits.Add("독 면역");
        }
        if ((data.Immunities & StatusEffect.Slow) != 0) {
            traits.Add("둔화 면역");
        }
        if ((data.Immunities & StatusEffect.Stun) != 0) {
            traits.Add("기절 면역");
        }
        if ((data.Immunities & StatusEffect.Burn) != 0) {
            traits.Add("화상 면역");
        }
        if (data.NextForm != null) {
            traits.Add("변신");
        }
        if (data.KillAllOnDeath) {
            traits.Add("최후의 저항");
        }
        return string.Join(", ", traits);
    }
}
// AI-assisted code end
