// AI-assisted code start
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임 소리 (Assets/Audio, 사운드_사용표.md 기준). GameHud 프리팹에 붙어 있다.
///   배경 음악 BGM_Stage_Loop: 게임 씬에서 반복 재생 (설정 창의 배경음 음량)
///   웨이브 시작 SFX_Wave_Start: WaveSpawner.WaveStarted (입구 버튼으로 일찍 불러도)
///   목숨 감소 SFX_Player_Damage: Enemy.ReachedGoal, 한 프레임에 여러 마리가 들어와도 한 번
///   적 사망 SFX_Unit_Death: Enemy.Killed (보스는 pitch 낮게)
///   건설 SFX_Tower_Build: 타워 자리에 타워가 새로 생기면. 강화는 메뉴 연결(TowerBuildMenuAdapter)이 PlayTowerBuild를 부른다
///   타워 공격 SFX_*Tower_Shoot_Lv12 / Lv34: AttackTower.Attack이 PlayTowerShoot (Lv2·Lv4는 살짝 높게)
///   적 근접 타격 SFX_Hit_Light / Heavy: Enemy.MeleeHit (EnemyData.HeavyHit, 보스는 낮게)
///   병사·영웅 타격 / 사망: Soldier가 PlayAllyHit / PlayAllyDeath (Lv3 이상·영웅은 묵직한 소리, 사망은 높게)
/// 레드락(SFX_RedLock_*)은 봉인 기능이 아직 없어서 연결하지 않았다.
/// 씬에 AudioListener(소리를 듣는 귀)가 없으면 메인 카메라에 붙인다 (맵 카메라에는 기본으로 없다).
/// 효과음은 설정 창의 효과음 음량을 따른다 (AudioVolume). 전체 음량·음소거는 AudioListener라 모든 소리에 적용.
/// 다른 소리를 더 넣을 때는 GameSounds.Instance?.PlayVaried(clip) 한 줄로 재생하면 된다.
/// </summary>
public class GameSounds : MonoBehaviour {
    // 1. 상수
    private const int EFFECT_CHANNELS = 8;            // 동시에 겹쳐 날 수 있는 효과음 수 (채널마다 pitch를 따로)
    private const float DEFAULT_MUSIC_VOLUME = 0.7f;  // 사용표 권장 0.6~0.8
    private const float VARIED_PITCH_MIN = 0.95f;     // 자주 나는 소리 반복감 줄이기 (사용표)
    private const float VARIED_PITCH_MAX = 1.05f;
    private const float BOSS_DEATH_PITCH = 0.7f;
    private const float NORMAL_PITCH = 1f;
    private const float EVEN_LEVEL_PITCH = 1.08f;      // 같은 소리 구간 안에서 Lv2·Lv4는 살짝 높게 (사용표 1.05~1.1)
    private const float BOSS_HIT_PITCH = 0.8f;
    private const float ALLY_DEATH_PITCH = 1.15f;       // 아군 사망은 적과 구분되게 높게 (사용표)
    private const int STRONG_LEVEL = 3;                 // Lv3부터 강한 공격음 · 묵직한 타격음
    private const string ARCHER_TOWER = "ArcherTower";
    private const string ARTILLERY_TOWER = "ArtilleryTower";
    private const string MAGIC_TOWER = "MagicTower";

    // 2. 필드
    [SerializeField] private AudioClip stageMusic;
    [SerializeField] private AudioClip waveStart;
    [SerializeField] private AudioClip playerDamage;
    [SerializeField] private AudioClip unitDeath;
    [SerializeField] private AudioClip towerBuild;
    [Header("타워 공격 (Lv1·2 / Lv3·4)")]
    [SerializeField] private AudioClip archerShoot;
    [SerializeField] private AudioClip archerShootStrong;
    [SerializeField] private AudioClip artilleryShoot;
    [SerializeField] private AudioClip artilleryShootStrong;
    [SerializeField] private AudioClip magicShoot;
    [SerializeField] private AudioClip magicShootStrong;
    [Header("근접 타격")]
    [SerializeField] private AudioClip hitLight;
    [SerializeField] private AudioClip hitHeavy;
    [Range(0, 1)] [SerializeField] private float musicVolume = DEFAULT_MUSIC_VOLUME;

    private static GameSounds instance;
    private readonly List<AudioSource> channels = new();
    private readonly HashSet<TowerSlot> occupiedSlots = new();
    private TowerSlot[] slots;
    private AudioSource music;
    private int nextChannel;
    private int lastDamageFrame = -1;

    /// <summary>씬의 GameSounds (없으면 null). 플레이 중 스크립트를 다시 컴파일해 static이 비면 다시 찾는다.</summary>
    public static GameSounds Instance => instance ? instance : instance = FindAnyObjectByType<GameSounds>();

    // 4. 메서드 (public)
    /// <summary>효과음 한 번. pitch는 이 소리에만 적용된다.</summary>
    public void Play(AudioClip clip, float pitch = NORMAL_PITCH) {
        if (clip == null || channels.Count == 0) {
            return;
        }
        AudioSource channel = channels[nextChannel];
        nextChannel = (nextChannel + 1) % channels.Count;
        channel.pitch = pitch;
        channel.PlayOneShot(clip);
    }

