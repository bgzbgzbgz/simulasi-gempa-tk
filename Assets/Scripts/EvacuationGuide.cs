using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Penunjuk jalur evakuasi Level 3:
/// - Pita panah hijau chevron padat (>>>>>) yang rebah datar di atas lantai (TransformZ / horizontal).
/// - Panah diam statis (tanpa animasi scrolling) sesuai instruksi user.
/// - Terhubung rapat dari posisi pemain saat ini hingga checkpoint terakhir (Titik Kumpul di lapangan terbuka).
/// - Saat pemain melangkah maju melewati panah, bagian belakang pemain otomatis menghilang dan rute di depan tetap menuntun ke tujuan.
/// - Menampilkan bintang kecil bercahaya di setiap titik belokan untuk menarik perhatian anak-anak TK.
/// </summary>
[ExecuteAlways]
public class EvacuationGuide : MonoBehaviour
{
    public enum GuideMode
    {
        ManualWaypoints, // Direkomendasikan: rute belokan pasti, 100% bebas tembus tembok
        NavMeshAuto      // Otomatis via NavMesh
    }

    [Header("Mode Panduan Jalur")]
    [Tooltip("ManualWaypoints: rute belokan diatur lewat titik objek waypoint. NavMeshAuto: menggunakan AI Navigation.")]
    [SerializeField] private GuideMode guideMode = GuideMode.ManualWaypoints;

    [Header("Target & Pemain")]
    [SerializeField] private int guideLevel = 3;
    [SerializeField] private Transform player;
    [SerializeField] private Transform assemblyPoint;

    [Header("Preview di Editor")]
    [Tooltip("Centang untuk melihat rute panah hijau langsung di Scene & Game View tanpa perlu Play mode.")]
    [SerializeField] private bool previewInEditor = true;

    [Header("Rute Manual (Waypoints)")]
    [Tooltip("Daftar titik belokan evakuasi.")]
    [SerializeField] private List<Transform> manualWaypoints = new List<Transform>();

    [Tooltip("Jarak toleransi (meter) untuk mendeteksi pemain telah melewati waypoint aktif.")]
    [SerializeField, Min(0.5f)] private float waypointReachRadius = 2.2f;

    [Header("Tampilan Panah Chevron di Atas Lantai")]
    [Tooltip("Ketinggian pita garis menempel tepat di atas permukaan lantai (meter).")]
    [SerializeField, Min(0.01f)] private float heightAboveGround = 0.05f;

    [Tooltip("Lebar pita panah evakuasi di atas lantai.")]
    [SerializeField, Min(0.1f)] private float lineWidth = 0.85f;

    [Tooltip("Kerapatan pengulangan panah hijau chevron sepanjang rute.")]
    [SerializeField, Min(0.1f)] private float arrowDensity = 1.6f;

    [Tooltip("Material strip panah hijau chevron (Mat_EvacuationChevron_Strip).")]
    [SerializeField] private Material arrowMaterial;

    [Header("Bintang Penuntun (Glowing Stars)")]
    [Tooltip("Daftar objek bintang bercahaya penuntun di titik-titik rute.")]
    [SerializeField] private List<EvacuationStar> evacuationStars = new List<EvacuationStar>();

    private LineRenderer line;
    private GameObject lineVisualObj;
    private Material lineRuntimeMat;
    private int currentWaypointIndex = 0;
    private bool wasActive = false;

    // Untuk NavMesh fallback
    private NavMeshPath navPath;
    private readonly Vector3[] navCorners = new Vector3[64];

    private void Awake()
    {
        EnsureLineVisualObject();
        FindReferencesIfMissing();
        AutoPopulateWaypointsIfEmpty();
        AutoFindStarsIfEmpty();
    }

    private void OnEnable()
    {
        EnsureLineVisualObject();
        FindReferencesIfMissing();
    }

