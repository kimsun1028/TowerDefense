// AI-assisted code start
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI 빌더 공용: 픽셀아트 UI 그림 불러오기(Point, 9-slice 테두리)와 UI 오브젝트 만들기.
/// 그림은 Tools/gen_ui.py가 만든다 (Art/UI). 1텍셀 = PIXEL_SCALE UI px (1920x1080 기준).
/// </summary>
public static class UiArt {
    // 1. 상수
    public const float PIXEL_SCALE = 3f;
    private const string ART_DIR = "Assets/TowerDefense/Art/UI";
    private const float UI_PIXELS_PER_UNIT = 100f;
    private const float REFERENCE_WIDTH = 1920f;
    private const float REFERENCE_HEIGHT = 1080f;
    private const float MATCH_WIDTH_OR_HEIGHT = 0.5f;
    private const string FONT_DIR = "Assets/TowerDefense/Art/Fonts";
    private const float TEXT_OUTLINE_WIDTH = 0.28f;   // 글자 외곽선 (네온 틀·밝은 길 위에서도 읽히게)
    private const float TEXT_FACE_DILATE = 0.12f;
    private static readonly Color TEXT_OUTLINE_COLOR = new(24f / 255f, 18f / 255f, 30f / 255f, 1f); // 픽셀아트 외곽선과 같은 색

    // 4. 메서드 (public)
    /// <summary>Art/UI/name.png를 픽셀아트 스프라이트로 불러온다. border = 9-slice 테두리(텍셀).</summary>
    public static Sprite Load(string spriteName, int border = 0, float pixelsPerUnit = UI_PIXELS_PER_UNIT) {
        string path = $"{ART_DIR}/{spriteName}.png";
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) {
            Debug.LogError($"[UiArt] 그림이 없습니다: {path} (Tools/gen_ui.py 실행)");
            return null;
        }
        var borderVector = new Vector4(border, border, border, border);
        bool needsReimport = importer.textureType != TextureImporterType.Sprite
            || importer.filterMode != FilterMode.Point
            || importer.spriteBorder != borderVector
            || !Mathf.Approximately(importer.spritePixelsPerUnit, pixelsPerUnit)
            || importer.textureCompression != TextureImporterCompression.Uncompressed;
        if (needsReimport) {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.spriteBorder = borderVector;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    public static Sprite Icon(string iconName) {
        return Load("Icon_" + iconName);
    }

    /// <summary>화면 덮는 캔버스 (1920x1080 기준으로 늘고 줄어듦).</summary>
    public static GameObject CreateCanvas(string canvasName, int sortingOrder) {
        var root = new GameObject(canvasName, typeof(RectTransform));
        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        CanvasScaler scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(REFERENCE_WIDTH, REFERENCE_HEIGHT);
        scaler.matchWidthOrHeight = MATCH_WIDTH_OR_HEIGHT;
        root.AddComponent<GraphicRaycaster>();
        return root;
    }

    public static RectTransform CreateRect(string objectName, Transform parent) {
        var rectObject = new GameObject(objectName, typeof(RectTransform));
        rectObject.transform.SetParent(parent, false);
        return (RectTransform)rectObject.transform;
    }

    /// <summary>이미지. sliced = 9-slice (테두리를 PIXEL_SCALE배로 그림).</summary>
    public static Image CreateImage(string objectName, Transform parent, Sprite sprite, bool sliced = false, bool raycast = false) {
        Image image = CreateRect(objectName, parent).gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.raycastTarget = raycast;
        if (sliced) {
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 1f / PIXEL_SCALE;
        }
        return image;
    }

    /// <summary>글자. 기본으로 어두운 외곽선 머티리얼을 쓴다.</summary>
    public static TMP_Text CreateText(string objectName, Transform parent, TMP_FontAsset font, float size, Color color,
                                      TextAlignmentOptions alignment = TextAlignmentOptions.Left) {
        TextMeshProUGUI text = CreateRect(objectName, parent).gameObject.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.fontSharedMaterial = GetOutlineMaterial(font);
        text.fontSize = size;
        text.color = color;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;
        return text;
    }

    /// <summary>폰트별 외곽선 머티리얼 (Art/Fonts/&lt;폰트&gt; Outline.mat). 없으면 만든다.</summary>
    public static Material GetOutlineMaterial(TMP_FontAsset font) {
        string path = $"{FONT_DIR}/{font.name} Outline.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null) {
            material = new Material(font.material);
            AssetDatabase.CreateAsset(material, path);
        }
        material.EnableKeyword(ShaderUtilities.Keyword_Outline);
        material.SetFloat(ShaderUtilities.ID_OutlineWidth, TEXT_OUTLINE_WIDTH);
        material.SetColor(ShaderUtilities.ID_OutlineColor, TEXT_OUTLINE_COLOR);
        material.SetFloat(ShaderUtilities.ID_FaceDilate, TEXT_FACE_DILATE);
        EditorUtility.SetDirty(material);
        return material;
    }

    /// <summary>anchor = pivot 기준 점(0~1), position = 그 점에서 떨어진 거리.</summary>
    public static RectTransform Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size) {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    public static void Stretch(RectTransform rect) {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    public static void SetField(Object target, string field, Object value) {
        var serialized = new SerializedObject(target);
        serialized.FindProperty(field).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    public static void SetArray(Object target, string field, Object[] values) {
        var serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(field);
        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) {
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
// AI-assisted code end
