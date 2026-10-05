using UnityEngine;

public static class Util
{
    // 땅 기준 카메라 각도
    private const float theta = 45f;

    public static readonly float VerticalRatio = Mathf.Sin(Mathf.Deg2Rad * theta);

    public static bool IsInEllipse(Vector2 centerPosition, Vector2 targetPosition, float hrange)
    {   
        if(hrange <= 0)
        {
            throw new System.ArgumentOutOfRangeException(nameof(hrange),"horizontal range <= 0");
        }
        float vrange = VerticalRatio * hrange;
        Vector2 diff = centerPosition - targetPosition;
        float xdiff = diff.x/hrange;
        float ydiff = diff.y/vrange;
        bool isInEllipse = (xdiff*xdiff + ydiff*ydiff <= 1);
        return isInEllipse;
    }

    // 적의 몸통 원(충돌체)이 타원에 조금이라도 걸치는지 (사거리·폭발 범위 판정)
    // 타원의 가로·세로 반경을 몸통 반지름만큼 늘려서, 몸통 중심이 그 안에 있는지 확인. 충돌체가 없으면 발밑 한 점으로
    public static bool IsBodyTouchingEllipse(Vector2 centerPosition, float hrange, Component body)
    {
        Vector2 point = body.transform.position;
        float bodyRadius = 0f;
        if (body.TryGetComponent(out CircleCollider2D circle))
        {
            point = circle.bounds.center;
            bodyRadius = circle.bounds.extents.x;
        }
        float h = hrange + bodyRadius;
        float v = hrange * VerticalRatio + bodyRadius;
        Vector2 diff = point - centerPosition;
        float x = diff.x / h;
        float y = diff.y / v;
        return x * x + y * y <= 1f;
    }
}
