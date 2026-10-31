using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Kamera third-person dengan orbit tetap: dapat diputar, tetapi pemain tidak dapat zoom.
/// Kamera otomatis mendekat bila ada dinding atau objek di antara kamera dan target.
/// </summary>
public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField, Min(0f)] private float targetHeight = 1.5f;

    [Header("Orbit")]
    [SerializeField, Min(0.5f)] private float distance = 6f;
    [SerializeField] private float startYaw;
    [SerializeField] private float startPitch = -15f;
    [SerializeField, Min(0f)] private float rotationSensitivity = 0.15f;
    [SerializeField, Range(-89f, 0f)] private float minPitch = -45f;
    [SerializeField, Range(0f, 89f)] private float maxPitch = 10f;
    [SerializeField, Min(0f)] private float followSmoothness = 14f;
    [Tooltip("Menaikkan posisi kamera tanpa mengubah jarak, yaw, atau pitch orbit.")]
    [SerializeField] private float cameraHeightOffset;

    [Header("Soft Follow")]
    [SerializeField] private bool followPlayerDirection = true;
    [SerializeField, Min(0f)] private float directionFollowSpeed = 90f;
    [SerializeField, Range(0f, 180f)] private float sharpTurnIgnoreAngle = 120f;

    [Header("Camera Collision")]
    [SerializeField] private LayerMask collisionLayers = ~0;
    [SerializeField, Min(0.01f)] private float collisionRadius = 0.25f;
    [SerializeField, Min(0.1f)] private float collisionPadding = 0.1f;
    [SerializeField, Min(0.1f)] private float minimumDistance = 1f;

    private float yaw;
    private float pitch;
    private CharacterController targetController;

    private void Start()
    {
        yaw = startYaw;
        pitch = startPitch;
        targetController = target != null ? target.GetComponent<CharacterController>() : null;
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector2 orbitInput = ReadOrbitInput();
        if (orbitInput.sqrMagnitude > 0.001f)
        {
            yaw += orbitInput.x * rotationSensitivity;
            pitch = Mathf.Clamp(pitch - orbitInput.y * rotationSensitivity, minPitch, maxPitch);
        }
        else
        {
            FollowPlayerDirection();
        }

        Vector3 pivot = target.position + Vector3.up * targetHeight;
        Quaternion orbitRotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 baseCameraDirection = -(orbitRotation * Vector3.forward);
        Vector3 desiredOffset = baseCameraDirection * distance + Vector3.up * cameraHeightOffset;
        Vector3 cameraDirection = desiredOffset.normalized;
        float requestedDistance = desiredOffset.magnitude;
        float cameraDistance = GetClearCameraDistance(pivot, cameraDirection, requestedDistance);
        Vector3 desiredPosition = pivot + cameraDirection * cameraDistance;

        float followFactor = 1f - Mathf.Exp(-followSmoothness * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, desiredPosition, followFactor);
        transform.rotation = Quaternion.LookRotation(pivot - transform.position, Vector3.up);
    }

    private void FollowPlayerDirection()
    {
        if (!followPlayerDirection || targetController == null)
        {
            return;
        }

        Vector3 horizontalVelocity = Vector3.ProjectOnPlane(targetController.velocity, Vector3.up);
        if (horizontalVelocity.sqrMagnitude < 0.01f)
        {
            return;
        }

        float playerYaw = target.eulerAngles.y;
        float yawDifference = Mathf.Abs(Mathf.DeltaAngle(yaw, playerYaw));
        if (yawDifference > sharpTurnIgnoreAngle)
        {
            return;
        }

        yaw = Mathf.MoveTowardsAngle(yaw, playerYaw, directionFollowSpeed * Time.deltaTime);
    }

    private Vector2 ReadOrbitInput()
    {
        // Editor/PC: tahan klik kanan lalu gerakkan mouse.
        if (Mouse.current != null && Mouse.current.rightButton.isPressed)
        {
            return Mouse.current.delta.ReadValue();
        }

        // Mobile/Tablet: geser jari pada sisi kanan layar agar tidak bertabrakan dengan virtual joystick di sisi kiri.
        // Mendukung multi-touch agar pemain bisa berjalan sambil memutar kamera secara bersamaan.
        if (Touchscreen.current != null)
        {
            var touches = Touchscreen.current.touches;
            for (int i = 0; i < touches.Count; i++)
            {
                var touch = touches[i];
                if (touch.press.isPressed)
                {
                    // Abaikan jika sentuhan mengenai elemen UI (seperti tombol Merunduk / Crouch)
                    if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.touchId.ReadValue()))
                    {
                        continue;
                    }

                    Vector2 touchPosition = touch.position.ReadValue();
                    if (touchPosition.x >= Screen.width * 0.5f)
                    {
                        return touch.delta.ReadValue();
                    }
                }
            }
        }

        return Vector2.zero;
    }

    private float GetClearCameraDistance(Vector3 pivot, Vector3 cameraDirection, float requestedDistance)
    {
        RaycastHit[] hits = Physics.SphereCastAll(
            pivot,
            collisionRadius,
            cameraDirection,
            requestedDistance,
            collisionLayers,
            QueryTriggerInteraction.Ignore);

        float clearDistance = requestedDistance;
        foreach (RaycastHit hit in hits)
        {
            // Abaikan collider yang merupakan bagian dari Boy sendiri.
            if (hit.transform == target || hit.transform.IsChildOf(target) || target.IsChildOf(hit.transform))
            {
                continue;
            }

            clearDistance = Mathf.Min(clearDistance, hit.distance - collisionPadding);
        }

        return Mathf.Max(minimumDistance, clearDistance);
    }
}
