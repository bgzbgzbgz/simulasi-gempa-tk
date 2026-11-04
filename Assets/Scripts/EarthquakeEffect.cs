using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Efek gempa selama Level 2 (Drop & Cover): kamera bergetar, layar berkedip, suara gemuruh, dan benda jatuh.
/// Aktif otomatis saat GameManager berada di level gempa dan game berjalan; berhenti saat level itu selesai.
/// </summary>
[DefaultExecutionOrder(100)] // Berjalan setelah ThirdPersonCamera, supaya guncangan ditambahkan di atas posisi kamera
public class EarthquakeEffect : MonoBehaviour
{
    [Header("Level Gempa")]
    [Tooltip("Gempa terjadi selama level ini aktif (Level 2 = Drop & Cover).")]
    [SerializeField] private int quakeLevel = 2;

    [Header("Guncangan Kamera")]
    [Tooltip("Kamera yang diguncang. Kosongkan untuk memakai Main Camera.")]
    [SerializeField] private Transform cameraTransform;

    [Tooltip("Besar guncangan (meter). Jaga tetap kecil supaya anak tidak pusing.")]
    [SerializeField, Range(0f, 0.3f)] private float shakeAmplitude = 0.08f;

    [Tooltip("Kecepatan guncangan.")]
    [SerializeField, Min(0f)] private float shakeFrequency = 18f;

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

    public bool IsShaking => isShaking;

    private void Awake()
    {
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

    private void Update()
    {
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
        // Buang guncangan frame lalu, supaya ThirdPersonCamera menghitung dari posisi kamera yang asli
        RemoveLastOffset();

        GameManager gm = GameManager.Instance;
        bool shouldShake = gm != null && gm.isGameActive && gm.currentLevel == quakeLevel;
        if (shouldShake != isShaking)
        {
            SetShaking(shouldShake);
        }

        if (isShaking && overlayImage != null)
        {
            Color c = overlayImage.color;
            c.a = overlayMaxAlpha * (0.5f + 0.5f * Mathf.Sin(Time.time * 6f));
            overlayImage.color = c;
        }
    }

    private void LateUpdate()
    {
        if (!isShaking || cameraTransform == null)
        {
            return;
        }

        float t = Time.time * shakeFrequency;
        Vector3 offset = new Vector3(
            Mathf.PerlinNoise(noiseSeed, t) - 0.5f,
            Mathf.PerlinNoise(noiseSeed + 10f, t) - 0.5f,
            Mathf.PerlinNoise(noiseSeed + 20f, t) - 0.5f) * (2f * shakeAmplitude);

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

        if (shaking && !objectsReleased && fallingObjects != null)
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

