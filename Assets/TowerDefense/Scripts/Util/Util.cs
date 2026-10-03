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
}
