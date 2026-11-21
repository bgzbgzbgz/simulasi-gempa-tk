using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Menggerakkan karakter dengan WASD / Panah Keyboard (mendukung New & Old Input System)
/// serta Virtual Joystick UI menggunakan CharacterController.
/// Dilengkapi sistem Anti-Stuck fisik presisi dan dual-input bridge agar selalu bisa jalan di segala kondisi.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class BoyController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField, Min(0f)] private float moveSpeed = 4.5f;
    [SerializeField, Min(0f)] private float turnSpeed = 720f;

    [Header("Gravity")]
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float groundedVelocity = -2f;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string speedParameter = "Speed";
    [SerializeField] private string crouchParameter = "isCrouching";

    [Header("Touch / Mobile")]
    [SerializeField] private VirtualJoystick virtualJoystick;

    [Header("Crouch Collider Settings")]
    [Tooltip("Tinggi collider saat posisi merunduk.")]
    [SerializeField, Min(0.1f)] private float crouchHeight = 0.52f;

    [Tooltip("Tinggi pusat collider (Center Y) saat merunduk.")]
    [SerializeField] private float crouchCenterY = 0.30f;

    [Tooltip("Offset maju-mundur (Center Z) saat merunduk.")]
    [SerializeField] private float crouchCenterZ = 0.05f;

    private CharacterController characterController;
    private float verticalVelocity;
    private Transform cameraTransform;
    private bool isCrouching;

    private float standingHeight = 0.99f;
    private Vector3 standingCenter = new Vector3(0f, 0.52f, 0f);

#if ENABLE_INPUT_SYSTEM
    private InputAction moveAction;
    private InputAction crouchAction;
#endif

    public bool IsCrouching => isCrouching;

    /// <summary>
    /// Memeriksa apakah di atas kepala karakter aman untuk berdiri tanpa menabrak kolong meja atau plafon.
    /// </summary>
    public bool CanStandUp()
    {
        float safeCrouchH = Mathf.Max(0.5f, crouchHeight);
        float safeStandH = Mathf.Max(0.9f, standingHeight);
        Vector3 origin = transform.position + Vector3.up * safeCrouchH;
        float distance = safeStandH - safeCrouchH + 0.05f;
        float radius = characterController != null ? characterController.radius * 0.7f : 0.18f;

        RaycastHit[] hits = Physics.SphereCastAll(origin, radius, Vector3.up, distance, ~0, QueryTriggerInteraction.Ignore);
        foreach (var hit in hits)
        {
            if (hit.collider == null || hit.collider.gameObject == gameObject || hit.collider.transform.IsChildOf(transform))
                continue;
            return false;
        }

        Collider[] overlaps = Physics.OverlapSphere(origin + Vector3.up * (distance * 0.5f), radius);
        foreach (var col in overlaps)
        {
            if (col == null || col.gameObject == gameObject || col.transform.IsChildOf(transform)) continue;

            if (col.isTrigger)
            {
                CheckpointTrigger cp = col.GetComponent<CheckpointTrigger>();
                if (cp != null && cp.requireCrouch)
                {
                    return false;
                }
            }
        }

        return true;
    }

    public void SetCrouching(bool crouch)
    {
        if (!crouch && isCrouching && !CanStandUp())
        {
            return;
        }

        if (isCrouching == crouch)
        {
            return;
        }

        isCrouching = crouch;
        ApplyCrouchCollider(isCrouching);

        if (animator != null)
        {
            animator.SetBool(crouchParameter, crouch);
        }
    }

    private void ApplyCrouchCollider(bool crouching)
    {
        if (characterController == null) return;

        float effectiveCrouchH = Mathf.Max(0.52f, crouchHeight);
        float effectiveStandH = Mathf.Max(0.95f, standingHeight);

        if (crouching)
        {
            characterController.height = effectiveCrouchH;
            characterController.center = new Vector3(standingCenter.x, Mathf.Max(0.28f, crouchCenterY), crouchCenterZ);
        }
        else
        {
            characterController.height = effectiveStandH;
            characterController.center = standingCenter;
        }
    }

    private void OnValidate()
    {
        if (Application.isPlaying && characterController != null && isCrouching)
        {
            ApplyCrouchCollider(true);
        }
    }

    public void ToggleCrouch()
    {
        SetCrouching(!isCrouching);
    }

    private void Awake()
    {
        this.enabled = true;
        ConfigurePhysics();

        cameraTransform = Camera.main != null ? Camera.main.transform : null;

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
        if (animator != null)
        {
            animator.applyRootMotion = false;
        }

        if (virtualJoystick == null)
        {
            virtualJoystick = FindFirstObjectByType<VirtualJoystick>(FindObjectsInactive.Include);
        }

        if (moveSpeed < 3.5f)
        {
            moveSpeed = 4.5f;
        }

#if ENABLE_INPUT_SYSTEM
        SetupInputActions();
#endif
    }

