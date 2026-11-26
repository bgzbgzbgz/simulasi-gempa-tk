using System.Collections;
using UnityEngine;

/// <summary>
/// =========================================================================================
/// VOICE OVER MANAGER - PUSAT PENGATURAN SUARA BU GURU (MITIGASI GEMPA TK)
/// =========================================================================================
/// 
/// CARA MENGGANTI ATAU MENYESUAIKAN FILE VOICE OVER (VO):
/// -----------------------------------------------------------------------------------------
/// 1. CARA 1 (DRAG & DROP DI INSPECTOR - PALING MUDAH):
///    - Klik GameObject "VoiceOverManager" (atau "SoundManager") di Hierarchy Unity.
///    - Lihat Inspector pada komponen VoiceOverManager.
///    - Seret (drag & drop) file audio baru (.mp3 atau .wav) dari Project Window langsung
///      ke slot yang ingin diganti (contoh: slot "VO 07: Gempa Mulai").
/// 
/// 2. CARA 2 (GANTI VARIAN 1-KLIK: REGULER VS SLOW):
///    - Klik kanan pada judul script "VoiceOverManager" di Inspector.
///    - Pilih "Muat Varian Slow (Tempo Ramah Anak TK)" untuk suara lebih pelan/santai,
///      atau pilih "Muat Varian Reguler (Default)" untuk kembali ke tempo normal.
/// 
/// 3. CARA 3 (TIMPA FILE FISIK):
///    - Ganti langsung file di folder "Assets/Audio/VoiceOver/" dengan nama yang sama persis.
/// =========================================================================================
/// </summary>
public class VoiceOverManager : MonoBehaviour
{
    private static VoiceOverManager _instance;
    public static VoiceOverManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<VoiceOverManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("VoiceOverManager");
                    _instance = go.AddComponent<VoiceOverManager>();
                }
            }
            return _instance;
        }
    }

    public enum VoiceVariant
    {
        [Tooltip("Tempo bicara standar/normal.")]
        Regular,
        [Tooltip("Tempo bicara lebih lambat dan artikulatif, direkomendasikan untuk anak usia 5-6 tahun.")]
        Slow
    }

    [Header("=== PENGATURAN UMUM VOICE OVER ===")]
    [Tooltip("Pilihan varian suara yang sedang aktif.")]
    public VoiceVariant currentVariant = VoiceVariant.Regular;

    [Tooltip("Volume suara Bu Guru (0 = hening, 1 = maksimal).")]
    [Range(0f, 1f)] public float voVolume = 1.0f;

    [Tooltip("Jika aktif, volume suara gemuruh gempa dan sirine otomatis mengecil saat Bu Guru sedang berbicara.")]
    public bool enableAudioDucking = true;

    [Header("=== LEVEL 1: KENALI BENCANA (KUIS TV) ===")]
    [Tooltip("VO_02: Pembukaan Level 1 Kuis TV")]
    public AudioClip vo02_Level1Intro;

    [Tooltip("VO_03: Pertanyaan Soal 1 Visual Rumah Gempa")]
    public AudioClip vo03_Soal1;

    [Tooltip("VO_04: Pertanyaan Soal 2 Audio Gempa Bumi")]
    public AudioClip vo04_Soal2;

    [Tooltip("VO_05: Pujian saat jawaban kuis BENAR")]
    public AudioClip vo05_FeedbackBenar;

    [Tooltip("VO_06: Dorongan semangat saat jawaban kuis SALAH")]
    public AudioClip vo06_FeedbackSalah;

    [Header("=== LEVEL 2: DROP, COVER, HOLD ON ===")]
    [Tooltip("VO_07: Peringatan gempa terjadi, segera cari meja dan merunduk!")]
    public AudioClip vo07_Level2GempaMulai;

    [Tooltip("VO_08: Pujian saat murid sudah berada di kolong meja dan menunduk.")]
    public AudioClip vo08_Level2MasukKolong;

    [Header("=== LEVEL 3: EVAKUASI & SETELAHNYA ===")]
    [Tooltip("VO_09a: Gempa reda, arahan berdiri tertib ikuti jalur hijau.")]
    public AudioClip vo09a_Level3Mulai;

    [Tooltip("VO_09b: Peringatan gempa susulan kecil saat melewati lorong sekolah.")]
    public AudioClip vo09b_Level3GempaSusulan;

    [Tooltip("VO_10: Pujian saat tiba dengan selamat di Titik Kumpul lapangan.")]
    public AudioClip vo10_Level3Selesai;

    [Header("=== MENU UTAMA & SKOR AKHIR ===")]
    [Tooltip("VO_01: Salam pembuka di Menu Utama / awal simulasi.")]
    public AudioClip vo01_Intro;

    [Tooltip("VO_11: Penutupan saat pengumuman skor bintang akhir.")]
    public AudioClip vo11_EndingBintang;

    [Header("=== FILE TAMBAHAN / OPSIONAL ===")]
    public AudioClip voNamaKamuSiapa;
    public AudioClip voSiapaNamamu;
    public AudioClip voKarakter;
    public AudioClip voRevisi1;
    public AudioClip voRevisi2;

    [Header("Komponen Audio")]
    [SerializeField] private AudioSource voSource;

    private Coroutine duckingCoroutine;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureAudioSource();

