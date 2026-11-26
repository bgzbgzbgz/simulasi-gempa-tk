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
    public Text holdProgressText;

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
    public Image pauseVoiceOverBg;
    public Text pauseVoiceOverText;
    public GameObject pauseVoiceOverCheckIcon;
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
    public Image[] finalScoreStarImages = new Image[3];
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

        if (holdProgressText == null && holdProgressContainer != null)
        {
            holdProgressText = holdProgressContainer.transform.Find("HoldLabel")?.GetComponent<Text>();
        }

        if (buGuruHUD != null && buGuruSpeechText == null)
        {
            buGuruSpeechText = (buGuruHUD.Find("SpeechText") ?? buGuruHUD.Find("StatusTxt"))?.GetComponent<Text>();
        }

        if (finalScorePanel != null && (finalScoreStarImages == null || finalScoreStarImages.Length != 3 || finalScoreStarImages[0] == null))
        {
            finalScoreStarImages = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                Transform tStar = finalScorePanel.transform.Find($"Card/StarsRow/Star_{i}");
                if (tStar != null) finalScoreStarImages[i] = tStar.GetComponent<Image>();
            }
        }

        // Wiring Tombol Level Complete
        if (nextLevelButton != null)
        {
            nextLevelButton.onClick.RemoveAllListeners();
            nextLevelButton.onClick.AddListener(() =>
            {
                if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
                if (GameManager.Instance != null) GameManager.Instance.NextLevel();
            });
        }

        // Wiring Tombol Skor Akhir
        if (retryButton != null)
        {
            retryButton.onClick.RemoveAllListeners();
            retryButton.onClick.AddListener(() =>
            {
                if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
                if (GameManager.Instance != null) GameManager.Instance.RestartGame();
            });
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveAllListeners();
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

        if (pauseButton != null && pauseButton.onClick.GetPersistentEventCount() == 0)
        {
            pauseButton.onClick.RemoveAllListeners();
            pauseButton.onClick.AddListener(OpenPauseMenu);
        }

        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveAllListeners();
            resumeButton.onClick.AddListener(ClosePauseMenu);
        }

        if (pauseRetryButton != null)
        {
            pauseRetryButton.onClick.RemoveAllListeners();
            pauseRetryButton.onClick.AddListener(() =>
            {
                if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
                Time.timeScale = 1f;
                if (GameManager.Instance != null) GameManager.Instance.RestartGame();
            });
        }

        if (pauseMainMenuButton != null)
        {
            pauseMainMenuButton.onClick.RemoveAllListeners();
            pauseMainMenuButton.onClick.AddListener(() =>
            {
                if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonClick();
                Time.timeScale = 1f;
                if (GameManager.Instance != null) GameManager.Instance.GoToMainMenu();
            });
        }

        if (pauseBgmSlider != null)
        {
            pauseBgmSlider.minValue = 0f;
            pauseBgmSlider.maxValue = 1f;
            pauseBgmSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat("BGM_Volume", 0.7f));
            pauseBgmSlider.onValueChanged.RemoveAllListeners();
            pauseBgmSlider.onValueChanged.AddListener(val =>
            {
                PlayerPrefs.SetFloat("BGM_Volume", val);
                PlayerPrefs.Save();
            });
        }

        if (pauseSfxSlider != null)
        {
            pauseSfxSlider.minValue = 0f;
            pauseSfxSlider.maxValue = 1f;
            pauseSfxSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat("SFX_Volume", 0.85f));
            pauseSfxSlider.onValueChanged.RemoveAllListeners();
            pauseSfxSlider.onValueChanged.AddListener(val =>
            {
                PlayerPrefs.SetFloat("SFX_Volume", val);
                PlayerPrefs.Save();
                if (SoundManager.Instance != null) SoundManager.Instance.RefreshVolumes();
            });
        }

        if (pauseVoiceOverToggle != null)
        {
            if (pauseVoiceOverBg == null) pauseVoiceOverBg = pauseVoiceOverToggle.GetComponent<Image>();
            if (pauseVoiceOverText == null) pauseVoiceOverText = pauseVoiceOverToggle.GetComponentInChildren<Text>(true);
            if (pauseVoiceOverCheckIcon == null)
            {
                Transform tChk = pauseVoiceOverToggle.transform.Find("CheckIcon");
                if (tChk != null) pauseVoiceOverCheckIcon = tChk.gameObject;
            }
            bool voOn = PlayerPrefs.GetInt("VoiceOver_Enabled", 1) == 1;
            pauseVoiceOverToggle.SetIsOnWithoutNotify(voOn);
            UpdatePauseVOToggleVisual(voOn);
            pauseVoiceOverToggle.onValueChanged.RemoveAllListeners();
            pauseVoiceOverToggle.onValueChanged.AddListener(isOn =>
            {
                PlayerPrefs.SetInt("VoiceOver_Enabled", isOn ? 1 : 0);
                PlayerPrefs.Save();
                UpdatePauseVOToggleVisual(isOn);
                if (!isOn && VoiceOverManager.Instance != null) VoiceOverManager.Instance.StopVO();
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

    private void UpdatePauseVOToggleVisual(bool isOn)
    {
        if (pauseVoiceOverBg != null)
            pauseVoiceOverBg.color = isOn ? new Color(0.13f, 0.77f, 0.36f) : new Color(0.88f, 0.90f, 0.93f);
        if (pauseVoiceOverText != null)
        {
            pauseVoiceOverText.text = isOn ? "AKTIF" : "MATI";
            pauseVoiceOverText.color = isOn ? Color.white : new Color(0.12f, 0.16f, 0.22f);
        }
        if (pauseVoiceOverCheckIcon != null)
            pauseVoiceOverCheckIcon.SetActive(isOn);
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
        if (pauseBgmSlider != null) pauseBgmSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat("BGM_Volume", 0.7f));
        if (pauseSfxSlider != null) pauseSfxSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat("SFX_Volume", 0.85f));
        if (pauseVoiceOverToggle != null)
        {
            bool voOn = PlayerPrefs.GetInt("VoiceOver_Enabled", 1) == 1;
            pauseVoiceOverToggle.SetIsOnWithoutNotify(voOn);
            UpdatePauseVOToggleVisual(voOn);
        }
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
            pausePanel.transform.SetAsLastSibling();
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

        if (holdProgressText != null && max > 0f)
        {
            holdProgressText.text = $"Bertahan: {Mathf.Min(current, max):F1} / {max:F1} dtk";
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

        buGuruHUD.gameObject.SetActive(true);
        buGuruHUD.SetAsLastSibling();

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
        if (buGuruHUD == null || !buGuruHUD.gameObject.activeInHierarchy) return;
        if (buGuruSlideRoutine != null) StopCoroutine(buGuruSlideRoutine);
        buGuruSlideRoutine = StartCoroutine(SlideBuGuruRoutine(false, 0f));
    }

    private IEnumerator SlideBuGuruRoutine(bool slideIn, float autoHideDelay)
    {
        float duration = 0.35f;
        float elapsed = 0f;

        float offLeftX = -Mathf.Max(320f, buGuruHUD.sizeDelta.x + 60f);
        if (slideIn && buGuruHUD.anchoredPosition.x < offLeftX + 10f)
        {
            buGuruHUD.anchoredPosition = new Vector2(offLeftX, buGuruHUD.anchoredPosition.y);
        }

        Vector2 startPos = buGuruHUD.anchoredPosition;
        Vector2 targetPos = slideIn ? new Vector2(28f, startPos.y) : new Vector2(offLeftX, startPos.y);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float ease = 1f + 2.70158f * Mathf.Pow(t - 1f, 3f) + 1.70158f * Mathf.Pow(t - 1f, 2f);
            if (!slideIn) ease = Mathf.SmoothStep(0f, 1f, t);

            buGuruHUD.anchoredPosition = Vector2.LerpUnclamped(startPos, targetPos, ease);
            yield return null;
        }
        buGuruHUD.anchoredPosition = targetPos;

        if (!slideIn)
        {
            buGuruHUD.gameObject.SetActive(false);
            yield break;
        }

        if (autoHideDelay > 0f)
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
            levelCompletePanel.transform.SetAsLastSibling();
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
            finalScorePanel.transform.SetAsLastSibling();
        }

        if (scoreTimeText != null)
        {
            int mins = Mathf.FloorToInt(totalTime / 60f);
            int secs = Mathf.FloorToInt(totalTime % 60f);
            scoreTimeText.text = $"Waktu Selesai: {mins:00}:{secs:00} Detik • Sangat Tanggap & Mandiri";
        }

        if (scorePointsText != null)
        {
            string grade = score >= 100 ? "SEMPURNA!" : (score >= 80 ? "SANGAT BAIK!" : "LULUS");
            scorePointsText.text = $"TOTAL SKOR KELULUSAN: {score} / 100 ({grade})";
        }

        bool hasImageStars = false;
        if (finalScoreStarImages != null && finalScoreStarImages.Length == 3)
        {
            for (int i = 0; i < 3; i++)
            {
                if (finalScoreStarImages[i] != null)
                {
                    hasImageStars = true;
                    finalScoreStarImages[i].color = (i < stars) ? new Color(0.98f, 0.75f, 0.14f) : new Color(0.82f, 0.85f, 0.90f);
                }
            }
        }

        if (scoreStarsText != null && !hasImageStars)
        {
            scoreStarsText.text = $"PERINGKAT: {stars} / 3 BINTANG";
        }

        if (scoreEvaluationText != null)
        {
            string playerName = PlayerPrefs.GetString("PlayerName", "Ananda");
            scoreEvaluationText.text = $"Catatan Bu Guru: \"Ananda {playerName} tanggap dan tertib dalam mitigasi gempa bumi!\"";
        }

        if (scorePillar1Text != null) scorePillar1Text.text = "LEVEL 1: 30/30";
        if (scorePillar2Text != null) scorePillar2Text.text = "LEVEL 2: 35/35";
        if (scorePillar3Text != null) scorePillar3Text.text = "LEVEL 3: 35/35";

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayGameWin();
        }
    }

    #endregion
}
