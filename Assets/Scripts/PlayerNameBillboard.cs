using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Menampilkan nama pemain di atas kepala karakter dalam bentuk World Space UI (Billboard).
/// Mengambil nama dari PlayerPrefs ("PlayerName") yang diinput di Main Menu.
/// Selalu menghadap ke kamera pemain agar mudah dibaca dari segala sudut.
/// </summary>
public class PlayerNameBillboard : MonoBehaviour
{
    [Header("Target & Offset")]
    [Tooltip("Transform tulang kepala atau root karakter yang diikuti.")]
    public Transform targetTransform;

    [Tooltip("Offset ketinggian nama di atas target (meter).")]
    public Vector3 offset = new Vector3(0f, 0.40f, 0f);

    [Header("UI References")]
    [Tooltip("Komponen Text untuk menampilkan nama.")]
    public Text nameText;

    [Tooltip("Nama default jika pemain belum mengisi nama di Main Menu.")]
    public string defaultName = "Ananda";

    [Tooltip("Apakah menampilkan prefix gender (Putra/Putri) di samping nama?")]
    public bool includeGenderIcon = false;

    private Camera mainCamera;

    private void Awake()
    {
        // Cari kamera utama jika belum ada
        FindCamera();

        // Cari target kepala otomatis jika belum diset
        if (targetTransform == null)
        {
            AutoDetectHeadTarget();
        }

        // Auto-detect Text component jika belum diassign
        if (nameText == null)
        {
            nameText = GetComponentInChildren<Text>();
        }
    }

    private void Start()
    {
        RefreshPlayerName();
    }

    private void OnEnable()
    {
        RefreshPlayerName();
    }

    public void RefreshPlayerName()
    {
        string playerName = PlayerPrefs.GetString("PlayerName", defaultName);
        if (string.IsNullOrWhiteSpace(playerName))
        {
            playerName = defaultName;
        }

        string gender = PlayerPrefs.GetString("PlayerGender", "Boy");
        string prefix = "";
        if (includeGenderIcon)
        {
            prefix = (gender == "Girl") ? "[Putri] " : "[Putra] ";
        }

        if (nameText != null)
        {
            nameText.text = $"{prefix}{playerName}";
        }
    }

    public void SetPlayerName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName)) newName = defaultName;
        PlayerPrefs.SetString("PlayerName", newName);
        PlayerPrefs.Save();
        RefreshPlayerName();
    }

    private void LateUpdate()
    {
        if (mainCamera == null)
        {
            FindCamera();
            if (mainCamera == null) return;
        }

        // 1. Posisikan nama tepat di atas kepala / target
        Vector3 targetPos = targetTransform != null ? targetTransform.position : transform.parent.position;
        transform.position = targetPos + offset;

        // 2. Billboard: selalu menghadap ke kamera (rotasi sama dengan kamera agar tidak terbalik/mirrored)
        transform.rotation = mainCamera.transform.rotation;
    }

    private void FindCamera()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = Object.FindFirstObjectByType<Camera>();
        }
    }

    private void AutoDetectHeadTarget()
    {
        // 1. Coba cari tulang kepala via Animator HumanBodyBones
        Animator anim = GetComponentInParent<Animator>();
        if (anim != null && anim.isHuman)
        {
            Transform head = anim.GetBoneTransform(HumanBodyBones.Head);
            if (head != null)
            {
                targetTransform = head;
                return;
            }
        }

        // 2. Coba cari child dengan nama Head
        Transform parentTransform = transform.parent;
        if (parentTransform != null)
        {
            Transform[] allChildren = parentTransform.GetComponentsInChildren<Transform>(true);
            foreach (Transform t in allChildren)
            {
                string lower = t.name.ToLower();
                if (lower.Contains("head") && !lower.Contains("head_end"))
                {
                    targetTransform = t;
                    return;
                }
            }

            // Fallback: gunakan parent root dengan offset lebih tinggi
            targetTransform = parentTransform;
            offset = new Vector3(0f, 1.25f, 0f);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 pos = (targetTransform != null ? targetTransform.position : transform.position) + offset;
        Gizmos.DrawWireSphere(pos, 0.1f);
    }
#endif
}
