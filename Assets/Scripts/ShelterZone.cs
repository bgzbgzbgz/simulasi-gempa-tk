using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Trigger zona perlindungan (kolong meja) untuk Level 2 (Drop, Cover, Hold On).
/// Mendeteksi saat karakter masuk ke bawah meja dan memberikan feedback visual/event.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ShelterZone : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private string zoneName = "Kolong Meja Siswa";
    [SerializeField] private bool showDebugGUI = true;

    [Header("Events")]
    public UnityEvent onEnterShelter;
    public UnityEvent onExitShelter;

    /// <summary>
    /// Menandakan apakah karakter pemain saat ini berada di dalam zona perlindungan ini.
    /// </summary>
    public bool IsPlayerInside { get; private set; }

    private void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsPlayer(other))
        {
            IsPlayerInside = true;
            BoyController boy = other.GetComponent<BoyController>() ?? other.GetComponentInParent<BoyController>();
            if (boy != null)
            {
                boy.SetCrouching(true);
            }
            onEnterShelter?.Invoke();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsPlayer(other))
        {
            IsPlayerInside = false;
            BoyController boy = other.GetComponent<BoyController>() ?? other.GetComponentInParent<BoyController>();
            if (boy != null)
            {
                boy.SetCrouching(false);
            }
            onExitShelter?.Invoke();
        }
    }

    private bool IsPlayer(Collider col)
    {
        return col.GetComponent<BoyController>() != null ||
               col.CompareTag("Player") ||
               col.name.ToLower().Contains("boy");
    }

    private void OnGUI()
    {
        if (!showDebugGUI || !IsPlayerInside)
        {
            return;
        }

        // Tampilkan banner notifikasi di bagian atas layar saat berada di kolong meja
        GUIStyle style = new GUIStyle(GUI.skin.box);
        style.fontSize = 24;
        style.fontStyle = FontStyle.Bold;
        style.normal.textColor = Color.green;
        style.alignment = TextAnchor.MiddleCenter;

        float width = 450f;
        float height = 60f;
        float x = (Screen.width - width) * 0.5f;
        float y = 20f;

        GUI.Box(new Rect(x, y, width, height), $"BERHASIL BERLINDUNG!\n({zoneName})", style);
    }

    private void OnDrawGizmos()
    {
        // Visualisasi warna hijau transparan di Scene View
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.color = IsPlayerInside
                ? new Color(0f, 1f, 0f, 0.45f)
                : new Color(0.2f, 0.8f, 0.2f, 0.2f);

            Gizmos.matrix = transform.localToWorldMatrix;
            if (col is BoxCollider box)
            {
                Gizmos.DrawCube(box.center, box.size);
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(box.center, box.size);
            }
        }
    }
}
