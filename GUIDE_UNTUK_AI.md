# Guide: Game Simulasi Edukasi Evakuasi Gempa Bumi untuk Anak TK

> File ini adalah konteks lengkap project Tugas Akhir. Berikan file ini ke AI (Claude/ChatGPT/Gemini) sebagai context pertama sebelum minta bantuan coding.

---

## Deskripsi Project

**Judul TA:** Game Simulasi Edukasi Evakuasi Gempa Bumi untuk Anak TK  
**Engine:** Unity (C#), target build **Android APK**  
**Target user:** Anak usia Taman Kanak-Kanak  
**Repo GitHub:** *(sudah ada di GitHub)*

Game ini mensimulasikan prosedur evakuasi gempa bumi di lingkungan kelas TK. Pemain mengendalikan karakter anak laki-laki (third-person 3D) melalui 3 level berurutan:

| Level | Nama | Deskripsi |
|-------|------|-----------|
| 1 | Kenali Bencana | Kuis visual 3 soal pilihan gambar — kenali tanda gempa |
| 2 | Drop & Cover | Gempa terjadi → cari kolong meja → runduk & tahan posisi |
| 3 | Evakuasi | Gempa reda → ikuti jalur evakuasi ke titik kumpul di lapangan |

Penilaian berdasarkan **total waktu** (bintang 1-3) dan **poin kuis** (+10 per jawaban benar).

---

## Arsitektur & Konvensi Kode

- **Singleton pattern:** `GameManager.Instance` mengatur seluruh alur level
- **Event-driven:** Komponen memanggil method di GameManager saat objektif selesai
- **Null-check defensif:** Semua reference dicek null sebelum dipakai
- **Inspector-friendly:** Pakai `[Header]`, `[Tooltip]`, `[SerializeField]`
- **Komentar Bahasa Indonesia** di summary dan inline
- **UI:** Pakai `UnityEngine.UI` (uGUI), bukan UI Toolkit
- **Input:** `UnityEngine.InputSystem` (New Input System)
- **Tidak ada package tambahan** di luar bawaan Unity

---

## Progress Saat Ini

### ✅ Sudah Jadi
1. Karakter 3D (`boy.fbx`) — animasi Idle, Walk, Run, Crouch
2. Kontrol karakter PC + Mobile (`BoyController.cs` + `VirtualJoystick.cs` + `CrouchButton.cs`)
3. Kamera third-person orbit dengan collision (`ThirdPersonCamera.cs`)
4. Environment kelas TK dari denah FBX (`denah_tes.fbx`)
5. Level 1 kuis visual 3 soal (`Level1QuizManager.cs`)
6. Game Manager alur level + scoring bintang (`GameManager.cs`)
7. Mission UI HUD, timer, hold progress (`MissionUI.cs`)
8. Zona perlindungan kolong meja (`ShelterZone.cs`)
9. Checkpoint trigger dengan hold timer dan syarat crouch (`CheckpointTrigger.cs`)

### ❌ Belum Jadi (Butuh Bantuan)
1. **Efek simulasi gempa** — camera shake, overlay visual, suara gemuruh, benda jatuh
2. **Level 3: Sistem evakuasi** — waypoint/pathfinding ke titik kumpul di lapangan
3. **Polish UI scoring akhir** — animasi bintang, panel hasil yang lebih menarik untuk anak TK
4. **Bug fix & optimasi** — Android build, performance
5. **Audio** — integrasi suara narasi, SFX gempa, feedback

---

## Source Code Lengkap

### GameManager.cs
```csharp
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Mengatur alur level/misi evakuasi (Level 1, 2, 3), stopwatch waktu, poin, dan skor akhir.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Status Level")]
    [Tooltip("Level yang sedang aktif (1 = Kuis Bencana, 2 = Lindungi Diri di Meja, 3 = Evakuasi ke Lapangan)")]
    public int currentLevel = 1;
    public int maxLevel = 3;
    public bool isGameActive = false;

    [Header("Poin Gamifikasi")]
    public int currentScore = 0;

    [Header("Instruksi Setiap Level")]
    public string[] levelInstructions = new string[]
    {
        "Level 1: Kenali Bencana Gempa! Jawab 3 kuis visual di layar.",
        "Level 2: Terjadi Gempa! Segera cari kolong meja dan RUNDUK untuk berlindung!",
        "Level 3: Gempa reda! Berbaris di belakang Guru menuju Titik Kumpul di lapangan!"
    };

    [Header("Catatan Waktu & Penilaian")]
    public float totalElapsedTime = 0f;
    public float goldTimeTarget = 60f;
    public float silverTimeTarget = 120f;

    [Header("Komponen Terkait")]
    public MissionUI missionUI;
    public Level1QuizManager quizManager;
    public BoyController playerBoy;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (playerBoy == null)
        {
            playerBoy = FindFirstObjectByType<BoyController>();
        }
    }

    private void Start()
    {
        StartGame();
    }

    private void Update()
    {
        if (isGameActive)
        {
            totalElapsedTime += Time.deltaTime;
            if (missionUI != null)
            {
                missionUI.UpdateTimerDisplay(totalElapsedTime);
            }
        }
    }

    /// <summary>
    /// Memulai alur simulasi evakuasi.
    /// </summary>
    public void StartGame()
    {
        currentLevel = 1;
        totalElapsedTime = 0f;
        currentScore = 0;
        isGameActive = true;

        UpdateMissionStatus();

        // Mulai kuis Level 1 jika ada
        if (quizManager != null)
        {
            SetPlayerMovementEnabled(false);
            quizManager.StartQuiz();
        }
    }

    public void AddScore(int amount)
    {
        currentScore += amount;
        Debug.Log($"[GameManager] +{amount} Poin! Total Skor: {currentScore}");
    }

    /// <summary>
    /// Dipanggil otomatis oleh Level1QuizManager saat 3 soal selesai dijawab.
    /// </summary>
    public void OnLevel1QuizFinished()
    {
        Debug.Log("[GameManager] Kuis Level 1 Selesai!");
        CompleteLevel(1);
    }

    /// <summary>
    /// Dipanggil saat pemain menyelesaikan objektif pada level tertentu.
    /// </summary>
    public void CompleteLevel(int levelIndex)
    {
        if (levelIndex != currentLevel || !isGameActive)
        {
            return;
        }

        Debug.Log($"[GameManager] Level {levelIndex} Selesai!");

        if (currentLevel >= maxLevel)
        {
            FinishGame();
        }
        else
        {
            isGameActive = false;
            SetPlayerMovementEnabled(false);

            if (missionUI != null)
            {
                string title = $"LEVEL {currentLevel} SELESAI!";
                string desc = GetCompletionMessage(currentLevel);
                missionUI.ShowLevelComplete(title, desc);
            }
        }
    }

    /// <summary>
    /// Lanjut ke level berikutnya (dipanggil dari tombol Lanjut pada UI).
    /// </summary>
    public void NextLevel()
    {
        currentLevel++;
        isGameActive = true;

        if (missionUI != null)
        {
            missionUI.HideLevelComplete();
        }

        // Aktifkan kembali kontrol pemain saat masuk Level 2 & 3
        SetPlayerMovementEnabled(true);
        UpdateMissionStatus();
    }

    private void FinishGame()
    {
        isGameActive = false;
        SetPlayerMovementEnabled(false);

        int stars = 1;
        string evalText = "Cukup Baik! Tetap waspada dan percepat evakuasi.";

        if (totalElapsedTime <= goldTimeTarget)
        {
            stars = 3;
            evalText = "Luar Biasa! Respon evakuasi sangat cepat dan tepat! Kamu adalah Pahlawan Gempa!";
        }
        else if (totalElapsedTime <= silverTimeTarget)
        {
            stars = 2;
            evalText = "Bagus! Kamu berhasil evakuasi dengan selamat!";
        }

        if (missionUI != null)
        {
            missionUI.ShowFinalScore(totalElapsedTime, stars, evalText, currentScore);
        }
    }

    private void SetPlayerMovementEnabled(bool enabled)
    {
        if (playerBoy != null)
        {
            playerBoy.enabled = enabled;
        }
    }

    private void UpdateMissionStatus()
    {
        if (missionUI == null) return;

        int index = currentLevel - 1;
        string instruction = (index >= 0 && index < levelInstructions.Length)
            ? levelInstructions[index]
            : $"Lanjutkan Evakuasi - Tahap {currentLevel}";

        missionUI.SetInstruction(instruction);
        missionUI.ResetHoldProgress();
    }

    private string GetCompletionMessage(int level)
    {
        switch (level)
        {
            case 1:
                return "Hebat! Kamu berhasil mengenali tanda-tanda dan bahaya gempa bumi!";
            case 2:
                return "Pintar sekali! Kamu berhasil melindungi kepala dan berlindung di kolong meja!";
            default:
                return "Tahap ini berhasil diselesaikan!";
        }
    }

    public void RestartGame()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.buildIndex);
    }

    public void GoToMainMenu()
    {
        if (Application.CanStreamedLevelBeLoaded("MainMenu"))
        {
            SceneManager.LoadScene("MainMenu");
        }
        else
        {
            SceneManager.LoadScene(0);
        }
    }
}
```

### BoyController.cs
```csharp
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

    [Tooltip("Tinggi pusat collider (Center Y) saat merunduk.")]
    [SerializeField] private float crouchCenterY = 0.29f;

    [Tooltip("Offset maju-mundur (Center Z) saat merunduk.")]
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
        if (isCrouching == crouch) return;
        isCrouching = crouch;
        ApplyCrouchCollider(isCrouching);
        if (animator != null) animator.SetBool(crouchParameter, crouch);
    }

    private void ApplyCrouchCollider(bool crouching)
    {
        if (characterController == null) return;
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
        if (Application.isPlaying && characterController != null && isCrouching)
            ApplyCrouchCollider(true);
    }

    public void ToggleCrouch() => SetCrouching(!isCrouching);

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        if (characterController != null)
        {
            standingHeight = characterController.height;
            standingCenter = characterController.center;
        }
        cameraTransform = Camera.main != null ? Camera.main.transform : null;
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (virtualJoystick == null) virtualJoystick = FindFirstObjectByType<VirtualJoystick>();
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
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }

        if (characterController.isGrounded && verticalVelocity < 0f)
            verticalVelocity = groundedVelocity;

        verticalVelocity += gravity * Time.deltaTime;
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
                float targetMag = keyboard.leftShiftKey.isPressed ? 1f : 0.5f;
                keyboardInput = keyboardInput.normalized * targetMag;
            }
            if (keyboard.cKey.wasPressedThisFrame) SetCrouching(!isCrouching);
        }

        Vector2 joystickInput = virtualJoystick != null ? virtualJoystick.InputDirection : Vector2.zero;
        return Vector2.ClampMagnitude(keyboardInput + joystickInput, 1f);
    }

    private Vector3 GetCameraRelativeMovement(Vector2 input)
    {
        if (cameraTransform == null) return new Vector3(input.x, 0f, input.y);
        Vector3 cameraForward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
        Vector3 cameraRight = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;
        return (cameraForward * input.y + cameraRight * input.x);
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
```

### ThirdPersonCamera.cs
```csharp
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
        if (target == null) return;

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
        if (!followPlayerDirection || targetController == null) return;
        Vector3 horizontalVelocity = Vector3.ProjectOnPlane(targetController.velocity, Vector3.up);
        if (horizontalVelocity.sqrMagnitude < 0.01f) return;
        float playerYaw = target.eulerAngles.y;
        float yawDifference = Mathf.Abs(Mathf.DeltaAngle(yaw, playerYaw));
        if (yawDifference > sharpTurnIgnoreAngle) return;
        yaw = Mathf.MoveTowardsAngle(yaw, playerYaw, directionFollowSpeed * Time.deltaTime);
    }

    private Vector2 ReadOrbitInput()
    {
        if (Mouse.current != null && Mouse.current.rightButton.isPressed)
            return Mouse.current.delta.ReadValue();

        if (Touchscreen.current != null)
        {
            var touches = Touchscreen.current.touches;
            for (int i = 0; i < touches.Count; i++)
            {
                var touch = touches[i];
                if (touch.press.isPressed)
                {
                    if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.touchId.ReadValue()))
                        continue;
                    Vector2 touchPosition = touch.position.ReadValue();
                    if (touchPosition.x >= Screen.width * 0.5f)
                        return touch.delta.ReadValue();
                }
            }
        }
        return Vector2.zero;
    }

    private float GetClearCameraDistance(Vector3 pivot, Vector3 cameraDirection, float requestedDistance)
    {
        RaycastHit[] hits = Physics.SphereCastAll(pivot, collisionRadius, cameraDirection, requestedDistance, collisionLayers, QueryTriggerInteraction.Ignore);
        float clearDistance = requestedDistance;
        foreach (RaycastHit hit in hits)
        {
            if (hit.transform == target || hit.transform.IsChildOf(target) || target.IsChildOf(hit.transform))
                continue;
            clearDistance = Mathf.Min(clearDistance, hit.distance - collisionPadding);
        }
        return Mathf.Max(minimumDistance, clearDistance);
    }
}
```

### Level1QuizManager.cs
```csharp
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Mengelola kuis visual 3 soal interaktif untuk Level 1 (Kenali Bencana Gempa).
/// Cocok untuk anak usia TK (bebas teks rumit, tombol kartu besar, dipandu audio).
/// </summary>
public class Level1QuizManager : MonoBehaviour
{
    [Serializable]
    public class QuizQuestion
    {
        public string questionPrompt = "Pertanyaan Kuis";
        public AudioClip questionAudio;
        public string[] optionLabels = new string[3];
        public Sprite[] optionIcons = new Sprite[3];
        public AudioClip[] optionAudios = new AudioClip[3];
        public int correctOptionIndex = 0;
        public string correctFeedback = "Tepat sekali!";
        public string wrongFeedback = "Hmm, coba perhatikan lagi ya!";
    }

    [Header("Daftar Soal")]
    public QuizQuestion[] questions = new QuizQuestion[3];

    [Header("UI Reference")]
    public GameObject quizPanel;
    public Text promptText;
    public Button[] optionButtons = new Button[3];
    public Text[] optionTexts = new Text[3];
    public Image[] optionImages = new Image[3];
    public Text feedbackText;

    [Header("Audio Reference")]
    public AudioSource voiceAudioSource;
    public AudioSource sfxAudioSource;
    public AudioClip correctSfx;
    public AudioClip wrongSfx;

    [Header("Status Kuis")]
    public int currentQuestionIndex = 0;
    private bool isAnsweringLocked = false;

    private void Awake()
    {
        if (questions == null || questions.Length == 0)
            SetupDefaultQuestions();

        for (int i = 0; i < optionButtons.Length; i++)
        {
            int index = i;
            if (optionButtons[i] != null)
                optionButtons[i].onClick.AddListener(() => OnOptionClicked(index));
        }
    }

    public void SetupDefaultQuestions()
    {
        questions = new QuizQuestion[3];

        questions[0] = new QuizQuestion
        {
            questionPrompt = "Rumah manakah yang sedang terkena gempa bumi?",
            optionLabels = new[] { "Rumah A (Api / Terbakar)", "Rumah B (Berguncang / Gempa)", "Rumah C (Banjir / Air)" },
            correctOptionIndex = 1,
            correctFeedback = "Tepat sekali! Rumah B berguncang karena gempa bumi!",
            wrongFeedback = "Bukan yang itu. Cari rumah yang berguncang ya!"
        };

        questions[1] = new QuizQuestion
        {
            questionPrompt = "Saat gempa, benda apa di dalam kelas yang bergoyang?",
            optionLabels = new[] { "Lampu Gantung (Bergoyang)", "Bola di Lantai (Diam)", "Buku di Rak (Diam)" },
            correctOptionIndex = 0,
            correctFeedback = "Pintar! Lampu gantung akan berayun saat gempa!",
            wrongFeedback = "Coba perhatikan benda yang bergoyang ke kiri dan kanan!"
        };

        questions[2] = new QuizQuestion
        {
            questionPrompt = "Dengarkan suara berikut, mana suara gemuruh gempa?",
            optionLabels = new[] { "Suara A (Gemuruh Gempa)", "Suara B (Hujan Deras)", "Suara C (Petir)" },
            correctOptionIndex = 0,
            correctFeedback = "Hebat! Itu adalah suara gemuruh getaran gempa!",
            wrongFeedback = "Bukan yang itu. Coba dengarkan suara gemuruh bumi!"
        };
    }

    public void StartQuiz()
    {
        if (quizPanel != null) quizPanel.SetActive(true);
        currentQuestionIndex = 0;
        isAnsweringLocked = false;
        DisplayCurrentQuestion();
    }

    private void DisplayCurrentQuestion()
    {
        if (currentQuestionIndex >= questions.Length) { CompleteQuiz(); return; }

        QuizQuestion q = questions[currentQuestionIndex];
        if (promptText != null) promptText.text = $"Soal {currentQuestionIndex + 1}/3:\n{q.questionPrompt}";
        if (feedbackText != null) feedbackText.text = "";

        if (voiceAudioSource != null && q.questionAudio != null)
        {
            voiceAudioSource.Stop();
            voiceAudioSource.clip = q.questionAudio;
            voiceAudioSource.Play();
        }

        for (int i = 0; i < 3; i++)
        {
            if (i < optionTexts.Length && optionTexts[i] != null && i < q.optionLabels.Length)
                optionTexts[i].text = q.optionLabels[i];
            if (i < optionImages.Length && optionImages[i] != null && i < q.optionIcons.Length && q.optionIcons[i] != null)
            {
                optionImages[i].sprite = q.optionIcons[i];
                optionImages[i].enabled = true;
            }
            if (i < optionButtons.Length && optionButtons[i] != null)
                optionButtons[i].interactable = true;
        }
        isAnsweringLocked = false;
    }

    public void OnOptionClicked(int optionIndex)
    {
        if (isAnsweringLocked || currentQuestionIndex >= questions.Length) return;
        QuizQuestion q = questions[currentQuestionIndex];

        if (q.optionAudios != null && optionIndex < q.optionAudios.Length && q.optionAudios[optionIndex] != null)
            if (sfxAudioSource != null) sfxAudioSource.PlayOneShot(q.optionAudios[optionIndex]);

        if (optionIndex == q.correctOptionIndex)
            StartCoroutine(HandleCorrectAnswer(q));
        else
            StartCoroutine(HandleWrongAnswer(q));
    }

    private IEnumerator HandleCorrectAnswer(QuizQuestion q)
    {
        isAnsweringLocked = true;
        if (feedbackText != null) { feedbackText.color = new Color(0.2f, 0.9f, 0.3f); feedbackText.text = q.correctFeedback; }
        if (sfxAudioSource != null && correctSfx != null) sfxAudioSource.PlayOneShot(correctSfx);
        if (GameManager.Instance != null) GameManager.Instance.AddScore(10);
        yield return new WaitForSeconds(1.6f);
        currentQuestionIndex++;
        DisplayCurrentQuestion();
    }

    private IEnumerator HandleWrongAnswer(QuizQuestion q)
    {
        if (feedbackText != null) { feedbackText.color = new Color(1f, 0.4f, 0.4f); feedbackText.text = q.wrongFeedback; }
        if (sfxAudioSource != null && wrongSfx != null) sfxAudioSource.PlayOneShot(wrongSfx);
        yield return new WaitForSeconds(1.2f);
    }

    private void CompleteQuiz()
    {
        if (quizPanel != null) quizPanel.SetActive(false);
        if (GameManager.Instance != null) GameManager.Instance.OnLevel1QuizFinished();
    }
}
```

### ShelterZone.cs
```csharp
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Trigger zona perlindungan (kolong meja) untuk Level 2 (Drop, Cover, Hold On).
/// Mendeteksi saat karakter masuk ke bawah meja dan memberikan feedback visual/event.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ShelterZone : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private string zoneName = "Kolong Meja Siswa";
    [SerializeField] private bool showDebugGUI = true;

    [Header("Events")]
    public UnityEvent onEnterShelter;
    public UnityEvent onExitShelter;

    public bool IsPlayerInside { get; private set; }

    private void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsPlayer(other))
        {
            IsPlayerInside = true;
            BoyController boy = other.GetComponent<BoyController>() ?? other.GetComponentInParent<BoyController>();
            if (boy != null) boy.SetCrouching(true);
            onEnterShelter?.Invoke();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsPlayer(other))
        {
            IsPlayerInside = false;
            BoyController boy = other.GetComponent<BoyController>() ?? other.GetComponentInParent<BoyController>();
            if (boy != null) boy.SetCrouching(false);
            onExitShelter?.Invoke();
        }
    }

    private bool IsPlayer(Collider col)
    {
        return col.GetComponent<BoyController>() != null ||
               col.CompareTag("Player") ||
               col.name.ToLower().Contains("boy");
    }

    private void OnGUI()
    {
        if (!showDebugGUI || !IsPlayerInside) return;
        GUIStyle style = new GUIStyle(GUI.skin.box);
        style.fontSize = 24;
        style.fontStyle = FontStyle.Bold;
        style.normal.textColor = Color.green;
        style.alignment = TextAnchor.MiddleCenter;
        float width = 450f, height = 60f;
        float x = (Screen.width - width) * 0.5f, y = 20f;
        GUI.Box(new Rect(x, y, width, height), $"BERHASIL BERLINDUNG!\n({zoneName})", style);
    }

    private void OnDrawGizmos()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.color = IsPlayerInside ? new Color(0f, 1f, 0f, 0.45f) : new Color(0.2f, 0.8f, 0.2f, 0.2f);
            Gizmos.matrix = transform.localToWorldMatrix;
            if (col is BoxCollider box)
            {
                Gizmos.DrawCube(box.center, box.size);
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(box.center, box.size);
            }
        }
    }
}
```

### CheckpointTrigger.cs
```csharp
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
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        BoyController boy = GetBoyController(other);
        if (boy != null) { currentPlayer = boy; isPlayerInside = true; }
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
                GameManager.Instance.missionUI.ResetHoldProgress();
        }
    }

    private void Update()
    {
        if (isCompleted || !isPlayerInside || GameManager.Instance == null) return;
        if (GameManager.Instance.currentLevel != targetLevel || !GameManager.Instance.isGameActive) return;

        if (requireCrouch && currentPlayer != null && !currentPlayer.IsCrouching)
        {
            currentHoldTimer = 0f;
            if (GameManager.Instance.missionUI != null) GameManager.Instance.missionUI.ResetHoldProgress();
            return;
        }

        if (requiredHoldTime > 0f)
        {
            currentHoldTimer += Time.deltaTime;
            if (GameManager.Instance.missionUI != null)
                GameManager.Instance.missionUI.UpdateHoldProgress(currentHoldTimer, requiredHoldTime);
            if (currentHoldTimer >= requiredHoldTime) TriggerComplete();
        }
        else
        {
            TriggerComplete();
        }
    }

    private void TriggerComplete()
    {
        isCompleted = true;
        currentHoldTimer = 0f;
        if (GameManager.Instance.missionUI != null) GameManager.Instance.missionUI.ResetHoldProgress();
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
            if (col is BoxCollider box) Gizmos.DrawCube(box.center, box.size);
            else if (col is SphereCollider sphere) Gizmos.DrawSphere(sphere.center, sphere.radius);
        }
    }
}
```

### MissionUI.cs
```csharp
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Menangani tampilan HUD misi berjalan, popup level complete, dan popup skor akhir.
/// </summary>
public class MissionUI : MonoBehaviour
{
    [Header("HUD Elemen")]
    public Text instructionText;
    public Text timerText;
    public Slider holdProgressSlider;
    public GameObject holdProgressContainer;

    [Header("Panel Level Complete")]
    public GameObject levelCompletePanel;
    public Text levelCompleteTitleText;
    public Text levelCompleteDescText;
    public Button nextLevelButton;

    [Header("Panel Skor Akhir")]
    public GameObject finalScorePanel;
    public Text scoreTimeText;
    public Text scoreStarsText;
    public Text scorePointsText;
    public Text scoreEvaluationText;
    public Button retryButton;
    public Button mainMenuButton;

    private void Awake()
    {
        if (levelCompletePanel != null) levelCompletePanel.SetActive(false);
        if (finalScorePanel != null) finalScorePanel.SetActive(false);
        if (holdProgressContainer != null) holdProgressContainer.SetActive(false);

        if (nextLevelButton != null)
            nextLevelButton.onClick.AddListener(() => { if (GameManager.Instance != null) GameManager.Instance.NextLevel(); });
        if (retryButton != null)
            retryButton.onClick.AddListener(() => { if (GameManager.Instance != null) GameManager.Instance.RestartGame(); });
        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(() => { if (GameManager.Instance != null) GameManager.Instance.GoToMainMenu(); });
    }

    public void SetInstruction(string text)
    {
        if (instructionText != null) instructionText.text = text;
    }

    public void UpdateTimerDisplay(float seconds)
    {
        if (timerText != null)
        {
            int mins = Mathf.FloorToInt(seconds / 60f);
            int secs = Mathf.FloorToInt(seconds % 60f);
            timerText.text = $"{mins:00}:{secs:00}";
        }
    }

    public void UpdateHoldProgress(float current, float max)
    {
        if (holdProgressContainer != null)
        {
            bool show = current > 0f && current < max;
            holdProgressContainer.SetActive(show);
        }
        if (holdProgressSlider != null && max > 0f)
            holdProgressSlider.value = Mathf.Clamp01(current / max);
    }

    public void ResetHoldProgress()
    {
        if (holdProgressContainer != null) holdProgressContainer.SetActive(false);
        if (holdProgressSlider != null) holdProgressSlider.value = 0f;
    }

    public void ShowLevelComplete(string title, string description)
    {
        if (levelCompletePanel != null) levelCompletePanel.SetActive(true);
        if (levelCompleteTitleText != null) levelCompleteTitleText.text = title;
        if (levelCompleteDescText != null) levelCompleteDescText.text = description;
    }

    public void HideLevelComplete()
    {
        if (levelCompletePanel != null) levelCompletePanel.SetActive(false);
    }

    public void ShowFinalScore(float totalTime, int stars, string evaluation, int score = 0)
    {
        if (finalScorePanel != null) finalScorePanel.SetActive(true);
        if (scoreTimeText != null)
        {
            int mins = Mathf.FloorToInt(totalTime / 60f);
            int secs = Mathf.FloorToInt(totalTime % 60f);
            scoreTimeText.text = $"Total Waktu: {mins:00}:{secs:00}";
        }
        if (scorePointsText != null) scorePointsText.text = $"Total Poin: {score} Poin";
        if (scoreStarsText != null)
        {
            string starStr = "";
            for (int i = 0; i < stars; i++) starStr += "⭐ ";
            for (int i = stars; i < 3; i++) starStr += "☆ ";
            scoreStarsText.text = starStr.Trim();
        }
        if (scoreEvaluationText != null) scoreEvaluationText.text = evaluation;
    }
}
```

### VirtualJoystick.cs
```csharp
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Virtual Joystick UI untuk kontrol pergerakan karakter di layar sentuh Android atau mouse drag di Editor.
/// </summary>
public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("UI References")]
    [SerializeField] private RectTransform background;
    [SerializeField] private RectTransform handle;

    [Header("Settings")]
    [SerializeField, Min(10f)] private float handleRange = 75f;
    [SerializeField, Range(0f, 0.5f)] private float deadZone = 0.05f;

    public Vector2 InputDirection { get; private set; } = Vector2.zero;

    private Canvas canvas;
    private Camera canvasCamera;

    private void Awake()
    {
        Screen.orientation = ScreenOrientation.LandscapeLeft;
        canvas = GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            canvasCamera = canvas.worldCamera;
        if (background == null) background = GetComponent<RectTransform>();
        if (handle == null && transform.childCount > 0)
            handle = transform.GetChild(0).GetComponent<RectTransform>();
    }

    public void OnPointerDown(PointerEventData eventData) => OnDrag(eventData);

    public void OnDrag(PointerEventData eventData)
    {
        if (background == null || handle == null) return;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(background, eventData.position, canvasCamera, out Vector2 localPoint))
        {
            Vector2 offset = localPoint;
            if (offset.magnitude > handleRange) offset = offset.normalized * handleRange;
            handle.anchoredPosition = offset;
            Vector2 rawInput = offset / handleRange;
            InputDirection = rawInput.magnitude < deadZone ? Vector2.zero : rawInput;
        }
    }

    public void OnPointerUp(PointerEventData eventData) => ResetJoystick();
    private void OnDisable() => ResetJoystick();

    private void ResetJoystick()
    {
        InputDirection = Vector2.zero;
        if (handle != null) handle.anchoredPosition = Vector2.zero;
    }
}
```

### CrouchButton.cs
```csharp
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Tombol UI di pojok kanan bawah layar untuk mengontrol aksi merunduk / menunduk (Crouch).
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
        if (targetBoy == null) targetBoy = FindFirstObjectByType<BoyController>();
        if (buttonImage == null) buttonImage = GetComponent<Image>();
    }

    public void OnPointerDown(PointerEventData eventData) => OnClickButton();

    public void OnClickButton()
    {
        if (Time.unscaledTime - lastClickTime < ClickCooldown) return;
        lastClickTime = Time.unscaledTime;
        if (targetBoy == null) targetBoy = FindFirstObjectByType<BoyController>();
        if (targetBoy != null) targetBoy.ToggleCrouch();
    }

    private void Update()
    {
        if (targetBoy != null && buttonImage != null)
        {
            Color targetColor = targetBoy.IsCrouching ? activeCrouchColor : normalColor;
            buttonImage.color = Color.Lerp(buttonImage.color, targetColor, Time.deltaTime * 12f);
        }
    }
}
```

---

## Cara Pakai File Ini

1. **Buka Cursor IDE** (atau AI chat manapun)
2. **Copy-paste seluruh isi file ini** sebagai pesan pertama ke AI
3. Lalu minta fitur spesifik, contoh:
   - "Buatkan EarthquakeEffect.cs untuk camera shake dan screen overlay saat Level 2 dimulai"
   - "Buatkan EvacuationWaypoint.cs untuk Level 3 — pemain mengikuti waypoint ke titik kumpul"
   - "Perbaiki ShelterZone agar ada hold timer 5 detik sambil crouch"

**Aturan saat minta script baru:**
- Ikuti pattern singleton GameManager dan null-check yang sudah ada
- Pakai `UnityEngine` dan `UnityEngine.UI` saja, jangan tambah package baru
- Tambahkan `[Header]` dan `[Tooltip]` untuk Inspector
- Komentar bahasa Indonesia
- Jelaskan cara setup di Unity Editor (apa yang perlu di-assign di Inspector)
