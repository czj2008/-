using UnityEngine;

/// <summary>
/// 全局音频管理器。
///
/// 【单例 + DontDestroyOnLoad】整个游戏只有一个实例，切场景时不会销毁。
/// 【场景里放一个】在 Menu 场景（第一个场景）里放一个挂本脚本的物体，
///   之后切到 Battle 场景也会继续用这同一个（Battle 里不用再放）。
///
/// 【和 AudioSource 的关系】
///   SFX 用 PlayOneShot 播（多个音效可以重叠，不会打断）
///   BGM 用一个常驻的 AudioSource 循环播
/// </summary>
public class AudioManager : MonoBehaviour
{
    private const string KEY_VOLUME = "settings.volume";
    private const string KEY_BGM_VOLUME = "settings.bgmVolume";

    private static AudioManager _instance;

    public static AudioManager Instance
    {
        get
        {
            if (_instance == null)
            {
                // 先找场景里已经放好的
                _instance = FindObjectOfType<AudioManager>();

                // 没有才自动创建一个（保证不会因为"忘了放"而静音）
                if (_instance == null)
                {
                    GameObject go = new GameObject("~AudioManager");
                    _instance = go.AddComponent<AudioManager>();
                }

                DontDestroyOnLoad(_instance.gameObject);
            }
            return _instance;
        }
    }

    // =====================================================================
    //  素材（在 Inspector 里拖）
    // =====================================================================

    [Header("音效")]
    [Tooltip("按钮点击")]
    public AudioClip ClickClip;

    [Tooltip("受击（玩家和敌人共用，靠音调区分）")]
    public AudioClip HitClip;

    [Tooltip("胜利")]
    public AudioClip VictoryClip;

    [Tooltip("失败")]
    public AudioClip DefeatClip;

    [Header("背景音乐（可留空）")]
    public AudioClip BgmClip;

    // =====================================================================
    //  音量
    // =====================================================================

    [Header("音量（0~1）")]
    [Range(0f, 1f)] public float SfxVolume = 0.8f;
    [Range(0f, 1f)] public float BgmVolume = 0.35f;

    [Header("各音效的相对倍率")]
    public float ClickScale = 0.6f;
    public float HitScale = 0.9f;
    public float VictoryScale = 0.9f;
    public float DefeatScale = 0.9f;

    private AudioSource _sfxSource;
    private AudioSource _bgmSource;

    // =====================================================================
    //  初始化
    // =====================================================================

    private void Awake()
    {
        // 单例去重：如果已经有实例（比如从别的场景带过来的），销毁自己
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        // 两个 AudioSource：一个放音效，一个放 BGM
        _sfxSource = gameObject.AddComponent<AudioSource>();
        _sfxSource.playOnAwake = false;
        _sfxSource.spatialBlend = 0f;          // 2D
        _sfxSource.volume = 1f;

        _bgmSource = gameObject.AddComponent<AudioSource>();
        _bgmSource.playOnAwake = false;
        _bgmSource.spatialBlend = 0f;
        _bgmSource.loop = true;
        _bgmSource.volume = 0f;

        SfxVolume = PlayerPrefs.GetFloat(KEY_VOLUME, 0.8f);
        BgmVolume = PlayerPrefs.GetFloat(KEY_BGM_VOLUME, 0.35f);

        Debug.Log("[AudioManager] 初始化完成 | SFX " + SfxVolume.ToString("0.00") +
                  " BGM " + BgmVolume.ToString("0.00"));

        if (BgmClip != null) PlayBgm(BgmClip);
    }

    // =====================================================================
    //  音效接口
    // =====================================================================

    /// <summary>按钮点击。（没配 ClickClip 时用受击音升调替代）</summary>
    public void PlayClick()
    {
        if (ClickClip != null) Play(ClickClip, ClickScale, Random.Range(0.98f, 1.02f));
        else Play(HitClip, ClickScale * 0.7f, Random.Range(1.7f, 1.85f));   // 升调当"咔哒"
    }

    /// <summary>受击。pitch 越低越"沉"，用来区分玩家和敌人。</summary>
    public void PlayHit(float pitch = 1f)
    {
        Play(HitClip, HitScale, pitch * Random.Range(0.97f, 1.03f));
    }

    /// <summary>玩家受伤（音调偏低，听感更"重"）。</summary>
    public void PlayPlayerHit() { PlayHit(0.88f); }

    /// <summary>敌人受伤（音调偏高，听感更"脆"）。</summary>
    public void PlayEnemyHit() { PlayHit(1.15f); }

    /// <summary>胜利。</summary>
    public void PlayVictory() { Play(VictoryClip, VictoryScale, 1f); }

    /// <summary>失败。</summary>
    public void PlayDefeat() { Play(DefeatClip, DefeatScale, 1f); }

    private void Play(AudioClip clip, float scale, float pitch)
    {
        if (clip == null || _sfxSource == null) return;

        _sfxSource.pitch = pitch;
        _sfxSource.PlayOneShot(clip, Mathf.Clamp01(SfxVolume * scale));
    }

    // =====================================================================
    //  背景音乐
    // =====================================================================

    /// <summary>播放 BGM（会切换曲目）。</summary>
    public void PlayBgm(AudioClip clip)
    {
        if (clip == null || _bgmSource == null) return;
        if (_bgmSource.clip == clip && _bgmSource.isPlaying) return;   // 同一首就别重播

        BgmClip = clip;
        _bgmSource.clip = clip;
        _bgmSource.volume = BgmVolume;
        _bgmSource.Play();
    }

    /// <summary>停止 BGM。</summary>
    public void StopBgm()
    {
        if (_bgmSource != null) _bgmSource.Stop();
    }

    // =====================================================================
    //  音量设置（给设置界面调用）
    // =====================================================================

    public void SetSfxVolume(float value)
    {
        SfxVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(KEY_VOLUME, SfxVolume);
        PlayerPrefs.Save();
    }

    public void SetBgmVolume(float value)
    {
        BgmVolume = Mathf.Clamp01(value);
        if (_bgmSource != null) _bgmSource.volume = BgmVolume;
        PlayerPrefs.SetFloat(KEY_BGM_VOLUME, BgmVolume);
        PlayerPrefs.Save();
    }
}