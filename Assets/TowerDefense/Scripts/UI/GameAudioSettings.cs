// AI-assisted code start
using System;
using UnityEngine;

/// <summary>소리 종류. 전체 음량은 모든 소리에, 배경음·효과음은 그 종류의 AudioSource(AudioVolume)에만 곱해진다.</summary>
public enum AudioCategory {
    Music,
    Effects,
}

/// <summary>
/// 소리 설정 (전체 · 배경음 · 효과음 음량, 음소거). PlayerPrefs에 저장돼서 게임을 껐다 켜도 남는다.
///   전체 음량·음소거 → AudioListener.volume (모든 소리)
///   배경음·효과음 → AudioVolume 컴포넌트가 붙은 AudioSource에 곱해진다
/// 게임이 시작될 때 저장된 값을 바로 적용한다.
/// </summary>
public static class GameAudioSettings {
    // 1. 상수
    private const string MASTER_KEY = "TD.Audio.Master";
    private const string MUSIC_KEY = "TD.Audio.Music";
    private const string EFFECTS_KEY = "TD.Audio.Effects";
    private const string MUTED_KEY = "TD.Audio.Muted";
    private const float DEFAULT_VOLUME = 1f;

    // 2. 필드
    /// <summary>값이 바뀌면 (AudioVolume이 다시 계산한다).</summary>
    public static event Action Changed;

    public static float Master { get; private set; } = DEFAULT_VOLUME;
    public static float Music { get; private set; } = DEFAULT_VOLUME;
    public static float Effects { get; private set; } = DEFAULT_VOLUME;
    public static bool IsMuted { get; private set; }

    // 4. 메서드 (public)
    public static void SetMaster(float volume) {
        Master = Mathf.Clamp01(volume);
        Save(MASTER_KEY, Master);
    }

    public static void SetMusic(float volume) {
        Music = Mathf.Clamp01(volume);
        Save(MUSIC_KEY, Music);
    }

    public static void SetEffects(float volume) {
        Effects = Mathf.Clamp01(volume);
        Save(EFFECTS_KEY, Effects);
    }

    public static void SetMuted(bool muted) {
        IsMuted = muted;
        PlayerPrefs.SetInt(MUTED_KEY, muted ? 1 : 0);
        Apply();
    }

    /// <summary>그 종류의 음량 (전체 음량은 AudioListener가 따로 곱한다).</summary>
    public static float GetVolume(AudioCategory category) {
        return category == AudioCategory.Music ? Music : Effects;
    }

    // 4. 메서드 (private)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Load() {
        Master = PlayerPrefs.GetFloat(MASTER_KEY, DEFAULT_VOLUME);
        Music = PlayerPrefs.GetFloat(MUSIC_KEY, DEFAULT_VOLUME);
        Effects = PlayerPrefs.GetFloat(EFFECTS_KEY, DEFAULT_VOLUME);
        IsMuted = PlayerPrefs.GetInt(MUTED_KEY, 0) == 1;
        Apply();
    }

    private static void Save(string key, float value) {
        PlayerPrefs.SetFloat(key, value);
        Apply();
    }

    private static void Apply() {
        AudioListener.volume = IsMuted ? 0f : Master;
        Changed?.Invoke();
    }
}
// AI-assisted code end