#if UNITY_EDITOR
        AutoLoadClipsIfEmpty();
#endif
    }

    private void EnsureAudioSource()
    {
        if (voSource == null)
        {
            voSource = GetComponent<AudioSource>();
            if (voSource == null)
            {
                voSource = gameObject.AddComponent<AudioSource>();
            }
            voSource.playOnAwake = false;
            voSource.spatialBlend = 0f; // 2D Sound agar terdengar merata dan jernih
            voSource.volume = voVolume;
        }
    }

    public void PlayVO(AudioClip clip, bool duckBackground = true)
    {
        if (clip == null)
        {
            Debug.LogWarning("<color=yellow>[VoiceOverManager]</color> AudioClip kosong / belum di-assign!");
            return;
        }

        // Cek pengaturan toggle VoiceOver dari Main Menu
        if (PlayerPrefs.GetInt("VoiceOver_Enabled", 1) == 0)
        {
            Debug.Log("<color=grey>[VoiceOverManager]</color> VO dilewati karena dinonaktifkan di menu Pengaturan.");
            return;
        }

        EnsureAudioSource();
        voSource.Stop();
        voSource.clip = clip;
        voSource.volume = voVolume;
        voSource.Play();
        Debug.Log($"<color=cyan>[VoiceOverManager]</color> ▶ <b>MEMUTAR SUARA BU GURU:</b> {clip.name} (Volume: {Mathf.RoundToInt(voVolume * 100)}%)");

        if (duckBackground && enableAudioDucking)
        {
            if (duckingCoroutine != null) StopCoroutine(duckingCoroutine);
            duckingCoroutine = StartCoroutine(AudioDuckingRoutine(clip.length));
        }
    }

    public void StopVO()
    {
        if (voSource != null && voSource.isPlaying)
        {
            voSource.Stop();
        }
        if (duckingCoroutine != null)
        {
            StopCoroutine(duckingCoroutine);
            duckingCoroutine = null;
        }
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.SetDucking(false);
        }
    }

    private IEnumerator AudioDuckingRoutine(float duration)
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.SetDucking(true);
        }

        yield return new WaitForSeconds(duration + 0.35f);

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.SetDucking(false);
        }
        duckingCoroutine = null;
    }

    // =========================================================================
    // METODE PEMANGGILAN KHUSUS TIAP MOMEN GAMEPLAY
    // =========================================================================

    public void PlayVO_01_Intro() => PlayVO(vo01_Intro);
    public void PlayVO_02_Level1Intro() => PlayVO(vo02_Level1Intro);
    public void PlayVO_03_Soal1() => PlayVO(vo03_Soal1);
    public void PlayVO_04_Soal2() => PlayVO(vo04_Soal2);
    public void PlayVO_05_FeedbackBenar() => PlayVO(vo05_FeedbackBenar, duckBackground: false);
    public void PlayVO_06_FeedbackSalah() => PlayVO(vo06_FeedbackSalah, duckBackground: false);
    public void PlayVO_07_Level2GempaMulai() => PlayVO(vo07_Level2GempaMulai);
    public void PlayVO_08_Level2MasukKolong() => PlayVO(vo08_Level2MasukKolong);
    public void PlayVO_09a_Level3Mulai() => PlayVO(vo09a_Level3Mulai);
    public void PlayVO_09b_Level3GempaSusulan() => PlayVO(vo09b_Level3GempaSusulan);
    public void PlayVO_10_Level3Selesai() => PlayVO(vo10_Level3Selesai);
    public void PlayVO_11_EndingBintang() => PlayVO(vo11_EndingBintang);

