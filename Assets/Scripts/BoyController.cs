using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Menggerakkan karakter dengan WASD atau tombol panah menggunakan CharacterController.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class BoyController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField, Min(0f)] private float moveSpeed = 4f;
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
    [SerializeField, Min(0.1f)] private float crouchHeight = 0.5f;

    [Tooltip("Tinggi pusat collider (Center Y) saat merunduk. Perkecil angka ini jika ingin collider lebih turun ke bawah mendekati lantai.")]
    [SerializeField] private float crouchCenterY = 0.29f;

    [Tooltip("Offset maju-mundur (Center Z) saat merunduk (misal jika badan membungkuk ke depan).")]
    [SerializeField] private float crouchCenterZ = 0.05f;

    private CharacterController characterController;
    private float verticalVelocity;
    private Transform cameraTransform;
    private bool isCrouching;

    private float standingHeight = 0.99f;
    private Vector3 standingCenter = new Vector3(0f, 0.58f, 0f);

    public bool IsCrouching => isCrouching;

    public void SetCrouching(bool crouch)
    {
        if (isCrouching == crouch)
        {
            return;
        }

        isCrouching = crouch;

        // Sesuaikan ukuran CharacterController
        ApplyCrouchCollider(isCrouching);

        if (animator != null)
        {
            animator.SetBool(crouchParameter, crouch);
        }
    }

    private void ApplyCrouchCollider(bool crouching)
    {
        if (characterController == null)
        {
            return;
        }

        if (crouching)
        {
            characterController.height = crouchHeight;
            characterController.center = new Vector3(standingCenter.x, crouchCenterY, crouchCenterZ);
        }
        else
        {
            characterController.height = standingHeight;
            characterController.center = standingCenter;
        }
    }

    private void OnValidate()
    {
        // Memungkinkan tuning nilai crouchCenterY & crouchHeight secara LIVE saat Play Mode!
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
        characterController = GetComponent<CharacterController>();
        if (characterController != null)
        {
            standingHeight = characterController.height;
            standingCenter = characterController.center;
        }

        cameraTransform = Camera.main != null ? Camera.main.transform : null;
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (virtualJoystick == null)
        {
            virtualJoystick = FindFirstObjectByType<VirtualJoystick>();
        }
    }

    private void Update()
    {
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

        if (characterController.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = groundedVelocity;
        }

        verticalVelocity += gravity * Time.deltaTime;

        // Kecepatan berbeda saat menunduk atau lari
        float actualSpeed = moveSpeed * (isCrouching ? 0.5f : 1f);
        Vector3 finalMove = movement * actualSpeed;
        finalMove.y = verticalVelocity;

        characterController.Move(finalMove * Time.deltaTime);
    }

    private Vector2 ReadMoveInput()
    {
        Vector2 keyboardInput = Vector2.zero;
        if (Keyboard.current != null)
        {
            Keyboard keyboard = Keyboard.current;
            float horizontal = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1f : 0f)
                             - (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1f : 0f);
            float vertical = (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1f : 0f)
                           - (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1f : 0f);

            keyboardInput = new Vector2(horizontal, vertical);
            if (keyboardInput.sqrMagnitude > 0f)
            {
                // Di PC: WASD biasa = Jalan (0.5), tahan Shift = Lari (1.0)
                float targetMag = keyboard.leftShiftKey.isPressed ? 1f : 0.5f;
                keyboardInput = keyboardInput.normalized * targetMag;
            }

            // Toggle crouch dengan tombol C atau Ctrl di PC
            if (keyboard.cKey.wasPressedThisFrame)
            {
                SetCrouching(!isCrouching);
            }
        }

        Vector2 joystickInput = virtualJoystick != null ? virtualJoystick.InputDirection : Vector2.zero;
        Vector2 combined = keyboardInput + joystickInput;

        return Vector2.ClampMagnitude(combined, 1f);
    }

    private Vector3 GetCameraRelativeMovement(Vector2 input)
    {
        if (cameraTransform == null)
        {
            return new Vector3(input.x, 0f, input.y);
        }

        // Hilangkan kemiringan kamera agar karakter tetap bergerak di atas lantai.
        Vector3 cameraForward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
        Vector3 cameraRight = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;
        return (cameraForward * input.y + cameraRight * input.x);
    }

    private void UpdateMovementAnimation(float speedMagnitude)
    {
        if (animator != null)
        {
            // Saat menunduk: jika diam (magnitude < 0.05) beri nilai 0 agar animasi TER-PAUSE otomatis.
            // Saat bergerak, beri kecepatan 1 agar langkah menunduk berjalan normal.
            float animSpeed = isCrouching ? (speedMagnitude > 0.05f ? 1f : 0f) : speedMagnitude;
            animator.SetFloat(speedParameter, animSpeed);
        }
    }
}
