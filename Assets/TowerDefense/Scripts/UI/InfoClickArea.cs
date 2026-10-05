// AI-assisted code start
using UnityEngine;

/// <summary>
/// 이 오브젝트의 충돌체를 "클릭하면 정보 바에 보여줄 영역"으로 표시한다 (병사 등).
/// 같은 오브젝트나 부모의 IInfoSource를 보여준다.
/// 타워 사거리처럼 감지용 충돌체가 클릭을 가로채지 않도록, InfoSelector는 이 표시가 있는 충돌체만 대상으로 본다.
/// </summary>
public class InfoClickArea : MonoBehaviour {
    // 2. 필드
    private IInfoSource source;

    public IInfoSource Source => source ??= GetComponentInParent<IInfoSource>();
}
// AI-assisted code end
