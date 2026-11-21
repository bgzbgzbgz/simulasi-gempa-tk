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
    [Range(0.2f, 2.0f)]
    [Tooltip("Tinggi pivot target dari kaki karakter (meter).")]
    public float targetHeight = 0.85f;

    [Header("Orbit (Bisa diatur pakai Slider)")]
    [Range(1.2f, 6.0f)]
    [Tooltip("Jarak kamera ke target (meter). Bisa digeser slider.")]
    public float distance = 2.6f;

    [SerializeField] private float startYaw;

    [Range(-20f, 60f)]
    [Tooltip("Sudut pitch awal kamera (derajat).")]
    public float startPitch = 14f;

    [Tooltip("Kecepatan rotasi orbit saat menggeser layar/mouse.")]
    [SerializeField, Min(0f)] private float rotationSensitivity = 0.15f;
    [SerializeField, Range(-89f, 0f)] private float minPitch = -15f;
    [SerializeField, Range(0f, 89f)] private float maxPitch = 45f;
    [SerializeField, Min(0f)] private float followSmoothness = 14f;

    [Range(0f, 1.5f)]
    [Tooltip("Menaikkan posisi kamera tanpa mengubah jarak, yaw, atau pitch orbit.")]
    public float cameraHeightOffset = 0.35f;

    [Header("Guncangan Gempa (Earthquake Shake)")]
    [Tooltip("Aktifkan guncangan kamera saat gempa Level 2 atau gempa susulan.")]
    public bool enableEarthquakeShake = true;

    [Range(0.005f, 0.25f)]
    [Tooltip("Amplitudo kamera (meter). Otomatis tersinkron mengambil dari GameObject 'Efek_Gempa' (EarthquakeEffect) sebagai pusat pengaturan.")]
    public float shakeAmplitude = 0.04f;

    [Range(0f, 3.0f)]
    [Tooltip("Kemiringan getaran rotasi kamera saat gempa (derajat roll).")]
    public float shakeRollAngle = 0.6f;

    [Range(5f, 35f)]
    [Tooltip("Frekuensi getaran per detik.")]
    public float shakeFrequency = 18f;

    public void SetShakeAmplitude(float val)
    {
        shakeAmplitude = Mathf.Clamp(val, 0.005f, 0.25f);
    }

    [Header("Soft Follow")]
    [SerializeField] private bool followPlayerDirection = true;
    [SerializeField, Min(0f)] private float directionFollowSpeed = 90f;
    [SerializeField, Range(0f, 180f)] private float sharpTurnIgnoreAngle = 120f;

    [Header("Camera Collision")]
    [SerializeField] private LayerMask collisionLayers = ~0;
    [SerializeField, Min(0.01f)] private float collisionRadius = 0.25f;
    [SerializeField, Min(0.1f)] private float collisionPadding = 0.1f;
    [SerializeField, Min(0.1f)] private float minimumDistance = 1f;

    public static ThirdPersonCamera Instance { get; private set; }

    private float yaw;
    private float pitch;
    private CharacterController targetController;
    private float shakeNoiseSeed;
    private bool isAftershaking;
    private float aftershockCustomAmp = -1f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        shakeNoiseSeed = Random.value * 100f;
    }

    public void TriggerAftershock(float duration, float amplitude)
    {
        if (EarthquakeEffect.Instance != null)
        {
            EarthquakeEffect.Instance.TriggerAftershock(duration, amplitude);
        }
        StartCoroutine(AftershockRoutine(duration, amplitude));
    }

    private System.Collections.IEnumerator AftershockRoutine(float duration, float amplitude)
    {
        isAftershaking = true;
        aftershockCustomAmp = amplitude > 0f ? amplitude : 0.06f;
        yield return new WaitForSeconds(duration);
        isAftershaking = false;
        aftershockCustomAmp = -1f;
    }

    public void SetCameraDistance(float newDist)
    {
        distance = Mathf.Clamp(newDist, 1.2f, 6.0f);
        PlayerPrefs.SetFloat("CameraDistance", distance);
        PlayerPrefs.Save();
    }

    public void SetCameraHeight(float newHeight)
    {
        cameraHeightOffset = Mathf.Clamp(newHeight, 0f, 1.5f);
        PlayerPrefs.SetFloat("CameraHeight", cameraHeightOffset);
        PlayerPrefs.Save();
    }

    private void Start()
    {
        if (PlayerPrefs.HasKey("CameraDistance"))
        {
            distance = PlayerPrefs.GetFloat("CameraDistance", distance);
        }
        if (PlayerPrefs.HasKey("CameraHeight"))
        {
            cameraHeightOffset = PlayerPrefs.GetFloat("CameraHeight", cameraHeightOffset);
        }
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

        HandleZoomInput();

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

        ApplyEarthquakeShake();
    }

    private void ApplyEarthquakeShake()
    {
        if (!enableEarthquakeShake) return;

        bool isQuake = false;
        // Pusat pengaturan tunggal: Ambil dari EarthquakeEffect (GameObject 'Efek_Gempa') jika ada di scene
        float currentAmp = (EarthquakeEffect.Instance != null) ? EarthquakeEffect.Instance.shakeAmplitude : shakeAmplitude;

        // 1. Cek dari GameManager jika sedang Level 2 aktif
        if (GameManager.Instance != null && GameManager.Instance.isGameActive && GameManager.Instance.currentLevel == 2)
        {
            isQuake = true;
        }
        // 2. Cek dari gempa susulan coroutine / EarthquakeEffect
        else if (isAftershaking || (EarthquakeEffect.Instance != null && EarthquakeEffect.Instance.IsShaking))
        {
            isQuake = true;
            if (aftershockCustomAmp > 0f) currentAmp = aftershockCustomAmp;
        }

        if (!isQuake) return;

        float t = Time.time * shakeFrequency;
        float offsetX = (Mathf.PerlinNoise(shakeNoiseSeed, t) - 0.5f) * (2f * currentAmp);
        float offsetY = (Mathf.PerlinNoise(shakeNoiseSeed + 10.5f, t) - 0.5f) * (2f * currentAmp);
        float offsetZ = (Mathf.PerlinNoise(shakeNoiseSeed + 21.2f, t) - 0.5f) * (currentAmp);

        // Tambahkan offset translasi guncangan
        transform.position += transform.right * offsetX + transform.up * offsetY + transform.forward * offsetZ;

        // Tambahkan getaran rotasi (Roll & Pitch tremor)
        float roll = (Mathf.PerlinNoise(shakeNoiseSeed + 31.7f, t) - 0.5f) * (2f * shakeRollAngle);
        float pitchTremor = (Mathf.PerlinNoise(shakeNoiseSeed + 42.1f, t) - 0.5f) * (shakeRollAngle * 0.6f);
        transform.rotation *= Quaternion.Euler(pitchTremor, 0f, roll);
    }

    private void HandleZoomInput()
    {
        // 1. Mouse Scroll Wheel (PC / Editor)
        if (Mouse.current != null)
        {
            float scroll = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                distance = Mathf.Clamp(distance - Mathf.Sign(scroll) * 0.25f, 1.2f, 6.0f);
                PlayerPrefs.SetFloat("CameraDistance", distance);
            }
        }

        // 2. Pinch to Zoom (Touchscreen Mobile)
        if (Touchscreen.current != null && Touchscreen.current.touches.Count >= 2)
        {
            var t0 = Touchscreen.current.touches[0];
            var t1 = Touchscreen.current.touches[1];
            if (t0.press.isPressed && t1.press.isPressed)
            {
                Vector2 pos0 = t0.position.ReadValue();
                Vector2 pos1 = t1.position.ReadValue();
                Vector2 prevPos0 = pos0 - t0.delta.ReadValue();
                Vector2 prevPos1 = pos1 - t1.delta.ReadValue();

                float prevDist = (prevPos0 - prevPos1).magnitude;
                float currentDist = (pos0 - pos1).magnitude;
                float diff = currentDist - prevDist;

                if (Mathf.Abs(diff) > 0.5f)
                {
                    distance = Mathf.Clamp(distance - diff * 0.008f, 1.2f, 6.0f);
                    PlayerPrefs.SetFloat("CameraDistance", distance);
                }
            }
        }
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
