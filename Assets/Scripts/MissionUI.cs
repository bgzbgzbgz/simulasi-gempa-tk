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
        {
            nextLevelButton.onClick.AddListener(() =>
            {
                if (GameManager.Instance != null) GameManager.Instance.NextLevel();
            });
        }

        if (retryButton != null)
        {
            retryButton.onClick.AddListener(() =>
            {
                if (GameManager.Instance != null) GameManager.Instance.RestartGame();
            });
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(() =>
            {
                if (GameManager.Instance != null) GameManager.Instance.GoToMainMenu();
            });
        }
    }

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

    public void ShowLevelComplete(string title, string description)
    {
        if (levelCompletePanel != null)
        {
            levelCompletePanel.SetActive(true);
        }
        if (levelCompleteTitleText != null) levelCompleteTitleText.text = title;
        if (levelCompleteDescText != null) levelCompleteDescText.text = description;
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

        if (scoreTimeText != null)
        {
            int mins = Mathf.FloorToInt(totalTime / 60f);
            int secs = Mathf.FloorToInt(totalTime % 60f);
            scoreTimeText.text = $"Total Waktu: {mins:00}:{secs:00}";
        }

        if (scorePointsText != null)
        {
            scorePointsText.text = $"Total Poin: {score} Poin";
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
            scoreEvaluationText.text = evaluation;
        }
    }
}