#if ENABLE_INPUT_SYSTEM
    private void SetupInputActions()
    {
        if (moveAction == null)
        {
            moveAction = new InputAction("Move", InputActionType.Value);
            moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");
        }
        moveAction.Enable();

        if (crouchAction == null)
        {
            crouchAction = new InputAction("Crouch", InputActionType.Button, "<Keyboard>/c");
            crouchAction.performed += ctx => ToggleCrouch();
        }
        crouchAction.Enable();
    }
#endif

    private void ConfigurePhysics()
    {
        characterController = GetComponent<CharacterController>();
        if (characterController != null)
        {
            characterController.enabled = true;
            characterController.height = 0.99f;
            characterController.center = new Vector3(0f, 0.52f, 0f);
            characterController.radius = 0.20f;
            characterController.skinWidth = 0.015f;
            characterController.stepOffset = 0.3f;
            characterController.minMoveDistance = 0f;
            characterController.slopeLimit = 45f;

            standingHeight = characterController.height;
            standingCenter = characterController.center;
        }
    }

    private void OnEnable()
    {
        this.enabled = true;
#if ENABLE_INPUT_SYSTEM
        SetupInputActions();
#endif
        if (animator != null)
        {
            animator.applyRootMotion = false;
        }
    }

    public void StopMovement()
    {
        verticalVelocity = 0f;
        if (animator != null)
        {
            animator.SetFloat(speedParameter, 0f);
        }
    }

    private void OnDisable()
    {
#if ENABLE_INPUT_SYSTEM
        moveAction?.Disable();
        crouchAction?.Disable();
#endif
        StopMovement();
    }

    private void Start()
    {
        this.enabled = true;
        ConfigurePhysics();

        if (animator != null)
        {
            animator.applyRootMotion = false;
        }

        // Sedikit angkat karakter di awal agar mendarat mulus tanpa penetrasi lantai
        if (transform.position.y < 0.2f)
        {
            transform.position = new Vector3(transform.position.x, 0.22f, transform.position.z);
        }

        // Auto-fix: pastikan karakter Boy menggunakan material bertekstur
        SkinnedMeshRenderer smr = GetComponentInChildren<SkinnedMeshRenderer>();
        if (smr != null && (smr.sharedMaterial == null || smr.sharedMaterial.name.Contains("Default") || smr.sharedMaterial.name == "Lit"))
        {
#if UNITY_EDITOR
            Material boyMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_Boy.mat");
            if (boyMat != null)
            {
                smr.sharedMaterial = boyMat;
                Debug.Log("[BoyController] Auto-assign Mat_Boy pada SkinnedMeshRenderer.");
            }
#endif
        }

        // Auto-fix: pastikan meja belajar utama memiliki BoxCollider padat di daun mejanya
        GameObject table = GameObject.Find("Meja_Belajar_2");
        if (table != null && table.GetComponent<Collider>() == null)
        {
            BoxCollider bc = table.AddComponent<BoxCollider>();
            bc.isTrigger = false;
            bc.size = new Vector3(2.2f, 0.08f, 0.9f);
            bc.center = new Vector3(0f, 0.5f, 0f);
            Debug.Log("[BoyController] Auto-setup BoxCollider daun meja padat pada Meja_Belajar_2.");
        }
    }

    private void Update()
    {
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
        if (virtualJoystick == null)
        {
            virtualJoystick = FindFirstObjectByType<VirtualJoystick>(FindObjectsInactive.Include);
        }

        if (animator != null && animator.applyRootMotion)
        {
            animator.applyRootMotion = false;
        }

        Vector2 input = ReadMoveInput();
        Vector3 movement = GetCameraRelativeMovement(input);
        float currentSpeedMagnitude = input.magnitude;

        UpdateMovementAnimation(currentSpeedMagnitude);

        if (movement.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(movement.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                turnSpeed * Time.deltaTime);
        }

        if (characterController != null)
        {
            if (characterController.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = groundedVelocity;
            }

            verticalVelocity += gravity * Time.deltaTime;

            float actualSpeed = moveSpeed * (isCrouching ? 0.55f : 1f);
            Vector3 finalMove = movement * actualSpeed;
            finalMove.y = verticalVelocity;

            characterController.Move(finalMove * Time.deltaTime);

            // Anti-Stuck Fisik Presisi: periksa apakah gerak horizontal terblokir oleh collider
            Vector3 horizontalVel = Vector3.ProjectOnPlane(characterController.velocity, Vector3.up);
            if (movement.sqrMagnitude > 0.01f && horizontalVel.sqrMagnitude < 0.001f)
            {
                // PhysX mengunci pergerakan karakter, paksa translasi langsung agar karakter bebas bergerak
                transform.position += movement * (actualSpeed * Time.deltaTime);
            }
        }
        else
        {
            // Fallback jika tidak ada CharacterController
            float actualSpeed = moveSpeed * (isCrouching ? 0.55f : 1f);
            transform.position += movement * (actualSpeed * Time.deltaTime);
        }
    }

    private Vector2 ReadMoveInput()
    {
        Vector2 input = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
        // 1. Direct New Input System InputAction (paling stabil & tidak tergantung Keyboard.current)
        if (moveAction != null)
        {
            input = moveAction.ReadValue<Vector2>();
        }

        // 2. Direct Keyboard check jika InputAction belum menangkap
        if (input.sqrMagnitude < 0.001f)
        {
            try
            {
                var kb = Keyboard.current;
                if (kb != null)
                {
                    float h = 0f;
                    float v = 0f;
                    if (kb.wKey.isPressed || kb.upArrowKey.isPressed) v += 1f;
                    if (kb.sKey.isPressed || kb.downArrowKey.isPressed) v -= 1f;
                    if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) h += 1f;
                    if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) h -= 1f;

                    if (kb.cKey.wasPressedThisFrame)
                    {
                        ToggleCrouch();
                    }
                    input = new Vector2(h, v);
                }
            }
            catch { }
        }
