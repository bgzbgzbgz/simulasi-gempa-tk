using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Menangani tampilan HUD misi berjalan, popup level complete, popup skor akhir,
/// modal pengaturan pause, dan animasi avatar Bu Guru saat voice over.
/// Mengadopsi style kartun Upin & Ipin / TK Playful ramah anak.
/// </summary>
public class MissionUI : MonoBehaviour
{
    [Header("HUD Elemen")]
    public Text instructionText;
    public Text timerText;
    public Slider holdProgressSlider;
    public GameObject holdProgressContainer;

    [Header("HUD Bu Guru (Voice Over)")]
    public RectTransform buGuruHUD;
    public Image buGuruAvatarImg;
    public Text buGuruSpeechText;
    private Coroutine buGuruSlideRoutine;

    [Header("Panel Pause / Settings")]
    public GameObject pausePanel;
    public Button pauseButton;
    public Button resumeButton;
    public Button pauseRetryButton;
    public Button pauseMainMenuButton;
    public Slider pauseBgmSlider;
    public Slider pauseSfxSlider;
    public Toggle pauseVoiceOverToggle;
    public Slider pauseCameraDistanceSlider;
    public Text pauseCameraDistanceValueText;

    [Header("Panel Level Complete")]
    public GameObject levelCompletePanel;
    public Text levelCompleteTitleText;
    public Text levelCompleteDescText;
    public Button nextLevelButton;

    [Header("Panel Skor Akhir (Rapor Pahlawan Cilik)")]
    public GameObject finalScorePanel;
    public Text scoreTimeText;
    public Text scoreStarsText;
    public Text scorePointsText;
    public Text scoreEvaluationText;
    public Text scorePillar1Text;
    public Text scorePillar2Text;
    public Text scorePillar3Text;
    public Button retryButton;
    public Button mainMenuButton;

    private void Awake()
    {
        if (levelCompletePanel != null) levelCompletePanel.SetActive(false);
        if (finalScorePanel != null) finalScorePanel.SetActive(false);
        if (holdProgressContainer != null) holdProgressContainer.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);

        // Wiring Tombol Level Complete
        if (nextLevelButton != null)
        {
            nextLevelButton.onClick.AddListener(() =>
            {
                if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
                if (GameManager.Instance != null) GameManager.Instance.NextLevel();
            });
        }

        // Wiring Tombol Skor Akhir
        if (retryButton != null)
        {
            retryButton.onClick.AddListener(() =>
            {
                if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
                if (GameManager.Instance != null) GameManager.Instance.RestartGame();
            });
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(() =>
            {
                if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
                Time.timeScale = 1f;
                if (GameManager.Instance != null) GameManager.Instance.GoToMainMenu();
            });
        }

        // Wiring Tombol Pause & Menu Pause
        if (pauseButton == null)
        {
            Transform tBtn = transform.Find("HUD_Top/Btn_Pause") ?? transform.parent?.Find("HUD_Top/Btn_Pause");
            if (tBtn == null)
            {
                GameObject g = GameObject.Find("Btn_Pause");
                if (g != null) tBtn = g.transform;
            }
            if (tBtn != null) pauseButton = tBtn.GetComponent<Button>();
        }

