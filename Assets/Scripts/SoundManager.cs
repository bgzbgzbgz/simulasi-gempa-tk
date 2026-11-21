using UnityEngine;

/// <summary>
/// Mengelola pemutaran seluruh Sound Effect (SFX) simulasi edukasi gempa.
/// Singleton global yang dapat dipanggil dari mana saja (GameManager, BagPickup, EarthquakeEffect, MissionUI).
/// </summary>
public class SoundManager : MonoBehaviour
{
    private static SoundManager _instance;
    public static SoundManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<SoundManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("SoundManager");
                    _instance = go.AddComponent<SoundManager>();
                }
            }
            return _instance;
        }
    }

    [Header("Audio Clips")]
    public AudioClip sfxAlarmSiren;
    public AudioClip sfxBagPickup;
    public AudioClip sfxButtonClick;
    public AudioClip sfxEarthquakeRumble;
    public AudioClip sfxGameWin;
    public AudioClip sfxLevelComplete;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource loopQuakeSource;
    [SerializeField] private AudioSource loopSirenSource;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureAudioSources();
        AutoLoadClipsIfMissing();
    }

    private void EnsureAudioSources()
    {
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
        }
        if (loopQuakeSource == null)
        {
            loopQuakeSource = gameObject.AddComponent<AudioSource>();
            loopQuakeSource.playOnAwake = false;
            loopQuakeSource.loop = true;
            loopQuakeSource.volume = 0.65f;
        }
        if (loopSirenSource == null)
        {
            loopSirenSource = gameObject.AddComponent<AudioSource>();
            loopSirenSource.playOnAwake = false;
            loopSirenSource.loop = true;
            loopSirenSource.volume = 0.45f;
        }
    }

    private void AutoLoadClipsIfMissing()
    {
#if UNITY_EDITOR
        if (sfxBagPickup == null) sfxBagPickup = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/sfx_bag_pickup.wav");
        if (sfxLevelComplete == null) sfxLevelComplete = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/sfx_level_complete.wav");
        if (sfxGameWin == null) sfxGameWin = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/sfx_game_win.wav");
        if (sfxButtonClick == null) sfxButtonClick = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/sfx_button_click.wav");
        if (sfxEarthquakeRumble == null) sfxEarthquakeRumble = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/sfx_earthquake_rumble.wav");
        if (sfxAlarmSiren == null) sfxAlarmSiren = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/sfx_alarm_siren.wav");
#endif
    }

    public void PlayButtonClick()
    {
        PlayOneShot(sfxButtonClick, 0.7f);
    }

    public void PlayBagPickup()
    {
        PlayOneShot(sfxBagPickup, 0.9f);
    }

    public void PlayLevelComplete()
    {
        PlayOneShot(sfxLevelComplete, 0.85f);
    }

    public void PlayGameWin()
    {
        PlayOneShot(sfxGameWin, 1.0f);
    }

    public void StartEarthquake()
    {
        EnsureAudioSources();
        if (sfxEarthquakeRumble != null)
        {
            loopQuakeSource.clip = sfxEarthquakeRumble;
            if (!loopQuakeSource.isPlaying) loopQuakeSource.Play();
        }
        if (sfxAlarmSiren != null)
        {
            loopSirenSource.clip = sfxAlarmSiren;
            if (!loopSirenSource.isPlaying) loopSirenSource.Play();
        }
    }

    public void StopEarthquake()
    {
        if (loopQuakeSource != null && loopQuakeSource.isPlaying) loopQuakeSource.Stop();
        if (loopSirenSource != null && loopSirenSource.isPlaying) loopSirenSource.Stop();
    }

    private void PlayOneShot(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;
        EnsureAudioSources();
        sfxSource.PlayOneShot(clip, volume);
    }
}
