using UnityEngine;

/// <summary>
/// Komponen untuk objek Tas Evakuasi di dalam kelas.
/// Diambil pada awal Level 3 untuk melindungi kepala saat evakuasi menuju lapangan.
/// </summary>
[RequireComponent(typeof(Collider))]
public class BagPickup : MonoBehaviour
{
    [Header("Pengaturan")]
    public string bagName = "Tas Sekolah";
    [Tooltip("Offset posisi tas saat menempel di kepala.")]
    public Vector3 headOffset = new Vector3(0f, 0.18f, 0f);
    [Tooltip("Rotasi tas saat menempel di kepala.")]
    public Vector3 headRotation = new Vector3(90f, 0f, 0f);
    [Tooltip("Skala tas saat menempel di kepala karakter.")]
    public Vector3 customScale = new Vector3(0.35f, 0.45f, 0.2f);

    [Header("Audio")]
    public AudioClip pickupSound;

    [Header("Visual Gizmo")]
    public Color gizmoColor = new Color(1f, 0.6f, 0.1f, 0.4f);

    private bool isPickedUp = false;

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
        if (isPickedUp) return;

        // Hanya bisa diambil saat Level 3 aktif dan belum pernah diambil
        if (GameManager.Instance == null || GameManager.Instance.currentLevel != 3 || GameManager.Instance.hasPickedUpBag)
        {
            return;
        }

        BoyController boy = other.GetComponent<BoyController>() ?? other.GetComponentInParent<BoyController>();
        if (boy == null) return;

        PickUp(boy);
    }

    /// <summary>
    /// Menempelkan tas di atas kepala karakter dan memberi tahu GameManager.
    /// </summary>
    public void PickUp(BoyController boy)
    {
        isPickedUp = true;

        Transform headBone = FindHeadTransform(boy);

        // Tempelkan tas sebagai child dari kepala (atau karakter)
        transform.SetParent(headBone, false);

        // Jika menempel langsung di root boy (bukan bone), beri offset ketinggian badan
        if (headBone == boy.transform)
        {
            transform.localPosition = headOffset + Vector3.up * 1.05f;
        }
        else
        {
            transform.localPosition = headOffset;
        }

        transform.localEulerAngles = headRotation;
        transform.localScale = customScale;

        // Matikan collider agar tidak bentrok dengan CharacterController
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false;
        }

        // Putar audio jika ada
        if (pickupSound != null)
        {
            AudioSource.PlayClipAtPoint(pickupSound, transform.position);
        }
        else if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayBagPickup();
        }

        // Laporkan ke GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnBagPickedUp();
        }
    }

    private Transform FindHeadTransform(BoyController boy)
    {
        Animator anim = boy.GetComponentInChildren<Animator>();
        if (anim != null && anim.isHuman)
        {
            Transform humanHead = anim.GetBoneTransform(HumanBodyBones.Head);
            if (humanHead != null) return humanHead;
        }

        // Cari bone dengan nama 'Head'
        foreach (Transform t in boy.GetComponentsInChildren<Transform>())
        {
            if (t.name.ToLower().Contains("head"))
            {
                return t;
            }
        }

        // shortcut: fallback ke root boy jika rig bukan humanoid; upgrade dengan set manual di Inspector
        return boy.transform;
    }

    private void OnDrawGizmos()
    {
        if (isPickedUp) return;

        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.color = gizmoColor;
            Gizmos.matrix = transform.localToWorldMatrix;

            if (col is BoxCollider box)
            {
                Gizmos.DrawCube(box.center, box.size);
            }
            else if (col is SphereCollider sphere)
            {
                Gizmos.DrawSphere(sphere.center, sphere.radius);
            }
        }
    }
}
