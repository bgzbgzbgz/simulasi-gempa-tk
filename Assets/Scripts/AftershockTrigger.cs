using System.Collections;
using UnityEngine;

/// <summary>
/// Trigger gempa susulan (aftershock) di sela-sela rute navigasi evakuasi Level 3.
/// Bisa dipasang langsung pada Waypoint (misal WP_03_PintuKelas_Dalam) atau sebagai objek trigger mandiri.
/// Mendukung deteksi berbasis radius jarak (sangat andal untuk Waypoint) dan/atau Collider Trigger.
/// Memutar Voice Over (VO_09b), mengaktifkan guncangan kamera ringan, getaran HP, dan mengupdate instruksi HUD.
/// </summary>
public class AftershockTrigger : MonoBehaviour
{
    [Header("Target Level")]
    [Tooltip("Level di mana trigger ini aktif (default: Level 3 evakuasi)")]
    public int targetLevel = 3;

    [Tooltip("Jika dicentang, trigger hanya menyala saat game aktif di Level 3. Matikan (uncheck) jika ingin langsung tes jalan ke pintu saat Play mode!")]
    public bool onlyActiveInLevel3 = false;

    [Header("Mode Deteksi Jarak (Waypoint)")]
    [Tooltip("Jika aktif, gempa susulan akan otomatis terpicu saat pemain berada dalam radius jarak ini (sangat andal bila ditempel di Waypoint!).")]
    public bool useDistanceCheck = true;

    [Tooltip("Jarak deteksi pemain (meter) jika ditempel pada Waypoint.")]
    [SerializeField, Min(0.5f)] public float triggerRadius = 2.5f;

    [Header("Pengaturan Gempa Susulan")]
    [Tooltip("Durasi guncangan gempa susulan (detik)")]
    public float aftershockDuration = 5f;

    [Tooltip("Kekuatan guncangan kamera (default: 0.08f, terasa nyata seperti gempa utama tapi lebih ringan)")]
    [Range(0.01f, 0.25f)] public float shakeAmplitude = 0.08f;

    [Header("Audio & Voice Over")]
    [Tooltip("Klip audio VO_09b_Level3_GempaSusulan.wav")]
    public AudioClip voiceOverClip;

    [Tooltip("AudioSource untuk memutar VO. Jika kosong, akan otomatis mengambil atau menambahkan AudioSource di objek ini.")]
    public AudioSource voiceAudioSource;

    [Header("Teks Instruksi HUD")]
    [TextArea(2, 4)]
    public string aftershockInstruction = "Awas anak-anak, ada gempa susulan kecil! Jangan panik, tetap lindungi kepalamu dan jalan cepat menuju lapangan terbuka!";

    [TextArea(2, 4)]
    public string resumeInstruction = "Alhamdulillah, gempa susulan reda! Lanjutkan evakuasi menuju Titik Kumpul di lapangan terbuka!";

    [Header("Visual Gizmo Editor")]
    public Color gizmoColor = new Color(1f, 0.6f, 0.1f, 0.35f);

    private bool hasTriggered = false;
    private Transform playerTransform;

    private void Awake()
    {
        // Jika objek ini memiliki Collider, pastikan isTrigger aktif
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }

