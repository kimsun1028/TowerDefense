// AI-assisted code start
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 클릭했을 때 화면 아래 정보 바에 보여줄 수 있는 대상 (적, 타워 등).
/// 정보 바(UI)는 이 인터페이스만 보고 그리므로, 적·타워 어느 쪽이든 구현하면 같은 바에 나온다.
/// </summary>
public interface IInfoSource {
    /// <summary>대상이 아직 유효한가 (죽었거나 사라졌으면 false → 정보 바가 닫힘).</summary>
    bool IsInfoValid { get; }
    string InfoName { get; }
    /// <summary>초상화 (없으면 null).</summary>
    Sprite InfoPortrait { get; }
    /// <summary>체력바를 보여줄지 (타워는 보통 false).</summary>
    bool HasInfoHealth { get; }
    float InfoHp { get; }
    float InfoMaxHp { get; }
    /// <summary>한 줄 설명 (특징). 없으면 빈 문자열.</summary>
    string InfoDescription { get; }
    /// <summary>선택 표시를 그릴 발밑 위치.</summary>
    Vector2 InfoFootPosition { get; }

    /// <summary>정보 줄 목록을 채운다 (매 프레임 호출되므로 rows를 재사용).</summary>
    void FillInfoRows(List<InfoRow> rows);
}

/// <summary>정보 바의 한 줄: 항목 이름과 값.</summary>
public readonly struct InfoRow {
    // 2. 필드
    private readonly string label;
    private readonly string value;

    public string Label => label;
    public string Value => value;

    // 3. 생성자
    public InfoRow(string rowLabel, string rowValue) {
        label = rowLabel;
        value = rowValue;
    }
}
// AI-assisted code end
