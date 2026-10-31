using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Tombol UI di pojok kanan bawah layar untuk mengontrol aksi merunduk / menunduk (Crouch).
/// Cocok untuk kontrol jempol kanan di mobile dan mengubah warna saat aktif.
/// </summary>
public class CrouchButton : MonoBehaviour, IPointerDownHandler
{
    [SerializeField] private BoyController targetBoy;
    [SerializeField] private Image buttonImage;
    [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 0.35f);
    [SerializeField] private Color activeCrouchColor = new Color(0.2f, 0.85f, 0.4f, 0.9f);

    private float lastClickTime = -1f;
    private const float ClickCooldown = 0.2f;

    private void Awake()
    {
        if (targetBoy == null)
        {
            targetBoy = FindFirstObjectByType<BoyController>();
        }

        if (buttonImage == null)
        {
            buttonImage = GetComponent<Image>();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        OnClickButton();
    }

    public void OnClickButton()
    {
        // Mencegah double-click / multiple trigger dalam rentang waktu singkat
        if (Time.unscaledTime - lastClickTime < ClickCooldown)
        {
            return;
        }
        lastClickTime = Time.unscaledTime;

        if (targetBoy == null)
        {
            targetBoy = FindFirstObjectByType<BoyController>();
        }

        if (targetBoy != null)
        {
            targetBoy.ToggleCrouch();
            Debug.Log($"[CrouchButton] Status Nunduk: {targetBoy.IsCrouching}");
        }
    }

    private void Update()
    {
        // Selaraskan visual tombol jika status menunduk berubah (misal via tombol C di PC atau saat di kolong meja)
        if (targetBoy != null && buttonImage != null)
        {
            Color targetColor = targetBoy.IsCrouching ? activeCrouchColor : normalColor;
            buttonImage.color = Color.Lerp(buttonImage.color, targetColor, Time.deltaTime * 12f);
        }
    }
}
