// AI-assisted code start
using UnityEngine;

/// <summary>
/// 땅 위에 희미한 사거리 원을 그린다 (링 메뉴 버튼에 마우스를 올렸을 때 "지으면/강화하면 여기까지 닿는다" 미리보기).
/// 원 그림은 실행할 때 한 번 만들어 쓴다 (안은 옅게, 테두리는 조금 진하게). 크기는 가로·세로 반경으로 늘린다.
///   공격 타워 사거리: 2.5D 타원 (세로 = 가로 × EllipseRange.VERTICAL_RATIO, 타워 쪽 Util.IsInEllipse와 같은 모양)
///   병영 집결지 거리: 정원 (RallyPointTool과 같은 모양)
/// </summary>
public class RangePreview : MonoBehaviour {
    // 1. 상수
    private const int TEXTURE_SIZE = 256;
    private const float EDGE_WIDTH = 4f;          // 테두리 두께 (텍셀)
    private const float FILL_ALPHA = 0.07f;
    private const float EDGE_ALPHA = 0.45f;
    private const int SORTING_ORDER = -5;         // 타워 자리(-6) 위, 자리 번호(-4)·타워(0) 아래
    private const float HALF = 0.5f;
    private static readonly Color DEFAULT_COLOR = new(0.55f, 0.95f, 1f, 1f);

    // 2. 필드
    [SerializeField] private Color color = DEFAULT_COLOR;

    private static RangePreview instance;
    private SpriteRenderer circle;

    /// <summary>씬의 RangePreview. 없으면 하나 만든다.</summary>
    public static RangePreview Instance {
        get {
            if (instance == null) {
                instance = FindAnyObjectByType<RangePreview>();
            }
            if (instance == null) {
                instance = new GameObject(nameof(RangePreview)).AddComponent<RangePreview>();
            }
            return instance;
        }
    }

    // 4. 메서드 (public)
    /// <summary>공격 사거리 (2.5D 타원). range = 가로 반경.</summary>
    public void ShowEllipse(Vector2 center, float range) {
        Show(center, range, range * EllipseRange.VERTICAL_RATIO);
    }

    /// <summary>정원 (병영 집결지 거리).</summary>
    public void ShowCircle(Vector2 center, float radius) {
        Show(center, radius, radius);
    }

    public void Hide() {
        if (circle != null) {
            circle.enabled = false;
        }
    }

    // 4. 메서드 (private)
    private void Awake() {
        instance = this;
        circle = new GameObject("Circle").AddComponent<SpriteRenderer>();
        circle.transform.SetParent(transform, false);
        circle.sprite = CreateCircleSprite();
        circle.sortingOrder = SORTING_ORDER;
        circle.color = color;
        circle.enabled = false;
    }

    private void Show(Vector2 center, float radiusX, float radiusY) {
        if (radiusX <= 0f || radiusY <= 0f) {
            Hide();
            return;
        }
        circle.transform.position = center;
        circle.transform.localScale = new Vector3(radiusX * 2f, radiusY * 2f, 1f); // 그림 지름 = 1 유닛
        circle.color = color;
        circle.enabled = true;
    }

    /// <summary>지름 1 유닛짜리 원: 안은 FILL_ALPHA, 테두리 EDGE_WIDTH는 EDGE_ALPHA (흰색 → SpriteRenderer.color로 물든다).</summary>
    private static Sprite CreateCircleSprite() {
        var texture = new Texture2D(TEXTURE_SIZE, TEXTURE_SIZE, TextureFormat.RGBA32, false) {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
        };
        float radius = TEXTURE_SIZE * HALF;
        var pixels = new Color32[TEXTURE_SIZE * TEXTURE_SIZE];
        for (int y = 0; y < TEXTURE_SIZE; y++) {
            for (int x = 0; x < TEXTURE_SIZE; x++) {
                float distance = Vector2.Distance(new Vector2(x + HALF, y + HALF), new Vector2(radius, radius));
                float alpha = 0f;
                if (distance <= radius) {
                    float edge = Mathf.Clamp01((distance - (radius - EDGE_WIDTH)) / EDGE_WIDTH);
                    alpha = Mathf.Lerp(FILL_ALPHA, EDGE_ALPHA, edge) * Mathf.Clamp01(radius - distance + HALF);
                }
                pixels[y * TEXTURE_SIZE + x] = new Color(1f, 1f, 1f, alpha);
            }
        }
        texture.SetPixels32(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, TEXTURE_SIZE, TEXTURE_SIZE), new Vector2(HALF, HALF), TEXTURE_SIZE);
    }
}
// AI-assisted code end
