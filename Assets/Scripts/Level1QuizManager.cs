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
        public AudioClip[] optionAudios = new AudioClip[3]; // Opsional (untuk soal suara)
        public int correctOptionIndex = 0; // 0 = A, 1 = B, 2 = C
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
        // Pastikan panel kuis selalu nonaktif secara default saat awal game
        if (quizPanel != null)
        {
            quizPanel.SetActive(false);
        }

        // Inisialisasi default 3 soal sesuai dokumen acuan jika belum di-set di Inspector
        if (questions == null || questions.Length == 0)
        {
            SetupDefaultQuestions();
        }

        // Hubungkan event click tombol opsi 0, 1, 2
        for (int i = 0; i < optionButtons.Length; i++)
        {
            int index = i;
            if (optionButtons[i] != null)
            {
                optionButtons[i].onClick.AddListener(() => OnOptionClicked(index));
            }
        }
    }

    public void SetupDefaultQuestions()
    {
        questions = new QuizQuestion[3];

        // Soal 1: Jenis Bencana
        questions[0] = new QuizQuestion
        {
            questionPrompt = "Rumah manakah yang sedang terkena gempa bumi?",
            optionLabels = new string[] { "Rumah A (Api / Terbakar)", "Rumah B (Berguncang / Gempa)", "Rumah C (Banjir / Air)" },
            correctOptionIndex = 1, // Rumah B
            correctFeedback = "Tepat sekali! Rumah B berguncang karena gempa bumi!",
            wrongFeedback = "Bukan yang itu. Cari rumah yang berguncang ya!"
        };

        // Soal 2: Tanda Visual
        questions[1] = new QuizQuestion
        {
            questionPrompt = "Saat gempa, benda apa di dalam kelas yang bergoyang?",
            optionLabels = new string[] { "Lampu Gantung (Bergoyang)", "Bola di Lantai (Diam)", "Buku di Rak (Diam)" },
            correctOptionIndex = 0, // Lampu Gantung
            correctFeedback = "Pintar! Lampu gantung akan berayun saat gempa!",
            wrongFeedback = "Coba perhatikan benda yang bergoyang ke kiri dan kanan!"
        };

        // Soal 3: Tanda Audio
        questions[2] = new QuizQuestion
        {
            questionPrompt = "Dengarkan suara berikut, mana suara gemuruh gempa?",
            optionLabels = new string[] { "Suara A (Gemuruh Gempa)", "Suara B (Hujan Deras)", "Suara C (Petir)" },
            correctOptionIndex = 0, // Gemuruh
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
        if (currentQuestionIndex >= questions.Length)
        {
            CompleteQuiz();
            return;
        }

        QuizQuestion q = questions[currentQuestionIndex];
        if (promptText != null) promptText.text = $"Soal {currentQuestionIndex + 1}/3:\n{q.questionPrompt}";
        if (feedbackText != null) feedbackText.text = "";

        // Putar audio pertanyaan jika ada
        if (voiceAudioSource != null && q.questionAudio != null)
        {
            voiceAudioSource.Stop();
            voiceAudioSource.clip = q.questionAudio;
            voiceAudioSource.Play();
        }

        // Tampilkan 3 opsi
        for (int i = 0; i < 3; i++)
        {
            if (i < optionTexts.Length && optionTexts[i] != null && i < q.optionLabels.Length)
            {
                optionTexts[i].text = q.optionLabels[i];
            }
            if (i < optionImages.Length && optionImages[i] != null && i < q.optionIcons.Length)
            {
                if (q.optionIcons[i] != null)
                {
                    optionImages[i].sprite = q.optionIcons[i];
                    optionImages[i].enabled = true;
                }
            }
            if (i < optionButtons.Length && optionButtons[i] != null)
            {
                optionButtons[i].interactable = true;
            }
        }

        isAnsweringLocked = false;
    }

    public void OnOptionClicked(int optionIndex)
    {
        if (isAnsweringLocked || currentQuestionIndex >= questions.Length) return;

        QuizQuestion q = questions[currentQuestionIndex];

        // Jika soal memiliki cuplikan audio untuk opsi tersebut, putar
        if (q.optionAudios != null && optionIndex < q.optionAudios.Length && q.optionAudios[optionIndex] != null)
        {
            if (sfxAudioSource != null)
            {
                sfxAudioSource.PlayOneShot(q.optionAudios[optionIndex]);
            }
        }

        if (optionIndex == q.correctOptionIndex)
        {
            // Jawaban BENAR
            StartCoroutine(HandleCorrectAnswer(q));
        }
        else
        {
            // Jawaban SALAH
            StartCoroutine(HandleWrongAnswer(q));
        }
    }

    private IEnumerator HandleCorrectAnswer(QuizQuestion q)
    {
        isAnsweringLocked = true;

        if (feedbackText != null)
        {
            feedbackText.color = new Color(0.2f, 0.9f, 0.3f);
            feedbackText.text = q.correctFeedback;
        }

        if (sfxAudioSource != null && correctSfx != null)
        {
            sfxAudioSource.PlayOneShot(correctSfx);
        }

        // Tambah poin gamifikasi di GameManager (+10 poin per soal benar)
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore(10);
        }

        yield return new WaitForSeconds(1.6f);

        currentQuestionIndex++;
        DisplayCurrentQuestion();
    }

    private IEnumerator HandleWrongAnswer(QuizQuestion q)
    {
        if (feedbackText != null)
        {
            feedbackText.color = new Color(1f, 0.4f, 0.4f);
            feedbackText.text = q.wrongFeedback;
        }

        if (sfxAudioSource != null && wrongSfx != null)
        {
            sfxAudioSource.PlayOneShot(wrongSfx);
        }

        yield return new WaitForSeconds(1.2f);
    }

    private void CompleteQuiz()
    {
        if (quizPanel != null) quizPanel.SetActive(false);

        // Notifikasi ke GameManager bahwa Level 1 Kuis telah selesai
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnLevel1QuizFinished();
        }
    }
}
