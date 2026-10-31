using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Virtual Joystick UI untuk kontrol pergerakan karakter di layar sentuh Android atau mouse drag di Editor.
/// </summary>
public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("UI References")]
    [Tooltip("Transform background / lingkaran luar joystick.")]
    [SerializeField] private RectTransform background;

    [Tooltip("Transform handle / tombol analog tengah.")]
    [SerializeField] private RectTransform handle;

    [Header("Settings")]
    [Tooltip("Jarak maksimal handle bergeser dari titik tengah background (pixel canvas).")]
    [SerializeField, Min(10f)] private float handleRange = 75f;

    [Tooltip("Deadzone input (0 = respon langsung, nilai kecil untuk mencegah getaran jari).")]
    [SerializeField, Range(0f, 0.5f)] private float deadZone = 0.05f;

    /// <summary>
    /// Vektor arah pergerakan yang sudah dinormalisasi (-1 s/d 1 pada sumbu X dan Y).
    /// </summary>
    public Vector2 InputDirection { get; private set; } = Vector2.zero;

    private Canvas canvas;
    private Camera canvasCamera;

    private void Awake()
    {
        Screen.orientation = ScreenOrientation.LandscapeLeft;

        canvas = GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            canvasCamera = canvas.worldCamera;
        }

        if (background == null)
        {
            background = GetComponent<RectTransform>();
        }

        if (handle == null && transform.childCount > 0)
        {
            handle = transform.GetChild(0).GetComponent<RectTransform>();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (background == null || handle == null)
        {
            return;
        }

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            background,
            eventData.position,
            canvasCamera,
            out Vector2 localPoint))
        {
            Vector2 offset = localPoint;
            float distance = offset.magnitude;

            if (distance > handleRange)
            {
                offset = offset.normalized * handleRange;
            }

            handle.anchoredPosition = offset;

            // Hitung input ternormalisasi (-1 s/d 1)
            Vector2 rawInput = offset / handleRange;
            if (rawInput.magnitude < deadZone)
            {
                InputDirection = Vector2.zero;
            }
            else
            {
                InputDirection = rawInput;
            }
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        ResetJoystick();
    }

    private void OnDisable()
    {
        ResetJoystick();
    }

    private void ResetJoystick()
    {
        InputDirection = Vector2.zero;
        if (handle != null)
        {
            handle.anchoredPosition = Vector2.zero;
        }
    }
}
