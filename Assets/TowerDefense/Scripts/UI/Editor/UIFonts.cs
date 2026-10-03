// AI-assisted code start
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

/// <summary>
/// UI 폰트 (나눔고딕, SIL OFL 1.1). TTF에서 TextMeshPro 폰트 에셋(SDF)을 만들어 둔다.
/// 동적(Dynamic) 에셋이라 처음 쓰는 글자는 그때그때 아틀라스에 추가된다 (한글 전체를 미리 굽지 않음).
/// </summary>
public static class UIFonts {
    // 1. 상수
    private const string FONT_DIR = "Assets/TowerDefense/Art/Fonts";
    private const string REGULAR_TTF = FONT_DIR + "/NanumGothic-Regular.ttf";
    private const string BOLD_TTF = FONT_DIR + "/NanumGothic-Bold.ttf";
    private const string REGULAR_ASSET = FONT_DIR + "/NanumGothic-Regular SDF.asset";
    private const string BOLD_ASSET = FONT_DIR + "/NanumGothic-Bold SDF.asset";
    private const int SAMPLING_POINT_SIZE = 64;
    private const int ATLAS_PADDING = 6;
    private const int ATLAS_SIZE = 1024; // 부족하면 다중 아틀라스로 자동 추가

    // 2. 필드 (프로퍼티)
    public static TMP_FontAsset Regular => GetOrCreate(REGULAR_TTF, REGULAR_ASSET);
    public static TMP_FontAsset Bold => GetOrCreate(BOLD_TTF, BOLD_ASSET);

    // 4. 메서드 (public)
    /// <summary>TMP 기본 리소스(셰이더·설정)가 없으면 가져온다. 폰트 에셋을 만들기 전에 필요.</summary>
    public static void EnsureTmpEssentials() {
        if (AssetDatabase.FindAssets("t:TMP_Settings").Length > 0) {
            return;
        }
        // Packages/ 경로를 실제 파일 경로로 바꿔서 가져온다 (가져오기는 비동기로 끝남)
        string package = System.IO.Path.GetFullPath("Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage");
        AssetDatabase.ImportPackage(package, false);
        AssetDatabase.Refresh();
    }

    // 4. 메서드 (private)
    private static TMP_FontAsset GetOrCreate(string ttfPath, string assetPath) {
        TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
        if (existing != null) {
            return existing;
        }
        Font font = AssetDatabase.LoadAssetAtPath<Font>(ttfPath);
        if (font == null) {
            Debug.LogError($"[UIFonts] 폰트 파일이 없습니다: {ttfPath}");
            return null;
        }
        TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(font, SAMPLING_POINT_SIZE, ATLAS_PADDING,
            GlyphRenderMode.SDFAA, ATLAS_SIZE, ATLAS_SIZE, AtlasPopulationMode.Dynamic, true);
        fontAsset.name = System.IO.Path.GetFileNameWithoutExtension(assetPath);
        AssetDatabase.CreateAsset(fontAsset, assetPath);

        // 에디터에서 쌓인 글자는 빌드 때 비움 (파일 크기·변경 줄이기). 공개 프로퍼티가 없어 직렬화 필드로 설정
        var serialized = new SerializedObject(fontAsset);
        serialized.FindProperty("m_ClearDynamicDataOnBuild").boolValue = true;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        // 아틀라스 텍스처와 머티리얼을 같은 에셋 안에 저장 (안 하면 다시 열 때 사라짐)
        fontAsset.atlasTextures[0].name = fontAsset.name + " Atlas";
        AssetDatabase.AddObjectToAsset(fontAsset.atlasTextures[0], fontAsset);
        fontAsset.material.name = fontAsset.name + " Material";
        AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
        AssetDatabase.SaveAssets();
        return fontAsset;
    }
}
// AI-assisted code end