        if (voiceAudioSource == null)
        {
            voiceAudioSource = GetComponent<AudioSource>();
        }
    }

    private void Start()
    {
        FindPlayer();
    }

    private void FindPlayer()
    {
        if (playerTransform == null)
        {
            BoyController boy = Object.FindFirstObjectByType<BoyController>();
            if (boy != null)
            {
                playerTransform = boy.transform;
            }
            else if (Camera.main != null)
            {
                playerTransform = Camera.main.transform;
            }
        }
    }

    private bool IsLevelAllowed()
    {
        if (!onlyActiveInLevel3) return true;
        GameManager gm = GameManager.Instance;
        if (gm == null) return true;
        return gm.currentLevel == targetLevel;
    }

    private void Update()
    {
        if (hasTriggered) return;

        if (!IsLevelAllowed())
        {
            return;
        }

        // Deteksi jarak (Distance Check) - sangat andal saat ditempelkan ke Waypoint
        if (useDistanceCheck)
        {
            if (playerTransform == null)
            {
                FindPlayer();
            }

            if (playerTransform != null)
            {
                // Hitung jarak horizontal 2D (X, Z) agar ketinggian lantai/tangga tidak mengacaukan deteksi
                Vector2 p2D = new Vector2(playerTransform.position.x, playerTransform.position.z);
                Vector2 my2D = new Vector2(transform.position.x, transform.position.z);

                if (Vector2.Distance(p2D, my2D) <= triggerRadius)
                {
                    TriggerAftershockSequence();
                }
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered) return;

        if (!IsLevelAllowed())
        {
            return;
        }

        BoyController boy = other.GetComponent<BoyController>() ?? other.GetComponentInParent<BoyController>();
        if (boy != null)
        {
            TriggerAftershockSequence();
        }
    }

    public void TriggerAftershockSequence()
    {
        if (hasTriggered) return;
        hasTriggered = true;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartCoroutine(ExecuteAftershock());
        }
        else
        {
            StartCoroutine(ExecuteAftershock());
        }
    }

    private IEnumerator ExecuteAftershock()
    {
        Debug.Log($"<color=orange>[AftershockTrigger]</color> GEMPA SUSULAN TERPICU! (Durasi: {aftershockDuration}s, Amplitudo: {shakeAmplitude})");

        // 1. Suara Gemuruh Gempa (SoundManager) - PERSIS SEPERTI GEMPA UTAMA LEVEL 2!
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.StartEarthquake();
        }

        // 2. Guncangan Kamera (ThirdPersonCamera) - PERSIS SEPERTI GEMPA UTAMA LEVEL 2!
        ThirdPersonCamera cam = ThirdPersonCamera.Instance ?? Object.FindFirstObjectByType<ThirdPersonCamera>();
        if (cam != null)
        {
            cam.TriggerAftershock(aftershockDuration, shakeAmplitude);
        }
        else
        {
            EarthquakeEffect quake = EarthquakeEffect.Instance ?? Object.FindFirstObjectByType<EarthquakeEffect>();
            if (quake != null)
            {
                quake.TriggerAftershock(aftershockDuration, shakeAmplitude);
            }
        }

        // 3. Putar Voice Over jika ada
        if (voiceOverClip != null)
        {
            if (voiceAudioSource == null)
            {
                voiceAudioSource = gameObject.AddComponent<AudioSource>();
            }
            voiceAudioSource.PlayOneShot(voiceOverClip);
        }

        // 4. Perbarui instruksi di HUD MissionUI
        if (GameManager.Instance != null && GameManager.Instance.missionUI != null)
        {
            GameManager.Instance.missionUI.SetInstruction(aftershockInstruction);
        }

#if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
#endif

        // Tunggu hingga guncangan gempa susulan reda
        yield return new WaitForSeconds(aftershockDuration);

        // Hentikan suara gemuruh gempa - PERSIS SEPERTI GEMPA UTAMA LEVEL 2!
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.StopEarthquake();
        }

        Debug.Log("<color=green>[AftershockTrigger]</color> Gempa susulan reda, lanjutkan evakuasi.");

        // 5. Kembalikan instruksi evakuasi jika level 3 masih berjalan
        if (GameManager.Instance != null && GameManager.Instance.isGameActive)
        {
            if (GameManager.Instance.missionUI != null)
            {
                GameManager.Instance.missionUI.SetInstruction(resumeInstruction);
            }
        }
    }

    private void OnDrawGizmos()
    {
        Collider col = GetComponent<Collider>();
        if (col is BoxCollider box)
        {
            Gizmos.color = gizmoColor;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.center, box.size);
            Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.9f);
            Gizmos.DrawWireCube(box.center, box.size);
        }
        else if (useDistanceCheck)
        {
            // Tampilan visual radius bola jika ditempel di Waypoint
            Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.25f);
            Gizmos.DrawSphere(transform.position, triggerRadius);
            Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.9f);
            Gizmos.DrawWireSphere(transform.position, triggerRadius);
        }
    }
}
