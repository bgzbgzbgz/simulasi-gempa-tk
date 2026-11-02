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

    [Header("Syarat Objektif")]
    [Tooltip("Apakah karakter pemain harus dalam posisi menunduk (Crouch)?")]
    public bool requireCrouch = false;

    [Tooltip("Durasi yang dibutuhkan pemain berada di zona ini (detik). Isi 0 untuk langsung selesai saat disentuh.")]
    public float requiredHoldTime = 0f;

    [Header("Visual Debug")]
    public Color gizmoColor = new Color(0.2f, 0.9f, 0.4f, 0.35f);

    private float currentHoldTimer = 0f;
    private bool isPlayerInside = false;
    private BoyController currentPlayer = null;
    private bool isCompleted = false;

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

        // Cek apakah saat ini level yang aktif sesuai
        if (GameManager.Instance.currentLevel != targetLevel || !GameManager.Instance.isGameActive)
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

        if (GameManager.Instance.missionUI != null)
        {
            GameManager.Instance.missionUI.ResetHoldProgress();
        }

        GameManager.Instance.CompleteLevel(targetLevel);
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
