using UnityEngine;

/// <summary>
/// Trigger checkpoint / objektif untuk level tertentu.
/// Mendukung durasi bertahan (Hold Time) untuk simulasi berlindung dan syarat harus menunduk (Crouch).
/// </summary>
[RequireComponent(typeof(Collider))]
public class CheckpointTrigger : MonoBehaviour
{
    [Header("Pengaturan Level Target")]
    [Tooltip("Level yang harus aktif di GameManager agar trigger ini merespons")]
    public int targetLevel = 1;
    public string checkpointName = "Titik Checkpoint";

    [Header("Mode Trigger Mulai Simulasi")]
    [Tooltip("Jika dicentang, checkpoint ini berfungsi sebagai trigger pemicu awal simulasi (bukan menyelesaikan level).")]
    public bool isStartSimulationTrigger = false;

    [Header("Syarat Objektif")]
    [Tooltip("Apakah karakter pemain harus dalam posisi menunduk (Crouch)?")]
    public bool requireCrouch = false;

    [Tooltip("Apakah pemain harus sudah mengambil tas pelindung kepala?")]
    public bool requireBag = false;

    [Tooltip("Durasi yang dibutuhkan pemain berada di zona ini (detik). Isi 0 untuk langsung selesai saat disentuh.")]
    public float requiredHoldTime = 0f;

    [Header("Visual Debug")]
    public Color gizmoColor = new Color(0.2f, 0.9f, 0.4f, 0.35f);

    private float currentHoldTimer = 0f;
    private bool isPlayerInside = false;
    private BoyController currentPlayer = null;
    private bool isCompleted = false;
    private bool hasPlayedShelterVO = false;

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
        BoyController boy = GetBoyController(other);
        if (boy != null)
        {
            currentPlayer = boy;
            isPlayerInside = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        BoyController boy = GetBoyController(other);
        if (boy != null && boy == currentPlayer)
        {
            isPlayerInside = false;
            currentPlayer = null;
            currentHoldTimer = 0f;

            if (GameManager.Instance != null && GameManager.Instance.missionUI != null)
            {
                GameManager.Instance.missionUI.ResetHoldProgress();
            }
        }
    }

    private void Update()
    {
        if (isCompleted || !isPlayerInside || GameManager.Instance == null)
        {
            return;
        }

        // Jika ini adalah trigger awal mulai simulasi
        if (isStartSimulationTrigger)
        {
            if (!GameManager.Instance.isGameActive)
            {
                TriggerComplete();
            }
            return;
        }

        // Cek apakah saat ini level yang aktif sesuai
        if (GameManager.Instance.currentLevel != targetLevel || !GameManager.Instance.isGameActive)
        {
            return;
        }

        // Cek syarat mengambil tas jika diwajibkan (misal pada checkpoint titik kumpul Level 3)
        if (requireBag && !GameManager.Instance.hasPickedUpBag)
        {
            return;
        }

        // Cek syarat menunduk jika diwajibkan
        if (requireCrouch && currentPlayer != null && !currentPlayer.IsCrouching)
        {
            currentHoldTimer = 0f;
            if (GameManager.Instance.missionUI != null)
            {
                GameManager.Instance.missionUI.ResetHoldProgress();
            }
            return;
        }

        // Putar VO Bu Guru memuji saat murid menunduk di kolong meja (Level 2)
        if (targetLevel == 2 && requireCrouch && !hasPlayedShelterVO)
        {
            hasPlayedShelterVO = true;
            if (VoiceOverManager.Instance != null)
            {
                VoiceOverManager.Instance.PlayVO_08_Level2MasukKolong();
            }
        }

        // Logika durasi bertahan (Hold Timer)
        if (requiredHoldTime > 0f)
        {
            currentHoldTimer += Time.deltaTime;

            if (GameManager.Instance.missionUI != null)
            {
                GameManager.Instance.missionUI.UpdateHoldProgress(currentHoldTimer, requiredHoldTime);
            }

            if (currentHoldTimer >= requiredHoldTime)
            {
                TriggerComplete();
            }
        }
        else
        {
            // Langsung selesai begitu masuk zona
            TriggerComplete();
        }
    }

    private void TriggerComplete()
    {
        isCompleted = true;
        currentHoldTimer = 0f;

        if (GameManager.Instance != null)
        {
            if (GameManager.Instance.missionUI != null)
            {
                GameManager.Instance.missionUI.ResetHoldProgress();
            }

            if (isStartSimulationTrigger)
            {
                GameManager.Instance.StartSimulation();
                
                // Matikan trigger ini agar tidak terpanggil ulang,
                // tapi JANGAN matikan gameObject jika ini adalah TV (prop kelas)
                this.enabled = false;
                Collider col = GetComponent<Collider>();
                if (col != null) col.enabled = false;

                if (gameObject.name.Contains("Trigger") || gameObject.name.Contains("Zona"))
                {
                    gameObject.SetActive(false);
                }
            }
            else
            {
                GameManager.Instance.CompleteLevel(targetLevel);
            }
        }
    }

    private BoyController GetBoyController(Collider col)
    {
        return col.GetComponent<BoyController>() ?? col.GetComponentInParent<BoyController>();
    }

    private void OnDrawGizmos()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.color = isPlayerInside ? Color.green : gizmoColor;
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