    /// <summary>자주 나는 소리용: pitch를 0.95~1.05에서 랜덤으로 (타워 공격, 타격, 사망).</summary>
    public void PlayVaried(AudioClip clip) {
        Play(clip, Random.Range(VARIED_PITCH_MIN, VARIED_PITCH_MAX));
    }

    /// <summary>건설·강화 소리.</summary>
    public void PlayTowerBuild() {
        Play(towerBuild);
    }

    /// <summary>타워 공격음. towerType = 타워 클래스 이름 (ArcherTower / ArtilleryTower / MagicTower), level = 1~4.</summary>
    public void PlayTowerShoot(string towerType, int level) {
        bool strong = level >= STRONG_LEVEL;
        AudioClip clip = towerType switch {
            ARCHER_TOWER => strong ? archerShootStrong : archerShoot,
            ARTILLERY_TOWER => strong ? artilleryShootStrong : artilleryShoot,
            MAGIC_TOWER => strong ? magicShootStrong : magicShoot,
            _ => null,
        };
        float levelPitch = level % 2 == 0 ? EVEN_LEVEL_PITCH : NORMAL_PITCH;
        Play(clip, levelPitch * Random.Range(VARIED_PITCH_MIN, VARIED_PITCH_MAX));
    }

    /// <summary>병사·영웅 근접 타격. 영웅이거나 Lv3 이상이면 묵직한 소리.</summary>
    public void PlayAllyHit(bool isHero, int level) {
        PlayVaried(isHero || level >= STRONG_LEVEL ? hitHeavy : hitLight);
    }

    /// <summary>병사·영웅 사망 (적보다 높은 소리).</summary>
    public void PlayAllyDeath() {
        Play(unitDeath, ALLY_DEATH_PITCH);
    }

    // 4. 메서드 (private)
    private void Awake() {
        instance = this;
        EnsureListener();
        music = CreateSource("Music", AudioCategory.Music, musicVolume);
        music.loop = true;
        for (int i = 0; i < EFFECT_CHANNELS; i++) {
            channels.Add(CreateSource($"Effect{i}", AudioCategory.Effects, NORMAL_PITCH));
        }
    }

    private void Start() {
        slots = FindObjectsByType<TowerSlot>(FindObjectsSortMode.None);
        foreach (TowerSlot slot in slots) {
            if (slot.IsOccupied) {
                occupiedSlots.Add(slot);
            }
        }
        if (stageMusic != null) {
            music.clip = stageMusic;
            music.Play();
        }
    }

    private void OnEnable() {
        WaveSpawner.WaveStarted += OnWaveStarted;
        Enemy.ReachedGoal += OnEnemyReachedGoal;
        Enemy.Killed += OnEnemyKilled;
        Enemy.MeleeHit += OnEnemyMeleeHit;
    }

    private void OnDisable() {
        WaveSpawner.WaveStarted -= OnWaveStarted;
        Enemy.ReachedGoal -= OnEnemyReachedGoal;
        Enemy.Killed -= OnEnemyKilled;
        Enemy.MeleeHit -= OnEnemyMeleeHit;
    }

    private void Update() {
        if (slots == null) {
            return;
        }
        foreach (TowerSlot slot in slots) {
            if (slot == null) {
                continue;
            }
            bool wasOccupied = occupiedSlots.Contains(slot);
            if (slot.IsOccupied && !wasOccupied) {
                occupiedSlots.Add(slot);
                PlayTowerBuild(); // 링 메뉴든 숫자키든 새로 지어졌다
            } else if (!slot.IsOccupied && wasOccupied) {
                occupiedSlots.Remove(slot); // 판매
            }
        }
    }

    private void OnWaveStarted(int wave) {
        Play(waveStart);
    }

    private void OnEnemyReachedGoal(Enemy enemy) {
        if (lastDamageFrame == Time.frameCount) {
            return;
        }
        lastDamageFrame = Time.frameCount;
        Play(playerDamage);
    }

    private void OnEnemyKilled(Enemy enemy) {
        if (enemy != null && enemy.IsBoss) {
            Play(unitDeath, BOSS_DEATH_PITCH);
        } else {
            PlayVaried(unitDeath);
        }
    }

    private void OnEnemyMeleeHit(Enemy enemy) {
        AudioClip clip = enemy.Data != null && enemy.Data.HeavyHit ? hitHeavy : hitLight;
        if (enemy.IsBoss) {
            Play(clip, BOSS_HIT_PITCH);
        } else {
            PlayVaried(clip);
        }
    }

    /// <summary>소리를 들으려면 씬에 AudioListener가 하나 있어야 한다. 없으면 메인 카메라(없으면 이 오브젝트)에 붙인다.</summary>
    private void EnsureListener() {
        if (FindAnyObjectByType<AudioListener>() != null) {
            return;
        }
        Camera cam = Camera.main;
        GameObject host = cam != null ? cam.gameObject : gameObject;
        host.AddComponent<AudioListener>();
    }

    /// <summary>자식 AudioSource + 설정 창 음량(AudioVolume).</summary>
    private AudioSource CreateSource(string sourceName, AudioCategory category, float baseVolume) {
        var sourceObject = new GameObject(sourceName);
        sourceObject.transform.SetParent(transform, false);
        AudioSource source = sourceObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f; // 2D
        AudioVolume volume = sourceObject.AddComponent<AudioVolume>();
        volume.Category = category;
        volume.BaseVolume = baseVolume;
        return source;
    }
}
// AI-assisted code end
