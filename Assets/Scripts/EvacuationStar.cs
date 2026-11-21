using System.Collections;
using UnityEngine;

/// <summary>
/// Bintang kecil bercahaya penuntun jalur evakuasi Level 3.
/// Mengapung lembut, berputar perlahan, dan memancarkan cahaya hangat ke lantai & dinding
/// agar anak-anak TK tertarik untuk mendekat dan mengikuti jalur evakuasi.
/// </summary>
public class EvacuationStar : MonoBehaviour
{
    [Header("Pengaturan Bintang")]
    [Tooltip("Jarak deteksi pemain menyentuh/mengambil bintang (meter).")]
    [SerializeField] private float pickupRadius = 1.4f;

    [Tooltip("Kecepatan berputar (derajat per detik).")]
    [SerializeField] private float rotationSpeed = 80f;

    [Tooltip("Tinggi ayunan mengapung naik-turun (meter).")]
    [SerializeField] private float bobHeight = 0.12f;

    [Tooltip("Kecepatan ayunan mengapung.")]
    [SerializeField] private float bobSpeed = 2.8f;

    [Tooltip("Audio clip saat bintang disentuh/dilewati.")]
    [SerializeField] private AudioClip collectSound;

    [Header("Komponen Cahaya")]
    [SerializeField] private Light starLight;

    [SerializeField] private Vector3 initialPos;
    private float timeOffset;
    private bool isCollected = false;
    private Transform playerTransform;

    public void SetAnchorPosition(Vector3 pos)
    {
        initialPos = pos;
        transform.position = pos;
    }

    private void Awake()
    {
        if (initialPos == Vector3.zero && transform.position != Vector3.zero)
        {
            initialPos = transform.position;
        }
        timeOffset = Random.Range(0f, Mathf.PI * 2f);

        if (starLight == null)
        {
            starLight = GetComponentInChildren<Light>();
        }

        if (collectSound == null)
        {
#if UNITY_EDITOR
            collectSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/sfx_bag_pickup.wav");
#endif
        }
    }

    private void OnEnable()
    {
        isCollected = false;
        transform.localScale = Vector3.one;
        if (starLight != null) starLight.enabled = true;
    }

    private void Update()
    {
        if (isCollected) return;

        // 1. Ayunan mengapung naik-turun (bobbing)
        float newY = initialPos.y + Mathf.Sin((Time.time + timeOffset) * bobSpeed) * bobHeight;
        transform.position = new Vector3(initialPos.x, newY, initialPos.z);

        // 2. Berputar perlahan di sumbu tegak (Y)
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);

        // 3. Efek cahaya bernapas lembut (pulsating light)
        if (starLight != null)
        {
            starLight.intensity = 1.4f + Mathf.PingPong(Time.time * 1.8f, 0.6f);
        }

        // 4. Deteksi kedekatan pemain
        if (playerTransform == null)
        {
            BoyController boy = FindFirstObjectByType<BoyController>();
            if (boy != null) playerTransform = boy.transform;
            else if (Camera.main != null) playerTransform = Camera.main.transform;
        }

        if (playerTransform != null)
        {
            Vector3 diff = playerTransform.position - transform.position;
            diff.y = 0f; // Hanya jarak horizontal lantai
            if (diff.sqrMagnitude <= pickupRadius * pickupRadius)
            {
                Collect();
            }
        }
    }

    /// <summary>
    /// Dipanggil saat pemain menyentuh atau melewati bintang.
    /// </summary>
    public void Collect()
    {
        if (isCollected) return;
        isCollected = true;

        // Pemicu gempa susulan jika bintang ini ditempeli AftershockTrigger
        AftershockTrigger aftershock = GetComponent<AftershockTrigger>();
        if (aftershock != null)
        {
            aftershock.TriggerAftershockSequence();
        }

        if (collectSound != null)
        {
            AudioSource.PlayClipAtPoint(collectSound, transform.position, 0.9f);
        }

        StartCoroutine(PopAndDisable());
    }

    public void ResetStar()
    {
        StopAllCoroutines();
        isCollected = false;
        if (initialPos != Vector3.zero)
        {
            transform.position = initialPos;
        }
        transform.localScale = Vector3.one;
        if (starLight != null) starLight.enabled = true;
        gameObject.SetActive(true);
    }

    private IEnumerator PopAndDisable()
    {
        Vector3 startScale = transform.localScale;
        Vector3 popScale = startScale * 1.35f;
        float elapsed = 0f;
        float popDuration = 0.12f;
        float shrinkDuration = 0.18f;

        // Fase 1: Pop membesar singkat
        while (elapsed < popDuration)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(startScale, popScale, elapsed / popDuration);
            yield return null;
        }

        // Fase 2: Menyusut cepat dan menghilang
        elapsed = 0f;
        while (elapsed < shrinkDuration)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(popScale, Vector3.zero, elapsed / shrinkDuration);
            yield return null;
        }

        if (starLight != null) starLight.enabled = false;
        gameObject.SetActive(false);
    }
}