#if UNITY_EDITOR
    private void AutoLoadClipsIfEmpty()
    {
        if (vo01_Intro == null || vo07_Level2GempaMulai == null)
        {
            if (currentVariant == VoiceVariant.Slow)
                LoadSlowClips();
            else
                LoadRegularClips();
        }
    }

    [ContextMenu("1. Muat Varian Reguler (Default)")]
    public void LoadRegularClips()
    {
        currentVariant = VoiceVariant.Regular;
        vo01_Intro = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/VoiceOver/VO_01_Intro.mp3");
        vo02_Level1Intro = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/VoiceOver/VO_02_Level1_Intro.mp3");
        vo03_Soal1 = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/VoiceOver/VO_03_Soal1.mp3");
        vo04_Soal2 = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/VoiceOver/VO_04_Soal2.mp3");
        vo05_FeedbackBenar = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/VoiceOver/VO_05_Feedback_Benar.mp3");
        vo06_FeedbackSalah = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/VoiceOver/VO_06_Feedback_Salah.mp3");
        vo07_Level2GempaMulai = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/VoiceOver/VO_07_Level2_GempaMulai.mp3");
        vo08_Level2MasukKolong = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/VoiceOver/VO_08_Level2_MasukKolong.mp3");
        vo09a_Level3Mulai = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/VoiceOver/VO_09a_Level3_Mulai.mp3");
        vo09b_Level3GempaSusulan = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/VoiceOver/VO_09b_Level3_GempaSusulan.mp3");
        vo10_Level3Selesai = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/VoiceOver/VO_10_Level3_Selesai.mp3");
        vo11_EndingBintang = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/VoiceOver/VO_11_Ending_Bintang.mp3");

        voNamaKamuSiapa = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/VoiceOver/namamusiapa.wav");
        voSiapaNamamu = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/VoiceOver/siapanamamu.wav");
        voKarakter = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/VoiceOver/karakter.wav");
        voRevisi1 = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/VoiceOver/Revisi 1.mp3");
        voRevisi2 = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/VoiceOver/Revisi 2.mp3");

        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log("[VoiceOverManager] Berhasil memuat seluruh Varian REGULER!");
    }

    [ContextMenu("2. Muat Varian Slow (Tempo Ramah Anak TK)")]
    public void LoadSlowClips()
    {
        currentVariant = VoiceVariant.Slow;
        vo01_Intro = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/VoiceOver/VO_01_Intro_slow.mp3") ?? vo01_Intro;
        vo02_Level1Intro = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/VoiceOver/VO_02_Level1_Intro_slow.mp3") ?? vo02_Level1Intro;
        vo03_Soal1 = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/VoiceOver/VO_03_Soal1_slow.mp3") ?? vo03_Soal1;
        vo04_Soal2 = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/VoiceOver/VO_04_Soal2_slow.mp3") ?? vo04_Soal2;
        vo05_FeedbackBenar = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/VoiceOver/VO_05_Feedback_Benar.mp3");
        vo06_FeedbackSalah = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/VoiceOver/VO_06_Feedback_Salah.mp3");
        vo07_Level2GempaMulai = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/VoiceOver/VO_07_Level2_GempaMulai.mp3");
        vo08_Level2MasukKolong = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/VoiceOver/VO_08_Level2_MasukKolong.mp3");
        vo09a_Level3Mulai = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/VoiceOver/VO_09a_Level3_Mulai.mp3");
        vo09b_Level3GempaSusulan = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/VoiceOver/VO_09b_Level3_GempaSusulan_slow.mp3") ?? vo09b_Level3GempaSusulan;
        vo10_Level3Selesai = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/VoiceOver/VO_10_Level3_Selesai.mp3");
        vo11_EndingBintang = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/VoiceOver/VO_11_Ending_Bintang.mp3");

        voNamaKamuSiapa = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/VoiceOver/namamusiapa.wav");
        voSiapaNamamu = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/VoiceOver/siapanamamu.wav");
        voKarakter = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/VoiceOver/karakter.wav");

        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log("[VoiceOverManager] Berhasil memuat seluruh Varian SLOW!");
    }
#endif
}
