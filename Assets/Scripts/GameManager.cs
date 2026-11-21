using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Mengatur alur simulasi evakuasi gempa (Level 1 Kuis TV, Level 2 Drop & Cover, Level 3 Evakuasi ke Lapangan).
/// Dilengkapi sistem stopwatch waktu, poin gamifikasi, dan rating bintang akhir.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Status Level")]
    [Tooltip("Level yang sedang aktif (1 = Kuis TV, 2 = Lindungi Diri di Meja, 3 = Evakuasi ke Lapangan)")]
    public int currentLevel = 1;
    [Tooltip("Pilih level awal saat Play di Editor untuk pengujian cepat (1 = Normal Kuis, 3 = Langsung Evakuasi Jalur Hijau & Bintang)")]
    [Range(1, 3)]
    public int startLevelOnPlay = 1;
    public int maxLevel = 3;
    public bool isGameActive = false;
    [Tooltip("Apakah pemain sudah mengambil tas pelindung kepala di Level 3?")]
    public bool hasPickedUpBag = false;

    [Header("Poin Gamifikasi")]
    public int currentScore = 0;

    [Header("Instruksi Setiap Level")]
    public string[] levelInstructions = new string[]
    {
        "Level 1: Perhatikan layar TV dan jawab 2 kuis tanda bencana gempa bumi!",
        "Level 2: Terjadi Gempa! Segera cari kolong meja dan RUNDUK untuk berlindung!",
        "Level 3: Gempa mereda! Segera cari dan ambil tas untuk lindungi kepalamu!"
    };
    [Tooltip("Instruksi lanjutan Level 3 setelah tas berhasil diambil")]
    public string level3EvacuateInstruction = "Level 3: Bagus! Sekarang ikuti jalur evakuasi menuju Titik Kumpul di lapangan terbuka!";

    [Header("Alur Mulai via Checkpoint")]
    [Tooltip("Jika dicentang, simulasi menunggu pemain melangkah ke zona TV terlebih dahulu.")]
    public bool waitForStartTrigger = true;
    public string waitingInstruction = "Level 1: Ayo berjalan ke lingkaran zona TV untuk mulai belajar tanda gempa bumi!";
    [Tooltip("Aktifkan kuis edukasi 2 soal saat pemain mendekati TV.")]
    public bool enableLevel1Quiz = true;

    [Header("Catatan Waktu & Penilaian")]
    public float totalElapsedTime = 0f;
    public float goldTimeTarget = 60f;
    public float silverTimeTarget = 120f;

    [Header("Pengaturan Gempa (Level 2)")]
    [Tooltip("Intensitas gempa Level 2 (meter). Otomatis tersinkron dengan GameObject 'Efek_Gempa' (EarthquakeEffect) sebagai pusat pengaturan.")]
    [Range(0.005f, 0.25f)] public float earthquakeShakeStrength = 0.04f;

    [Tooltip("Kemiringan kamera saat gempa berguncang (derajat). Geser ke 0 jika tidak ingin kamera miring.")]
    [Range(0f, 3f)] public float earthquakeRollAngle = 0.6f;

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

        if (SoundManager.Instance == null)
        {
            GameObject smObj = new GameObject("SoundManager", typeof(SoundManager));
        }

        if (playerBoy == null)
        {
            playerBoy = FindFirstObjectByType<BoyController>();
        }

        enableLevel1Quiz = true;
        waitForStartTrigger = true;

        if (quizManager == null)
        {
            quizManager = FindFirstObjectByType<Level1QuizManager>(FindObjectsInactive.Include);
        }

        // Pastikan panel kuis tersembunyi sampai pemain menginjak zona TV
        if (quizManager != null)
        {
            if (quizManager.quizPanel != null)
            {
                quizManager.quizPanel.SetActive(false);
            }
        }

        GameObject quizObj = GameObject.Find("Panel_Level1_Quiz");
        if (quizObj != null)
        {
            quizObj.SetActive(false);
        }

        // Pastikan objek diorama 3D legacy dibersihkan jika masih ada
        GameObject diorama = GameObject.Find("Diorama_Soal1_Rumah");
        if (diorama != null)
        {
            Destroy(diorama);
        }

        // Posisikan trigger lingkaran tepat di depan TV (Smart_TV_Level1)
        GameObject tv = GameObject.Find("Smart_TV_Level1");
        if (tv == null) tv = GameObject.Find("denah_v2/Smart_TV_Level1");
        if (tv == null) tv = GameObject.Find("denah_tes/Smart_TV_Level1");
        if (tv == null)
        {
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (t.name == "Smart_TV_Level1")
                {
                    tv = t.gameObject;
                    break;
                }
            }
        }
        if (tv != null)
        {
            Vector3 tvPos = tv.transform.position;
            GameObject triggerTV = GameObject.Find("Trigger_Mulai_Simulasi");
            if (triggerTV != null)
            {
                triggerTV.transform.position = new Vector3(tvPos.x, 0.14f, tvPos.z + 0.9f);
                BoxCollider bc = triggerTV.GetComponent<BoxCollider>();
                if (bc != null)
                {
                    bc.size = new Vector3(3f, 2.5f, 3f);
                }
            }

            // Hapus collider pada TV fisik jika ada (agar tidak membengkak karena scale model)
            // Trigger yang digunakan murni lingkaran lantai Trigger_Mulai_Simulasi
            BoxCollider tvCol = tv.GetComponent<BoxCollider>();
            if (tvCol != null) Destroy(tvCol);
            CheckpointTrigger tvCp = tv.GetComponent<CheckpointTrigger>();
            if (tvCp != null) Destroy(tvCp);
        }

        // Nonaktifkan otomatis Meja 1 dan Meja 3 beserta kursinya, sisakan HANYA Meja 2
        string[] desksToHide = new string[]
        {
            "Meja_Belajar_1", "Meja_Belajar_3",
            "Kursi_1_1", "Kursi_1_2", "Kursi_1_3", "Kursi_1_4",
            "Kursi_3_1", "Kursi_3_2", "Kursi_3_3", "Kursi_3_4"
        };
        foreach (string deskName in desksToHide)
        {
            GameObject deskObj = GameObject.Find(deskName);
            if (deskObj != null)
            {
                deskObj.SetActive(false);
            }
        }
    }

    private void Start()
    {
        if (startLevelOnPlay == 3)
        {
            JumpToLevel3ForTesting();
            return;
        }

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

    [ContextMenu("Test: Lompat ke Level 3")]
    public void JumpToLevel3ForTesting()
    {
        currentLevel = 3;
        isGameActive = true;
        hasPickedUpBag = true;
        SetPlayerMovementEnabled(true);

        if (missionUI != null)
        {
            missionUI.SetInstruction(level3EvacuateInstruction);
        }

        if (playerBoy != null)
        {
            var cc = playerBoy.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            playerBoy.transform.position = new Vector3(0.40f, 0.15f, -9.95f);
            playerBoy.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            if (cc != null) cc.enabled = true;
        }

        ThirdPersonCamera cam = FindFirstObjectByType<ThirdPersonCamera>();
        if (cam != null && playerBoy != null)
        {
            cam.transform.position = playerBoy.transform.position + Vector3.up * 2.0f + Vector3.forward * 2.8f;
            cam.transform.LookAt(playerBoy.transform.position + Vector3.up * 0.9f);
        }

        EvacuationGuide guide = FindFirstObjectByType<EvacuationGuide>();
        if (guide != null)
        {
            guide.AutoFindStarsIfEmpty();
        }

        Debug.Log("[GameManager] Melompat langsung ke Level 3 Evakuasi!");
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
    /// Memulai alur simulasi saat pemain melangkah ke zona TV di depan kelas.
    /// </summary>
    public void StartSimulation()
    {
        totalElapsedTime = 0f;
        currentScore = 0;
        hasPickedUpBag = false;
        isGameActive = true;

        if (quizManager == null)
        {
            quizManager = FindFirstObjectByType<Level1QuizManager>(FindObjectsInactive.Include);
        }

        if (enableLevel1Quiz && quizManager != null)
        {
            currentLevel = 1;
            SetPlayerMovementEnabled(false);
            quizManager.StartQuiz();
        }
        else
        {
            currentLevel = 2; // Langsung ke Level 2 (Gempa) jika opsi kuis dimatikan
            SetPlayerMovementEnabled(true);
            if (SoundManager.Instance != null) SoundManager.Instance.StartEarthquake();
        }

        UpdateMissionStatus();
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
    /// Dipanggil otomatis oleh BagPickup saat pemain mengambil tas pelindung kepala pada Level 3.
    /// </summary>
    public void OnBagPickedUp()
    {
        if (hasPickedUpBag) return;

        hasPickedUpBag = true;
        AddScore(15);
        Debug.Log("[GameManager] Pemain berhasil mengambil tas pelindung kepala!");

        if (missionUI != null)
        {
            missionUI.SetInstruction(level3EvacuateInstruction);
        }
    }

    /// <summary>
    /// Dipanggil otomatis oleh Level1QuizManager saat 2 soal selesai dijawab.
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

        if (levelIndex == 2)
        {
            AddScore(35);
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.StopEarthquake();
            }
        }

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

        if (currentLevel == 2 && SoundManager.Instance != null)
        {
            SoundManager.Instance.StartEarthquake();
        }

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
        AddScore(20); // Bonus Poin Evakuasi Lapangan Selamat!

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

    public void SetPlayerMovementEnabled(bool enabled)
    {
        if (playerBoy != null)
        {
            if (!enabled)
            {
                playerBoy.StopMovement();
            }
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
                return "Pintar sekali! Kamu sudah memahami tanda visual animasi dan suara gempa bumi!";
            case 2:
                return "Hebat! Kamu berhasil berlindung di kolong meja sampai gempa reda. Sekarang bersiap untuk evakuasi!";
            default:
                return "Tahap ini berhasil diselesaikan!";
        }
    }

    public void RestartGame()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.StopEarthquake();
        }
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }

    public void GoToMainMenu()
    {
        if (Application.CanStreamedLevelBeLoaded("MainMenu"))
        {
            SceneManager.LoadScene("MainMenu");
        }
        else
        {
            RestartGame();
        }
    }
}