#endif

        // 3. Fallback Legacy Input jika didukung environment
        if (input.sqrMagnitude < 0.001f)
        {
            try
            {
                float lh = 0f;
                float lv = 0f;
                if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) lv += 1f;
                if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) lv -= 1f;
                if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) lh += 1f;
                if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) lh -= 1f;

                if (Mathf.Approximately(lh, 0f) && Mathf.Approximately(lv, 0f))
                {
                    lh = Input.GetAxisRaw("Horizontal");
                    lv = Input.GetAxisRaw("Vertical");
                }

                if (Input.GetKeyDown(KeyCode.C))
                {
                    ToggleCrouch();
                }
                input = new Vector2(lh, lv);
            }
            catch { }
        }

        if (input.sqrMagnitude > 0.01f)
        {
            input = input.normalized;
        }

        // 4. Virtual Joystick UI (Mobile touch & mouse drag)
        if (virtualJoystick == null)
        {
            virtualJoystick = FindFirstObjectByType<VirtualJoystick>(FindObjectsInactive.Include);
        }

        Vector2 joystickInput = virtualJoystick != null ? virtualJoystick.InputDirection : Vector2.zero;
        Vector2 combined = input + joystickInput;

        return Vector2.ClampMagnitude(combined, 1f);
    }

    private Vector3 GetCameraRelativeMovement(Vector2 input)
    {
        if (input.sqrMagnitude < 0.001f)
        {
            return Vector3.zero;
        }

        if (cameraTransform == null)
        {
            return new Vector3(input.x, 0f, input.y);
        }

        Vector3 cameraForward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
        Vector3 cameraRight = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;
        Vector3 move = (cameraForward * input.y + cameraRight * input.x);

        // Fallback jika kemiringan kamera membuat proyeksi nol
        if (move.sqrMagnitude < 0.001f)
        {
            return new Vector3(input.x, 0f, input.y);
        }

        return move;
    }

    private void UpdateMovementAnimation(float speedMagnitude)
    {
        if (animator != null)
        {
            float animSpeed = isCrouching ? (speedMagnitude > 0.05f ? 1f : 0f) : speedMagnitude;
            animator.SetFloat(speedParameter, animSpeed);
        }
    }

}
