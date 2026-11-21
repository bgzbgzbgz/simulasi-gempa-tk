using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Efek gempa selama Level 2 (Drop & Cover) dan gempa susulan Level 3: kamera bergetar, layar berkedip, suara gemuruh, dan benda jatuh.
/// Aktif otomatis saat GameManager berada di level gempa dan game berjalan; berhenti saat level itu selesai.
/// </summary>
[DefaultExecutionOrder(100)] // Berjalan setelah ThirdPersonCamera, supaya guncangan ditambahkan di atas posisi kamera
public class EarthquakeEffect : MonoBehaviour
{
    public static EarthquakeEffect Instance { get; private set; }

    [Header("Level Gempa")]
    [Tooltip("Gempa terjadi selama level ini aktif (Level 2 = Drop & Cover).")]
    [SerializeField] private int quakeLevel = 2;

    [Header("Guncangan Kamera")]
    [Tooltip("Kamera yang diguncang. Kosongkan untuk memakai Main Camera.")]
    [SerializeField] private Transform cameraTransform;

    [Tooltip("Besar guncangan (meter). Jaga tetap kecil supaya anak tidak pusing.")]
    [Range(0.005f, 0.25f)] public float shakeAmplitude = 0.04f;

    [Tooltip("Kecepatan guncangan.")]
    [Range(5f, 35f)] public float shakeFrequency = 18f;

    [Header("Gempa Susulan (Level 3)")]
    [Tooltip("Besar guncangan kamera default saat gempa susulan.")]
    [Range(0.005f, 0.2f)] public float aftershockAmplitude = 0.03f;

    public void SetShakeAmplitude(float val)
    {
        shakeAmplitude = Mathf.Clamp(val, 0.005f, 0.25f);
    }

    [Header("Overlay Layar (opsional)")]
    [Tooltip("Image layar penuh yang berkedip saat gempa. Boleh kosong.")]
    [SerializeField] private Image overlayImage;

    [Tooltip("Transparansi maksimum overlay saat berkedip.")]
    [SerializeField, Range(0f, 1f)] private float overlayMaxAlpha = 0.25f;

    [Header("Suara (opsional)")]
    [Tooltip("AudioSource berisi suara gemuruh gempa. Diputar berulang selama gempa. Boleh kosong.")]
    [SerializeField] private AudioSource rumbleAudio;

    [Header("Benda Jatuh (opsional)")]
    [Tooltip("Rigidbody benda yang jatuh saat gempa mulai. Centang Is Kinematic di Inspector supaya diam sebelum gempa.")]
    [SerializeField] private Rigidbody[] fallingObjects;

    private Vector3 lastOffset;
    private bool isShaking;
    private bool objectsReleased;
    private float noiseSeed;
    private Coroutine aftershockCoroutine;
    private float customShakeAmplitude = -1f;

    public bool IsShaking => isShaking;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        noiseSeed = Random.value * 100f;

        if (overlayImage != null)
        {
            overlayImage.raycastTarget = false;
            overlayImage.enabled = false;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
        // Buang guncangan frame lalu, supaya ThirdPersonCamera menghitung dari posisi kamera yang asli
        RemoveLastOffset();

        GameManager gm = GameManager.Instance;
        bool shouldShake = (gm != null && gm.isGameActive && gm.currentLevel == quakeLevel) || (aftershockCoroutine != null);
        if (shouldShake != isShaking)
        {
            SetShaking(shouldShake);
        }

        if (isShaking && overlayImage != null)
        {
            Color c = overlayImage.color;
            float maxAlpha = (aftershockCoroutine != null) ? overlayMaxAlpha * 0.5f : overlayMaxAlpha;
            c.a = maxAlpha * (0.5f + 0.5f * Mathf.Sin(Time.time * 6f));
            overlayImage.color = c;
        }
    }

    private void LateUpdate()
    {
        // Hindari guncangan ganda: Jika ThirdPersonCamera sudah menangani guncangan kamera (translasi + rotasi roll/pitch),
        // serahkan getaran kamera ke ThirdPersonCamera agar gerakan tetap mulus dan tersinkron.
        ThirdPersonCamera tpc = ThirdPersonCamera.Instance ?? (Camera.main != null ? Camera.main.GetComponent<ThirdPersonCamera>() : null);
        if (tpc != null && tpc.enableEarthquakeShake)
        {
            return;
        }

        if (!isShaking || cameraTransform == null)
        {
            return;
        }

        float amp = (customShakeAmplitude >= 0f) ? customShakeAmplitude : shakeAmplitude;
        float t = Time.time * shakeFrequency;
        Vector3 offset = new Vector3(
            Mathf.PerlinNoise(noiseSeed, t) - 0.5f,
            Mathf.PerlinNoise(noiseSeed + 10f, t) - 0.5f,
            Mathf.PerlinNoise(noiseSeed + 20f, t) - 0.5f) * (2f * amp);

        cameraTransform.position += offset;
        lastOffset = offset;
    }

    private void OnDisable()
    {
        RemoveLastOffset();
        if (isShaking)
        {
            SetShaking(false);
        }
    }

    /// <summary>
    /// Memicu gempa susulan sementara (misal di sela navigasi evakuasi Level 3).
    /// </summary>
    public void TriggerAftershock(float duration = 5f, float amplitude = -1f)
    {
        if (!gameObject.activeInHierarchy) return;

        if (aftershockCoroutine != null)
        {
            StopCoroutine(aftershockCoroutine);
        }
        aftershockCoroutine = StartCoroutine(AftershockRoutine(duration, amplitude > 0f ? amplitude : aftershockAmplitude));
    }

    private System.Collections.IEnumerator AftershockRoutine(float duration, float amplitude)
    {
        customShakeAmplitude = amplitude;
        SetShaking(true);

#if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
#endif

        yield return new WaitForSeconds(duration);

        customShakeAmplitude = -1f;
        aftershockCoroutine = null;
        SetShaking(false);
    }

    private void SetShaking(bool shaking)
    {
        isShaking = shaking;

        if (overlayImage != null)
        {
            overlayImage.enabled = shaking;
        }

        if (rumbleAudio != null)
        {
            if (shaking)
            {
                rumbleAudio.loop = true;
                if (!rumbleAudio.isPlaying) rumbleAudio.Play();
            }
            else
            {
                rumbleAudio.Stop();
            }
        }

        if (shaking && !objectsReleased && fallingObjects != null && aftershockCoroutine == null)
        {
            objectsReleased = true;
            foreach (Rigidbody rb in fallingObjects)
            {
                if (rb == null) continue;
                rb.isKinematic = false;
                rb.WakeUp();
            }
        }
    }

    private void RemoveLastOffset()
    {
        if (cameraTransform != null && lastOffset != Vector3.zero)
        {
            cameraTransform.position -= lastOffset;
        }
        lastOffset = Vector3.zero;
    }
}