        if (pauseButton != null)
        {
            pauseButton.onClick.RemoveListener(OpenPauseMenu);
            pauseButton.onClick.AddListener(OpenPauseMenu);
        }

        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveListener(ClosePauseMenu);
            resumeButton.onClick.AddListener(ClosePauseMenu);
        }

        if (pauseRetryButton != null)
        {
            pauseRetryButton.onClick.AddListener(() =>
            {
                if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
                Time.timeScale = 1f;
                if (GameManager.Instance != null) GameManager.Instance.RestartGame();
            });
        }

        if (pauseMainMenuButton != null)
        {
            pauseMainMenuButton.onClick.AddListener(() =>
            {
                if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
                Time.timeScale = 1f;
                if (GameManager.Instance != null) GameManager.Instance.GoToMainMenu();
            });
        }

        // Wiring Slider Kamera di Menu Pause
        if (pauseCameraDistanceSlider != null)
        {
            ThirdPersonCamera tpc = ThirdPersonCamera.Instance ?? Camera.main?.GetComponent<ThirdPersonCamera>();
            if (tpc != null)
            {
                pauseCameraDistanceSlider.minValue = 1.2f;
                pauseCameraDistanceSlider.maxValue = 5.0f;
                pauseCameraDistanceSlider.value = tpc.distance;
                if (pauseCameraDistanceValueText != null)
                {
                    pauseCameraDistanceValueText.text = $"{tpc.distance:F1}m";
                }
            }

            pauseCameraDistanceSlider.onValueChanged.RemoveListener(OnCameraDistanceSliderChanged);
            pauseCameraDistanceSlider.onValueChanged.AddListener(OnCameraDistanceSliderChanged);
        }
    }

    private void Update()
    {
        // Akses keyboard Escape atau P untuk membuka/tutup menu pengaturan
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
        {
            TogglePauseMenu();
        }
    }

    #region Pause System

    public void TogglePauseMenu()
    {
        if (pausePanel != null)
        {
            if (pausePanel.activeSelf) ClosePauseMenu();
            else OpenPauseMenu();
        }
    }

    public void OpenPauseMenu()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
        if (pauseCameraDistanceSlider != null)
        {
            ThirdPersonCamera tpc = ThirdPersonCamera.Instance ?? Camera.main?.GetComponent<ThirdPersonCamera>();
            if (tpc != null)
            {
                pauseCameraDistanceSlider.value = tpc.distance;
                if (pauseCameraDistanceValueText != null)
                {
                    pauseCameraDistanceValueText.text = $"{tpc.distance:F1}m";
                }
            }
        }
        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
            Time.timeScale = 0f; // Jeda gameplay
        }
    }

    public void ClosePauseMenu()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
            Time.timeScale = 1f; // Lanjutkan gameplay
        }
    }

    public void OnCameraDistanceSliderChanged(float val)
    {
        ThirdPersonCamera tpc = ThirdPersonCamera.Instance ?? Camera.main?.GetComponent<ThirdPersonCamera>();
        if (tpc != null)
        {
            tpc.SetCameraDistance(val);
        }
        if (pauseCameraDistanceValueText != null)
        {
            pauseCameraDistanceValueText.text = $"{val:F1}m";
        }
    }

    #endregion

    #region HUD Updates

    public void SetInstruction(string text)
    {
        if (instructionText != null)
        {
            instructionText.text = text;
        }
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
        {
            holdProgressSlider.value = Mathf.Clamp01(current / max);
        }
    }

    public void ResetHoldProgress()
    {
        if (holdProgressContainer != null)
        {
            holdProgressContainer.SetActive(false);
        }
        if (holdProgressSlider != null)
        {
            holdProgressSlider.value = 0f;
        }
    }

    #endregion

    #region Bu Guru Voice Over & Slide-in Animation

    /// <summary>
    /// Memunculkan avatar Bu Guru dengan transisi slide-in dari arah kiri ke kanan saat Voice Over aktif.
    /// </summary>
    public void ShowBuGuruVoice(string speechMessage = "", float displayDuration = 4.5f)
    {
        if (buGuruHUD == null) return;

        if (buGuruSpeechText != null && !string.IsNullOrEmpty(speechMessage))
        {
            buGuruSpeechText.text = speechMessage;
        }

        if (buGuruSlideRoutine != null)
        {
            StopCoroutine(buGuruSlideRoutine);
        }
        buGuruSlideRoutine = StartCoroutine(SlideBuGuruRoutine(true, displayDuration));
    }

    public void HideBuGuruVoice()
    {
        if (buGuruHUD == null) return;
        if (buGuruSlideRoutine != null) StopCoroutine(buGuruSlideRoutine);
        buGuruSlideRoutine = StartCoroutine(SlideBuGuruRoutine(false, 0f));
    }

    private IEnumerator SlideBuGuruRoutine(bool slideIn, float autoHideDelay)
    {
        float duration = 0.35f;
        float elapsed = 0f;

        Vector2 startPos = buGuruHUD.anchoredPosition;
        // Posisi off-screen sebelah kiri (x: -250), posisi aktif (x: 20)
        Vector2 targetPos = slideIn ? new Vector2(20f, startPos.y) : new Vector2(-260f, startPos.y);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            // Easing Out Back untuk efek membal/bouncy kartun
            float ease = 1f + 2.70158f * Mathf.Pow(t - 1f, 3f) + 1.70158f * Mathf.Pow(t - 1f, 2f);
            if (!slideIn) ease = Mathf.SmoothStep(0f, 1f, t);

            buGuruHUD.anchoredPosition = Vector2.LerpUnclamped(startPos, targetPos, ease);
            yield return null;
        }
        buGuruHUD.anchoredPosition = targetPos;

        if (slideIn && autoHideDelay > 0f)
        {
            yield return new WaitForSeconds(autoHideDelay);
            yield return SlideBuGuruRoutine(false, 0f);
        }
    }

    #endregion

    #region Level Complete & Final Score

    public void ShowLevelComplete(string title, string description)
    {
        if (levelCompletePanel != null)
        {
            levelCompletePanel.SetActive(true);
        }

        string playerName = PlayerPrefs.GetString("PlayerName", "Ananda");
        if (levelCompleteTitleText != null)
        {
            levelCompleteTitleText.text = $"HEBAT KAMU, {playerName.ToUpper()}!";
        }

        if (levelCompleteDescText != null)
        {
            levelCompleteDescText.text = description;
        }

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayLevelComplete();
        }
    }

    public void HideLevelComplete()
    {
        if (levelCompletePanel != null)
        {
            levelCompletePanel.SetActive(false);
        }
    }

    public void ShowFinalScore(float totalTime, int stars, string evaluation, int score = 0)
    {
        if (finalScorePanel != null)
        {
            finalScorePanel.SetActive(true);
        }

        string playerName = PlayerPrefs.GetString("PlayerName", "Ananda");

        if (scoreTimeText != null)
        {
            int mins = Mathf.FloorToInt(totalTime / 60f);
            int secs = Mathf.FloorToInt(totalTime % 60f);
            scoreTimeText.text = $"⏱️ Waktu Selesai: {mins:00}:{secs:00} • Sangat Tanggap & Mandiri";
        }

        if (scorePointsText != null)
        {
            string grade = score >= 100 ? "SEMPURNA!" : (score >= 80 ? "SANGAT BAIK!" : "LULUS");
            scorePointsText.text = $"🏆 TOTAL SKOR KELULUSAN: {score} / 100 ({grade})";
        }

        if (scoreStarsText != null)
        {
            string starStr = "";
            for (int i = 0; i < stars; i++) starStr += "★ ";
            for (int i = stars; i < 3; i++) starStr += "☆ ";
            scoreStarsText.text = starStr.Trim();
        }

        if (scoreEvaluationText != null)
        {
            scoreEvaluationText.text = $"Catatan Bu Guru: \"Ananda {playerName} sangat pintar dan mandiri dalam mempraktikkan mitigasi bencana gempa bumi!\"";
        }

        if (scorePillar1Text != null) scorePillar1Text.text = "30 / 30 Poin";
        if (scorePillar2Text != null) scorePillar2Text.text = "35 / 35 Poin";
        if (scorePillar3Text != null) scorePillar3Text.text = "35 / 35 Poin";

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayGameWin();
        }
    }

    #endregion
}
