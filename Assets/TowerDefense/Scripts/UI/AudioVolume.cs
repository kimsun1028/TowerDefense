// AI-assisted code start
using UnityEngine;

/// <summary>
/// AudioSource에 붙이면 설정 화면의 배경음/효과음 음량을 따른다.
/// 실제 음량 = 이 소리의 기본 음량(baseVolume) × 종류 음량 (전체 음량은 AudioListener가 따로 곱한다).
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class AudioVolume : MonoBehaviour {
    // 1. 상수
    private const float DEFAULT_BASE_VOLUME = 1f;

    // 2. 필드
    [SerializeField] private AudioCategory category = AudioCategory.Effects;
    [Tooltip("이 소리 자체의 음량 (설정 음량을 곱하기 전)")]
    [Range(0, 1)] [SerializeField] private float baseVolume = DEFAULT_BASE_VOLUME;

    private AudioSource source;

    public AudioCategory Category {
        get => category;
        set {
            category = value;
            ApplyVolume();
        }
    }

    /// <summary>이 소리 자체의 음량 (0~1). 바꾸면 바로 다시 계산한다.</summary>
    public float BaseVolume {
        get => baseVolume;
        set {
            baseVolume = Mathf.Clamp01(value);
            ApplyVolume();
        }
    }

    // 4. 메서드 (private)
    private void Awake() {
        source = GetComponent<AudioSource>();
    }

    private void OnEnable() {
        GameAudioSettings.Changed += ApplyVolume;
        ApplyVolume();
    }

    private void OnDisable() {
        GameAudioSettings.Changed -= ApplyVolume;
    }

    private void ApplyVolume() {
        if (source == null) {
            return; // Awake 전 (코드에서 붙이는 중)
        }
        source.volume = baseVolume * GameAudioSettings.GetVolume(category);
    }
}
// AI-assisted code end