    /// <summary>
    /// Memastikan objek visual LineRenderer berada di child object khusus dengan rotasi -90 derajat (X)
    /// dan alignment TransformZ agar pita panah 100% REBAH DATAR di atas lantai dari semua sudut kamera,
    /// tanpa mengubah posisi maupun rotasi waypoint anak lainnya.
    /// </summary>
    public void EnsureLineVisualObject()
    {
        // 1. Jika ada LineRenderer lama langsung di root Jalur_Evakuasi, bersihkan agar tidak dobel
        LineRenderer rootLine = GetComponent<LineRenderer>();
        if (rootLine != null)
        {
            if (Application.isPlaying) Destroy(rootLine);
            else DestroyImmediate(rootLine);
        }

        // 2. Cari atau buat child GameObject "Line_Visual"
        Transform child = transform.Find("Line_Visual");
        if (child == null)
        {
            lineVisualObj = new GameObject("Line_Visual");
            lineVisualObj.transform.SetParent(transform, false);
            child = lineVisualObj.transform;
        }
        else
        {
            lineVisualObj = child.gameObject;
        }

        // Kunci posisi lokal di (0,0,0) dan rotasi -90 derajat sumbu X
        // agar local Z menghadap lurus ke ATAS (Vector3.up), membuat pita LineRenderer rebah rata horizontal
        child.localPosition = Vector3.zero;
        child.localRotation = Quaternion.Euler(-90f, 0f, 0f);
        child.localScale = Vector3.one;

        // 3. Konfigurasi LineRenderer di child object
        line = child.GetComponent<LineRenderer>();
        if (line == null)
        {
            line = child.gameObject.AddComponent<LineRenderer>();
        }

        line.useWorldSpace = true;
        line.alignment = LineAlignment.TransformZ;
        line.textureMode = LineTextureMode.Tile;
        line.numCornerVertices = 4;
        line.numCapVertices = 4;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.startWidth = lineWidth;
        line.endWidth = lineWidth;

        if (arrowMaterial == null)
        {
#if UNITY_EDITOR
            arrowMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_EvacuationChevron_Strip.mat");
#endif
        }

        if (arrowMaterial != null)
        {
            if (Application.isPlaying)
            {
                if (lineRuntimeMat == null) lineRuntimeMat = new Material(arrowMaterial);
                line.material = lineRuntimeMat;
            }
            else
            {
                line.sharedMaterial = arrowMaterial;
            }
        }
    }

    private void Start()
    {
        currentWaypointIndex = 0;
        if (Application.isPlaying)
        {
            SetStarsActive(false);
        }
    }

    private void OnDestroy()
    {
        if (lineRuntimeMat != null)
        {
            DestroyImmediate(lineRuntimeMat);
        }
    }

    public void FindReferencesIfMissing()
    {
        if (player == null)
        {
            BoyController boy = FindFirstObjectByType<BoyController>();
            if (boy != null) player = boy.transform;
            else if (Camera.main != null) player = Camera.main.transform;
        }

        if (assemblyPoint == null)
        {
            GameObject tk = GameObject.Find("Titik_Kumpul_Level3") ?? GameObject.Find("Penanda_Titik_Kumpul");
            if (tk != null)
            {
                assemblyPoint = tk.transform;
            }
            else
            {
                CheckpointTrigger[] cps = FindObjectsByType<CheckpointTrigger>(FindObjectsSortMode.None);
                foreach (var cp in cps)
                {
                    if (cp.targetLevel == 3)
                    {
                        assemblyPoint = cp.transform;
                        break;
                    }
                }
            }
        }
    }

    [ContextMenu("Cari Bintang di Scene")]
    public void AutoFindStarsIfEmpty()
    {
        if (evacuationStars == null) evacuationStars = new List<EvacuationStar>();
        evacuationStars.RemoveAll(s => s == null);

        if (evacuationStars.Count == 0)
        {
            evacuationStars.AddRange(GetComponentsInChildren<EvacuationStar>(true));
        }
    }

