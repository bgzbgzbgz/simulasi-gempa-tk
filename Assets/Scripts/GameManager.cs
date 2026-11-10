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
        "Level 1: Masuk ke lingkaran zona mulai di depan TV untuk memulai simulasi gempa!",
        "Level 2: Terjadi Gempa! Segera cari kolong meja dan RUNDUK untuk berlindung!",
        "Level 3: Gempa mereda! Ikuti jalur evakuasi menuju Titik Kumpul di lapangan terbuka!"
    };

    [Header("Alur Mulai via Checkpoint")]
    [Tooltip("Jika dicentang, simulasi menunggu pemain melangkah ke zona checkpoint mulai terlebih dahulu.")]
    public bool waitForStartTrigger = true;
    public string waitingInstruction = "Silakan bersiap di kelas. Masuk ke lingkaran zona mulai di depan TV untuk memicu simulasi gempa!";
    [Tooltip("Aktifkan HANYA jika ingin menyertakan kuis popup (default mati agar imersif).")]
    public bool enableLevel1Quiz = false;

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

        if (quizManager == null)
        {
            quizManager = FindFirstObjectByType<Level1QuizManager>(FindObjectsInactive.Include);
        }

        // Pastikan panel kuis selalu tersembunyi saat game dimulai
        if (quizManager != null)
        {
            if (quizManager.quizPanel != null)
            {
                quizManager.quizPanel.SetActive(false);
            }
            if (!enableLevel1Quiz)
            {
                quizManager.gameObject.SetActive(false);
            }
        }

        GameObject quizObj = GameObject.Find("Panel_Level1_Quiz");
        if (quizObj != null && !enableLevel1Quiz)
        {
            quizObj.SetActive(false);
        }
    }

    private void Start()
    {
        if (waitForStartTrigger)
        {
            isGameActive = false;
            currentLevel = 0;
            totalElapsedTime = 0f;

            if (missionUI != null)
            {
                missionUI.SetInstruction(waitingInstruction);
                missionUI.UpdateTimerDisplay(0f);
            }
            SetPlayerMovementEnabled(true);
        }
        else
        {
            StartSimulation();
        }
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
    /// Memulai alur simulasi evakuasi (dipanggil saat pemain menginjak Checkpoint Mulai).
    /// </summary>
    public void StartSimulation()
    {
        currentLevel = 2; // Langsung ke Level 2 (Gempa) karena trigger TV menggantikan kuis Level 1
        totalElapsedTime = 0f;
        currentScore = 10;
        isGameActive = true;
        SetPlayerMovementEnabled(true);

        UpdateMissionStatus();

        // Mulai kuis HANYA jika opsi enableLevel1Quiz diaktifkan secara eksplisit
        if (enableLevel1Quiz && quizManager != null)
        {
            currentLevel = 1;
            SetPlayerMovementEnabled(false);
            quizManager.StartQuiz();
        }
    }

    public void StartGame()
    {
        StartSimulation();
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
                return "Pintar sekali! Kamu berhasil melindungi kepala dan berlindung di kolong meja!";
            case 2:
                return "Hebat! Kamu berhasil mengevakuasi diri keluar melewati lorong dan jalan gang!";
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
