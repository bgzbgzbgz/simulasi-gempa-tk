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