    private void SetStarsActive(bool active)
    {
        if (evacuationStars == null) return;
        for (int i = 0; i < evacuationStars.Count; i++)
        {
            if (evacuationStars[i] != null)
            {
                if (active) evacuationStars[i].ResetStar();
                else evacuationStars[i].gameObject.SetActive(false);
            }
        }
    }

    [ContextMenu("Buat Waypoints Default")]
    public void AutoPopulateWaypointsIfEmpty()
    {
        if (manualWaypoints == null) manualWaypoints = new List<Transform>();
        manualWaypoints.RemoveAll(item => item == null);

        if (manualWaypoints.Count == 0 && transform.childCount > 0)
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (child.name.StartsWith("WP_"))
                {
                    manualWaypoints.Add(child);
                }
            }
        }

        if (manualWaypoints.Count == 0)
        {
            Vector3[] defaultPositions = new Vector3[]
            {
                new Vector3(0.40f, 0.15f, -9.95f),   // WP_01: Kolong meja kelas
                new Vector3(-2.80f, 0.15f, -12.71f), // WP_02: Ambang pintu keluar kelas
                new Vector3(-6.50f, 0.15f, -12.71f), // WP_03: Tengah gang paving luar pintu
                new Vector3(-6.50f, 0.15f, -28.00f), // WP_04: Ujung gang di pertigaan jalan aspal
                new Vector3(11.00f, 0.15f, -28.00f), // WP_05: Menyusuri jalan aspal ke arah timur
                new Vector3(11.00f, 0.15f, -20.00f), // WP_06: Belok masuk ke selasar TK
                new Vector3(19.00f, 0.15f, -18.40f)  // WP_07: Menuju gerbang lapangan terbuka
            };

            string[] names = new string[]
            {
                "WP_01_KolongMeja",
                "WP_02_PintuKelas",
                "WP_03_GangLuar",
                "WP_04_UjungGangAspal",
                "WP_05_JalanAspalTimur",
                "WP_06_SelasarTK",
                "WP_07_MenujuLapangan"
            };

            for (int i = 0; i < defaultPositions.Length; i++)
            {
                GameObject wpObj = new GameObject(names[i]);
                wpObj.transform.SetParent(transform, true);
                wpObj.transform.position = defaultPositions[i];
                manualWaypoints.Add(wpObj.transform);
            }
        }
    }

    /// <summary>
    /// Menempelkan ketinggian titik tepat di atas collider lantai agar tidak amblas atau melayang.
    /// Mengabaikan collider tubuh pemain saat memeriksa posisi pijakan kaki.
    /// </summary>
    private Vector3 GetGroundPosition(Vector3 worldPos, Transform ignoreTransform = null)
    {
        Vector3 rayStart = new Vector3(worldPos.x, worldPos.y + 2f, worldPos.z);
        RaycastHit[] hits = Physics.RaycastAll(rayStart, Vector3.down, 12f, ~0, QueryTriggerInteraction.Ignore);
        float bestY = -999f;
        bool found = false;

        for (int i = 0; i < hits.Length; i++)
        {
            Transform t = hits[i].transform;
            if (ignoreTransform != null && (t == ignoreTransform || t.IsChildOf(ignoreTransform)))
                continue;

            if (!found || hits[i].point.y > bestY)
            {
                bestY = hits[i].point.y;
                found = true;
            }
        }

        if (found)
        {
            return new Vector3(worldPos.x, bestY + heightAboveGround, worldPos.z);
        }
        return new Vector3(worldPos.x, worldPos.y + heightAboveGround, worldPos.z);
    }

    private void Update()
    {
        TickGuide();
    }

    private void LateUpdate()
    {
        if (Application.isPlaying)
        {
            TickGuide();
        }
    }

    private void TickGuide()
    {
        if (line == null) EnsureLineVisualObject();
        FindReferencesIfMissing();

        bool active = false;
        if (Application.isPlaying)
        {
            GameManager gm = GameManager.Instance;
            active = gm != null && gm.isGameActive && gm.currentLevel == guideLevel
                     && player != null && assemblyPoint != null;
        }
        else
        {
            active = previewInEditor && player != null && assemblyPoint != null;
        }

        // Tangani transisi aktivasi Level 3
        if (active != wasActive)
        {
            wasActive = active;
            SetStarsActive(active);
            if (active) currentWaypointIndex = 0;
        }

        if (!active)
        {
            if (line != null && line.positionCount != 0) line.positionCount = 0;
            return;
        }

        // 1. Tampilan Panah Hijau Chevron Statis (DIAM, TANPA ANIMASI)
        Material activeMat = Application.isPlaying ? (line != null ? line.material : lineRuntimeMat) : arrowMaterial;
        if (activeMat != null)
        {
            Vector2 tiling = new Vector2(arrowDensity, 1f);
            activeMat.SetTextureScale("_BaseMap", tiling);
            activeMat.mainTextureScale = tiling;
            activeMat.SetTextureOffset("_BaseMap", Vector2.zero);
            activeMat.mainTextureOffset = Vector2.zero;
        }

        if (line != null)
        {
            line.startWidth = lineWidth;
            line.endWidth = lineWidth;
        }

        // 2. Gambar rute berkelanjutan dari posisi pemain hingga titik kumpul akhir
        Transform currentTarget = assemblyPoint;

        // Cek jika ada tas yang belum diambil di kelas
        BagPickup bag = FindFirstObjectByType<BagPickup>();
        if (bag != null && bag.gameObject.activeInHierarchy && Application.isPlaying)
        {
            GameManager gm = GameManager.Instance;
            if (gm != null && !gm.hasPickedUpBag)
            {
                currentTarget = bag.transform;
            }
        }

        if (guideMode == GuideMode.ManualWaypoints && manualWaypoints.Count > 0)
        {
            DrawManualWaypointPath(currentTarget);
        }
        else
        {
            if (!TryDrawNavMeshPath(currentTarget))
            {
                DrawDirectPath(currentTarget);
            }
        }
    }

    /// <summary>
    /// Memperbarui progres rute secara cerdas berbasis segmen koridor/jalan:
    /// - Mengidentifikasi secara presisi di segmen mana pemain sedang melangkah.
    /// - Waypoint aktif hanya bergeser jika pemain benar-benar telah mencapai belokan atau melangkah ke segmen berikutnya.
    /// - Mencegah panah melompati belokan sebelum waktunya dan mencegah garis memotong pagar/tembok secara diagonal.
    /// </summary>
    private void UpdateWaypointProgress(Vector3 playerPos)
    {
        if (manualWaypoints == null || manualWaypoints.Count == 0) return;

        // Susun daftar urutan rute lengkap (termasuk assemblyPoint di ujung akhir)
        List<Transform> fullRoute = new List<Transform>(manualWaypoints);
        if (assemblyPoint != null && (fullRoute.Count == 0 || fullRoute[fullRoute.Count - 1] != assemblyPoint))
        {
            fullRoute.Add(assemblyPoint);
        }

        Vector2 p2D = new Vector2(playerPos.x, playerPos.z);
        int startSeg = Mathf.Max(0, currentWaypointIndex - 1);

        for (int i = startSeg; i < fullRoute.Count - 1; i++)
        {
            Transform wpA = fullRoute[i];
            Transform wpB = fullRoute[i + 1];
            if (wpA == null || wpB == null) continue;

            Vector2 a = new Vector2(wpA.position.x, wpA.position.z);
            Vector2 b = new Vector2(wpB.position.x, wpB.position.z);
            Vector2 u = b - a;
            float L = u.magnitude;
            if (L < 0.001f) continue;

            Vector2 uDir = u / L;
            Vector2 v = p2D - a;
            float s = Vector2.Dot(v, uDir);
            float dPerp = (v - s * uDir).magnitude;

            // Pemain berada di dalam koridor jalan ini (lebar toleransi jalan ~3.0 meter)
            if (dPerp < 3.0f && s >= -0.5f && s <= L + 0.5f)
            {
                if (s < L - 0.6f)
                {
                    currentWaypointIndex = Mathf.Max(currentWaypointIndex, i + 1);
                }
                else
                {
                    currentWaypointIndex = Mathf.Max(currentWaypointIndex, Mathf.Min(i + 2, manualWaypoints.Count));
                }
                break;
            }
        }

        if (currentWaypointIndex > manualWaypoints.Count)
        {
            currentWaypointIndex = manualWaypoints.Count;
        }
    }

    private void DrawManualWaypointPath(Transform target)
    {
        if (line == null || player == null) return;
        Vector3 playerPos = player.position;

        // Majukan indeks waypoint mengikuti langkah kaki pemain
        if (Application.isPlaying)
        {
            UpdateWaypointProgress(playerPos);
        }

        // Susun titik-titik panah jalur
        List<Vector3> points = new List<Vector3>();

        // 1. Titik awal: menempel tepat di bawah posisi kaki pemain saat ini
        points.Add(GetGroundPosition(playerPos, player));

        // 2. Titik-titik waypoint berikutnya yang belum dilewati (rute di depan pemain)
        for (int i = currentWaypointIndex; i < manualWaypoints.Count; i++)
        {
            if (manualWaypoints[i] != null)
            {
                points.Add(GetGroundPosition(manualWaypoints[i].position));
            }
        }

        // 3. Titik akhir: Titik Kumpul di lapangan terbuka
        if (target != null)
        {
            Vector3 finalPoint = GetGroundPosition(target.position);
            if (points.Count == 0 || Vector3.Distance(points[points.Count - 1], finalPoint) > 0.5f)
            {
                points.Add(finalPoint);
            }
        }

        line.positionCount = points.Count;
        for (int i = 0; i < points.Count; i++)
        {
            line.SetPosition(i, points[i]);
        }
    }

    private bool TryDrawNavMeshPath(Transform target)
    {
        if (target == null || line == null || player == null) return false;
        if (!NavMesh.SamplePosition(player.position, out NavMeshHit start, 5f, NavMesh.AllAreas)) return false;
        if (!NavMesh.SamplePosition(target.position, out NavMeshHit end, 5f, NavMesh.AllAreas)) return false;
        if (navPath == null) navPath = new NavMeshPath();
        if (!NavMesh.CalculatePath(start.position, end.position, NavMesh.AllAreas, navPath)) return false;
        if (navPath.status == NavMeshPathStatus.PathInvalid) return false;

        int count = navPath.GetCornersNonAlloc(navCorners);
        if (count < 2) return false;

        line.positionCount = count;
        for (int i = 0; i < count; i++)
        {
            line.SetPosition(i, GetGroundPosition(navCorners[i]));
        }
        return true;
    }

    private void DrawDirectPath(Transform target)
    {
        if (line == null || player == null || target == null) return;
        line.positionCount = 2;
        line.SetPosition(0, GetGroundPosition(player.position));
        line.SetPosition(1, GetGroundPosition(target.position));
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (manualWaypoints == null || manualWaypoints.Count == 0) return;

        for (int i = 0; i < manualWaypoints.Count; i++)
        {
            if (manualWaypoints[i] == null) continue;
            Vector3 pos = manualWaypoints[i].position;

            Gizmos.color = (i == currentWaypointIndex && Application.isPlaying) ? Color.magenta : Color.green;
            Gizmos.DrawSphere(pos, 0.3f);

            UnityEditor.Handles.Label(pos + Vector3.up * 0.5f, $"WP {i + 1}: {manualWaypoints[i].name}");

            if (i < manualWaypoints.Count - 1 && manualWaypoints[i + 1] != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(pos, manualWaypoints[i + 1].position);
            }
        }
    }
#endif
}
