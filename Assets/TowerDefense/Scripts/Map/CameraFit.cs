// AI-assisted code start
using UnityEngine;

/// <summary>
/// 화면 비율과 관계없이 지정한 월드 영역(맵)이 화면을 꽉 채우도록 카메라를 맞춘다.
/// Fit = 영역 전체가 보이게 (남는 곳은 배경색), Fill = 화면을 빈틈없이 채우게 (영역 일부가 잘릴 수 있음)
/// </summary>
[ExecuteAlways, RequireComponent(typeof(Camera))]
public class CameraFit : MonoBehaviour {
    public enum Mode { Fit, Fill }

    // 1. 상수
    private static readonly Rect DEFAULT_WORLD_RECT = new(-6.8f, -4.2f, 13.6f, 9.1f);
    private const float HALF = 0.5f;

    // 2. 필드
    [Tooltip("켜면 씬의 TDMap.ViewRect를 사용한다")]
    [SerializeField] private bool fitToMap = true;
    [SerializeField] private Rect worldRect = DEFAULT_WORLD_RECT;
    [SerializeField] private Mode mode = Mode.Fit;

    private Camera cam;
    private float lastAspect;

    public bool FitToMap { get => fitToMap; set => fitToMap = value; }
    public Rect WorldRect { get => worldRect; set => worldRect = value; }
    public Mode FitMode { get => mode; set => mode = value; }

    // 4. 메서드
    /// <summary>현재 화면 비율로 카메라 크기와 위치를 다시 맞춘다.</summary>
    public void Apply() {
        if (fitToMap && TDMap.Instance != null) {
            worldRect = TDMap.Instance.ViewRect;
        }
        lastAspect = cam.aspect;
        float sizeByHeight = worldRect.height * HALF;
        float sizeByWidth = worldRect.width * HALF / cam.aspect;
        cam.orthographic = true;
        cam.orthographicSize = mode == Mode.Fit
            ? Mathf.Max(sizeByHeight, sizeByWidth)
            : Mathf.Min(sizeByHeight, sizeByWidth);
        Vector2 center = worldRect.center;
        transform.position = new Vector3(center.x, center.y, transform.position.z);
    }

    private void OnEnable() {
        cam = GetComponent<Camera>();
        Apply();
    }

    private void Update() {
        if (!Mathf.Approximately(cam.aspect, lastAspect)) {
            Apply();
        }
    }

    private void OnValidate() {
        if (cam) {
            Apply();
        }
    }

    private void OnDrawGizmosSelected() {
        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(worldRect.center, worldRect.size);
    }
}
// AI-assisted code end
