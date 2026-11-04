using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Penunjuk jalur evakuasi Level 3: menggambar garis dari pemain ke titik kumpul.
/// Garis mengikuti NavMesh supaya berbelok lewat pintu dan koridor, tidak menembus tembok.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class EvacuationGuide : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Garis hanya tampil selama level ini aktif (Level 3 = Evakuasi).")]
    [SerializeField] private int guideLevel = 3;

    [Tooltip("Karakter pemain. Kosongkan untuk mencari BoyController otomatis.")]
    [SerializeField] private Transform player;

    [Tooltip("Titik kumpul tujuan evakuasi.")]
    [SerializeField] private Transform assemblyPoint;

    [Header("Tampilan Garis")]
    [Tooltip("Tinggi garis di atas lantai (meter) supaya tidak tenggelam di lantai.")]
    [SerializeField, Min(0f)] private float heightAboveGround = 0.1f;

    [Tooltip("Seberapa sering jalur dihitung ulang (detik).")]
    [SerializeField, Min(0.05f)] private float refreshInterval = 0.25f;

    [Tooltip("Jarak maksimum dari pemain/titik kumpul ke NavMesh terdekat (meter).")]
    [SerializeField, Min(0.1f)] private float navMeshSampleDistance = 2f;

    private LineRenderer line;
    private NavMeshPath path;
    private readonly Vector3[] corners = new Vector3[64];
    private float nextRefreshTime;
    private bool warnedNoNavMesh;

    private void Awake()
    {
        line = GetComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.positionCount = 0;
        path = new NavMeshPath();

        if (player == null)
        {
            BoyController boy = FindFirstObjectByType<BoyController>();
            if (boy != null) player = boy.transform;
        }
    }

    private void Update()
    {
        if (player == null)
        {
            BoyController boy = FindFirstObjectByType<BoyController>();
            if (boy != null) player = boy.transform;
        }
        GameManager gm = GameManager.Instance;
        bool active = gm != null && gm.isGameActive && gm.currentLevel == guideLevel
                      && player != null && assemblyPoint != null;

        if (!active)
        {
            line.positionCount = 0;
            return;
        }

        if (Time.time < nextRefreshTime) return;
        nextRefreshTime = Time.time + refreshInterval;

        if (!TryDrawNavMeshPath())
        {
            // shortcut: tanpa NavMesh garis ditarik lurus (bisa menembus tembok); Bake NavMesh untuk jalur yang benar
            if (!warnedNoNavMesh)
            {
                warnedNoNavMesh = true;
                Debug.LogWarning("[EvacuationGuide] Jalur NavMesh tidak ditemukan. Tambahkan NavMesh Surface pada denah lalu klik Bake.");
            }
            line.positionCount = 2;
            line.SetPosition(0, player.position + Vector3.up * heightAboveGround);
            line.SetPosition(1, assemblyPoint.position + Vector3.up * heightAboveGround);
        }
    }

    private bool TryDrawNavMeshPath()
    {
        if (!NavMesh.SamplePosition(player.position, out NavMeshHit start, navMeshSampleDistance, NavMesh.AllAreas)) return false;
        if (!NavMesh.SamplePosition(assemblyPoint.position, out NavMeshHit end, navMeshSampleDistance, NavMesh.AllAreas)) return false;
        if (!NavMesh.CalculatePath(start.position, end.position, NavMesh.AllAreas, path)) return false;
        if (path.status == NavMeshPathStatus.PathInvalid) return false;

        int count = path.GetCornersNonAlloc(corners);
        if (count < 2) return false;

        line.positionCount = count;
        for (int i = 0; i < count; i++)
        {
            line.SetPosition(i, corners[i] + Vector3.up * heightAboveGround);
        }
        return true;
    }
}

