using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Mengatur alur level/misi evakuasi (Level 1, 2, 3), stopwatch waktu, dan perhitungan skor akhir.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Status Level")]
    [Tooltip("Level yang sedang aktif (1 = Kolong Meja, 2 = Koridor & Gang, 3 = Titik Kumpul)")]
    public int currentLevel = 1;
    public int maxLevel = 3;
    public bool isGameActive = false;

    [Header("Instruksi Setiap Level")]
    public string[] levelInstructions = new string[]
    {
        "Level 1: Terjadi Gempa! Segera cari kolong meja dan RUNDUK untuk berlindung!",
        "Level 2: Gempa mereda! Evakuasi keluar ruangan melewati koridor dan jalan gang!",
        "Level 3: Menuju Zona Hijau (Titik Kumpul Evakuasi) di tengah lapangan!"
    };

    [Header("Catatan Waktu & Penilaian")]
    public float totalElapsedTime = 0f;

    [Header("Target Waktu Penilaian (Detik)")]
    [Tooltip("Selesai di bawah waktu ini dapat Bintang 3")]
    public float goldTimeTarget = 60f;
    [Tooltip("Selesai di bawah waktu ini dapat Bintang 2")]
    public float silverTimeTarget = 120f;

    [Header("UI Reference")]
    public MissionUI missionUI;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
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
    /// Memulai alur simulasi evakuasi dari Level 1.
    /// </summary>
    public void StartGame()
    {
        currentLevel = 1;
        totalElapsedTime = 0f;
        isGameActive = true;

        UpdateMissionStatus();
    }

    /// <summary>
    /// Dipanggil saat pemain berhasil menyelesaikan objektif/checkpoint pada level tertentu.
    /// </summary>
    public void CompleteLevel(int levelIndex)
    {
        // Hanya proses jika level yang diselesaikan sesuai dengan level aktif saat ini
        if (levelIndex != currentLevel || !isGameActive)
        {
            return;
        }

        Debug.Log($"[GameManager] Level {levelIndex} Selesai!");

        if (currentLevel >= maxLevel)
        {
            // Selesai seluruh simulasi -> Tampilkan Skor Akhir
            FinishGame();
        }
        else
        {
            // Pause sementara permainan & tampilkan popup Level Selesai
            isGameActive = false;
            if (missionUI != null)
            {
                string title = $"LEVEL {currentLevel} SELESAI!";
                string desc = GetCompletionMessage(currentLevel);
                missionUI.ShowLevelComplete(title, desc);
            }
        }
    }

    /// <summary>
    /// Lanjut ke level/fase berikutnya (dipanggil dari tombol Lanjut pada UI).
    /// </summary>
    public void NextLevel()
    {
        currentLevel++;
        isGameActive = true;

        if (missionUI != null)
        {
            missionUI.HideLevelComplete();
        }

        UpdateMissionStatus();
    }

    /// <summary>
    /// Menyelesaikan seluruh simulasi dan menghitung perolehan bintang/skor.
    /// </summary>
    private void FinishGame()
    {
        isGameActive = false;

        int stars = 1;
        string evalText = "Cukup Baik! Tetap waspada dan percepat evakuasi.";

        if (totalElapsedTime <= goldTimeTarget)
        {
            stars = 3;
            evalText = "Luar Biasa! Respon evakuasi sangat cepat dan tepat!";
        }
        else if (totalElapsedTime <= silverTimeTarget)
        {
            stars = 2;
            evalText = "Bagus! Kamu berhasil evakuasi dengan selamat.";
        }

        if (missionUI != null)
        {
            missionUI.ShowFinalScore(totalElapsedTime, stars, evalText);
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
                return "Hebat! Kamu berhasil melindungi kepala dan tubuh di kolong meja selama gempa.";
            case 2:
                return "Bagus sekali! Kamu berhasil melewati lorong dan jalan gang dengan aman.";
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
        // shortcut: Muat scene index 0 atau MainMenu jika belum dibuat di Build Settings
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
