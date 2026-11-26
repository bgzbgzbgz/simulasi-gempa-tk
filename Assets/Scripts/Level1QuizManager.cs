using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Mengelola kuis interaktif 2 soal di layar TV untuk Level 1 (Kenali Bencana Gempa Bumi).
/// Soal 1: Identifikasi Animasi Gempa (Animasi Rumah Kebakaran, Gempa, Banjir)
/// Soal 2: Identifikasi Suara Gempa (Suara Api/Sirine, Gemuruh Gempa, Air Banjir)
/// </summary>
public class Level1QuizManager : MonoBehaviour
{
    [Serializable]
    public class QuizOptionAnimation
    {
        public Sprite[] frames;
    }

    [Serializable]
    public class QuizQuestion
    {
        public string questionPrompt = "Pertanyaan Kuis";
        public AudioClip questionAudio;
        public string[] optionLabels = new string[3];
        public Sprite[] optionIcons = new Sprite[3];
        public QuizOptionAnimation[] optionAnimations = new QuizOptionAnimation[3];
        public AudioClip[] optionAudios = new AudioClip[3];
        public int correctOptionIndex = 1; // Default opsi B
        public string correctFeedback = "Tepat sekali!";
        public string wrongFeedback = "Hmm, coba perhatikan lagi ya!";
    }

    [Header("Daftar Soal (2 Soal Saja)")]
    public QuizQuestion[] questions = new QuizQuestion[2];

    [Header("Slot Audio Soal 2 (Tinggal Masukkan SFX di Sini)")]
    [Tooltip("Audio Suara A (Contoh: Api / Sirine Kebakaran)")]
    public AudioClip sfxSoal2_OpsiA_Kebakaran;

    [Tooltip("Audio Suara B - JAWABAN BENAR (Contoh: Gemuruh Gempa Bumi)")]
    public AudioClip sfxSoal2_OpsiB_Gempa;

    [Tooltip("Audio Suara C (Contoh: Gemericik Air Banjir)")]
    public AudioClip sfxSoal2_OpsiC_Banjir;

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
    private AnimatedImageUI[] optionAnimators;

    [Header("Figcraft UI Elements")]
    public Button confirmAnswerButton;
    public RectTransform buGuruCard;
    public Outline[] optionCardOutlines = new Outline[3];
    public Image[] optionCardBorderImages = new Image[3];
    public int selectedOptionIndex = -1;
    private Coroutine startSequenceRoutine;

    private void Awake()
    {
        // JANGAN nonaktifkan quizPanel di Awake, karena jika panel ini yang memiliki script ini,
        // memanggil SetActive(true) akan memicu Awake yang langsung mematikan dirinya sendiri!
        if (quizPanel == null)
        {
            quizPanel = gameObject;
        }

        // Auto-assign references jika belum terhubung
        if (promptText == null)
        {
            promptText = (transform.Find("Card_Quiz/Question_Prompt") ?? transform.Find("Card_Quiz/Text_Prompt"))?.GetComponent<Text>();
        }
        if (feedbackText == null)
        {
            feedbackText = (transform.Find("Card_Quiz/FeedbackText") ?? transform.Find("Card_Quiz/Text_Feedback"))?.GetComponent<Text>();
        }
        if (confirmAnswerButton == null)
        {
            confirmAnswerButton = transform.Find("Card_Quiz/Btn_ConfirmAnswer")?.GetComponent<Button>();
        }
        if (confirmAnswerButton != null && confirmAnswerButton.onClick.GetPersistentEventCount() == 0)
        {
            confirmAnswerButton.onClick.RemoveAllListeners();
            confirmAnswerButton.onClick.AddListener(OnConfirmAnswerClicked);
        }

        if (optionButtons == null || optionButtons.Length != 3 || optionButtons[0] == null)
        {
            optionButtons = new Button[3];
            for (int i = 0; i < 3; i++)
            {
                Transform t = transform.Find($"Card_Quiz/Btn_Option_{i}");
                if (t != null) optionButtons[i] = t.GetComponent<Button>();
            }
        }

        if (optionCardBorderImages == null || optionCardBorderImages.Length != 3)
        {
            optionCardBorderImages = new Image[3];
        }
        for (int i = 0; i < 3; i++)
        {
            if (optionButtons != null && i < optionButtons.Length && optionButtons[i] != null)
            {
                if (optionButtons[i].transform.Find("Card_Fill") != null)
                {
                    optionCardBorderImages[i] = optionButtons[i].GetComponent<Image>();
                }
            }
        }

        // Pastikan optionImages terhubung ke Image_Display (bukan background tombol)
        optionImages = new Image[3];
        for (int i = 0; i < 3; i++)
        {
            if (optionButtons != null && i < optionButtons.Length && optionButtons[i] != null)
            {
                Transform tDisp = optionButtons[i].transform.Find("Image_Display");
                if (tDisp != null)
                {
                    optionImages[i] = tDisp.GetComponent<Image>() ?? tDisp.gameObject.AddComponent<Image>();
                }
                else
                {
                    optionImages[i] = optionButtons[i].GetComponent<Image>();
                }
            }
        }

        if (optionTexts == null || optionTexts.Length != 3 || optionTexts[0] == null)
        {
            optionTexts = new Text[3];
            for (int i = 0; i < 3; i++)
            {
                if (optionButtons != null && i < optionButtons.Length && optionButtons[i] != null)
                {
                    Transform tLbl = optionButtons[i].transform.Find("Text_Label");
                    optionTexts[i] = tLbl != null ? tLbl.GetComponent<Text>() : optionButtons[i].GetComponentInChildren<Text>(true);
                }
            }
        }

        if (sfxAudioSource == null)
        {
            sfxAudioSource = GetComponent<AudioSource>();
            if (sfxAudioSource == null)
            {
                sfxAudioSource = gameObject.AddComponent<AudioSource>();
                sfxAudioSource.playOnAwake = false;
            }
        }

        // Setup komponen AnimatedImageUI pada setiap optionImage
        optionAnimators = new AnimatedImageUI[3];
        for (int i = 0; i < 3; i++)
        {
            if (optionImages[i] != null)
            {
                AnimatedImageUI anim = optionImages[i].GetComponent<AnimatedImageUI>();
                if (anim == null)
                {
                    anim = optionImages[i].gameObject.AddComponent<AnimatedImageUI>();
                }
                optionAnimators[i] = anim;
            }
        }

        // Setup default 2 soal jika belum diset
        if (questions == null || questions.Length != 2 || questions[0] == null || questions[0].optionAnimations == null || questions[0].optionAnimations[0] == null)
        {
            SetupDefaultQuestions();
        }

        // Hubungkan event klik tombol
        for (int i = 0; i < optionButtons.Length; i++)
        {
            int index = i;
            if (optionButtons[i] != null && optionButtons[i].onClick.GetPersistentEventCount() == 0)
            {
                optionButtons[i].onClick.RemoveAllListeners();
                optionButtons[i].onClick.AddListener(() => OnOptionClicked(index));
            }
        }
    }

    public void SetupDefaultQuestions()
    {
        questions = new QuizQuestion[2];

        // -------------------------------------------------------------
        // SOAL 1: IDENTIFIKASI ANIMASI RUMAH GEMPA
        // -------------------------------------------------------------
        questions[0] = new QuizQuestion
        {
            questionPrompt = "Perhatikan ketiga gambar rumah! Rumah manakah yang sedang terkena bahaya?",
            optionLabels = new string[] { "Rumah A", "Rumah B", "Rumah C" },
            correctOptionIndex = 1, // Rumah B
            correctFeedback = "Tepat sekali! Rumah B berguncang kuat karena tanah bergetar akibat gempa!",
            wrongFeedback = "Bukan yang itu. Coba perhatikan rumah yang sedang berguncang!"
        };

        // -------------------------------------------------------------
        // SOAL 2: IDENTIFIKASI SUARA GEMURUH GEMPA
        // -------------------------------------------------------------
        questions[1] = new QuizQuestion
        {
            questionPrompt = "Sentuh tiap kartu untuk mendengar suaranya! Mana suara yang menandakan bahaya?",
            optionLabels = new string[] { "Suara A", "Suara B", "Suara C" },
            correctOptionIndex = 1, // Suara B
            correctFeedback = "Hebat sekali! Itu adalah suara getaran tanah saat terjadi gempa bumi!",
            wrongFeedback = "Bukan yang itu. Sentuh kartunya untuk mendengarkan suara getaran tanah!"
        };

#if UNITY_EDITOR
        AutoAssignAssetsInEditor();
#endif
    }

#if UNITY_EDITOR
    public void AutoAssignAssetsInEditor()
    {
        // 1. Load Frame Animasi untuk Soal 1 (Rumah)
        if (questions != null && questions.Length > 0 && questions[0] != null)
        {
            questions[0].optionAnimations = new QuizOptionAnimation[3];

            // A: Fire
            questions[0].optionAnimations[0] = new QuizOptionAnimation { frames = LoadSpriteFrames("Assets/Sprites/Quiz/Fire") };
            questions[0].optionIcons[0] = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Quiz/card_fire.png");

            // B: Quake
            questions[0].optionAnimations[1] = new QuizOptionAnimation { frames = LoadSpriteFrames("Assets/Sprites/Quiz/Quake") };
            questions[0].optionIcons[1] = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Quiz/card_quake.png");

            // C: Flood
            questions[0].optionAnimations[2] = new QuizOptionAnimation { frames = LoadSpriteFrames("Assets/Sprites/Quiz/Flood") };
            questions[0].optionIcons[2] = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Quiz/card_flood.png");
        }

        // 2. Load Audio & Kartu untuk Soal 2 (Suara)
        if (questions != null && questions.Length > 1 && questions[1] != null)
        {
            questions[1].optionAudios = new AudioClip[3];
            questions[1].optionIcons = new Sprite[3];

            // Cek apakah slot publik sudah diisi atau cari placeholder di Assets/Audio/Quiz/
            if (sfxSoal2_OpsiA_Kebakaran == null)
            {
                sfxSoal2_OpsiA_Kebakaran = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Quiz/Soal2_Suara_A_Kebakaran.wav")
                    ?? UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/sfx_alarm_siren.wav");
            }
            if (sfxSoal2_OpsiB_Gempa == null)
            {
                sfxSoal2_OpsiB_Gempa = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Quiz/Soal2_Suara_B_Gempa.wav")
                    ?? UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/sfx_earthquake_rumble.wav");
            }
            if (sfxSoal2_OpsiC_Banjir == null)
            {
                sfxSoal2_OpsiC_Banjir = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Quiz/Soal2_Suara_C_Banjir.wav")
                    ?? UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/sfx_water_flood.wav");
            }

            // A: Sirine / Api
            questions[1].optionAudios[0] = sfxSoal2_OpsiA_Kebakaran;
            questions[1].optionIcons[0] = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Quiz/card_audio_fire.png");

            // B: Gemuruh Gempa (Jawaban Benar)
            questions[1].optionAudios[1] = sfxSoal2_OpsiB_Gempa;
            questions[1].optionIcons[1] = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Quiz/card_audio_quake.png");

            // C: Air Banjir
            questions[1].optionAudios[2] = sfxSoal2_OpsiC_Banjir;
            questions[1].optionIcons[2] = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Quiz/card_audio_flood.png");
        }

        // 3. SFX Correct / Wrong
        if (correctSfx == null)
        {
            correctSfx = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/sfx_level_complete.wav");
        }
        if (wrongSfx == null)
        {
            wrongSfx = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/sfx_button_click.wav");
        }

        UnityEditor.EditorUtility.SetDirty(this);
    }

    private Sprite[] LoadSpriteFrames(string folderPath)
    {
        List<Sprite> list = new List<Sprite>();
        if (!System.IO.Directory.Exists(folderPath)) return list.ToArray();

        string[] files = System.IO.Directory.GetFiles(folderPath, "*.png");
        System.Array.Sort(files);
        foreach (string file in files)
        {
            string assetPath = file.Replace('\\', '/');
            Sprite sp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sp != null)
            {
                list.Add(sp);
            }
        }
        return list.ToArray();
    }
#endif

    public void StartQuiz()
    {
        Debug.Log("[Level1QuizManager] StartQuiz dipanggil! Membuka panel kuis...");
        if (quizPanel != null)
        {
            quizPanel.SetActive(true);
            quizPanel.transform.SetAsLastSibling();
        }
        else
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
        }

        MissionUI mui = UnityEngine.Object.FindFirstObjectByType<MissionUI>();
        if (mui != null && mui.buGuruHUD != null)
        {
            mui.buGuruHUD.gameObject.SetActive(false);
        }

        currentQuestionIndex = 0;
        isAnsweringLocked = true;
        PopulateQuestionVisuals();

        if (startSequenceRoutine != null) StopCoroutine(startSequenceRoutine);
        startSequenceRoutine = StartCoroutine(StartQuizSequence());
    }

    private IEnumerator StartQuizSequence()
    {
        if (feedbackText != null)
        {
            feedbackText.color = new Color(0.01f, 0.52f, 0.78f);
            feedbackText.text = "Dengarkan penjelasan Bu Guru ya...";
        }
        if (VoiceOverManager.Instance != null && VoiceOverManager.Instance.vo02_Level1Intro != null && PlayerPrefs.GetInt("VoiceOver_Enabled", 1) == 1)
        {
            VoiceOverManager.Instance.PlayVO_02_Level1Intro();
            yield return new WaitForSeconds(VoiceOverManager.Instance.vo02_Level1Intro.length + 0.35f);
        }
        DisplayCurrentQuestion();
    }

    private void DisplayCurrentQuestion()
    {
        if (questions == null || questions.Length == 0 || currentQuestionIndex >= questions.Length)
        {
            CompleteQuiz();
            return;
        }

        QuizQuestion q = questions[currentQuestionIndex];
        PopulateQuestionVisuals();

        if (VoiceOverManager.Instance != null)
        {
            if (currentQuestionIndex == 0) VoiceOverManager.Instance.PlayVO_03_Soal1();
            else if (currentQuestionIndex == 1) VoiceOverManager.Instance.PlayVO_04_Soal2();
        }
        else if (voiceAudioSource != null && q.questionAudio != null)
        {
            voiceAudioSource.Stop();
            voiceAudioSource.clip = q.questionAudio;
            voiceAudioSource.Play();
        }

        StartCoroutine(SlideInBuGuru());
        isAnsweringLocked = false;
    }

    private void PopulateQuestionVisuals()
    {
        if (questions == null || questions.Length == 0 || currentQuestionIndex >= questions.Length) return;

        QuizQuestion q = questions[currentQuestionIndex];
        if (promptText != null)
        {
            promptText.text = q.questionPrompt;
        }
        if (feedbackText != null)
        {
            feedbackText.color = new Color(0.01f, 0.52f, 0.78f);
            feedbackText.text = currentQuestionIndex == 0
                ? "Pilih salah satu gambar lalu tekan tombol Pilih!"
                : "Sentuh salah satu kartu suara lalu tekan tombol Pilih!";
        }

        // Sinkronisasi slot publik Soal 2 audio jika diubah oleh user di Inspector
        if (currentQuestionIndex == 1 && questions != null && questions.Length > 1 && questions[1] != null)
        {
            if (questions[1].optionAudios == null || questions[1].optionAudios.Length != 3)
            {
                questions[1].optionAudios = new AudioClip[3];
            }
            if (sfxSoal2_OpsiA_Kebakaran != null) questions[1].optionAudios[0] = sfxSoal2_OpsiA_Kebakaran;
            if (sfxSoal2_OpsiB_Gempa != null) questions[1].optionAudios[1] = sfxSoal2_OpsiB_Gempa;
            if (sfxSoal2_OpsiC_Banjir != null) questions[1].optionAudios[2] = sfxSoal2_OpsiC_Banjir;
        }

        selectedOptionIndex = -1;
        UpdateSelectionOutlines();

        for (int i = 0; i < 3; i++)
        {
            if (i < optionTexts.Length && optionTexts[i] != null && q.optionLabels != null && i < q.optionLabels.Length)
            {
                optionTexts[i].text = q.optionLabels[i];
                optionTexts[i].alignment = TextAnchor.MiddleCenter;
            }

            // Pastikan Image_Display & AnimatedImageUI terhubung kuat
            if (i < optionButtons.Length && optionButtons[i] != null)
            {
                Transform tDisp = optionButtons[i].transform.Find("Image_Display");
                if (tDisp != null)
                {
                    optionImages[i] = tDisp.GetComponent<Image>() ?? tDisp.gameObject.AddComponent<Image>();
                    if (optionAnimators != null && i < optionAnimators.Length)
                    {
                        optionAnimators[i] = tDisp.GetComponent<AnimatedImageUI>() ?? tDisp.gameObject.AddComponent<AnimatedImageUI>();
                    }
                }
            }

            // Atur apakah opsi memakai animasi frame (seperti Soal 1 Rumah)
            bool hasAnim = q.optionAnimations != null && i < q.optionAnimations.Length &&
                           q.optionAnimations[i] != null && q.optionAnimations[i].frames != null &&
                           q.optionAnimations[i].frames.Length > 0;

            if (optionAnimators != null && i < optionAnimators.Length && optionAnimators[i] != null)
            {
                if (hasAnim)
                {
                    optionAnimators[i].enabled = true;
                    optionAnimators[i].SetFrames(q.optionAnimations[i].frames, 12.5f);
                }
                else
                {
                    optionAnimators[i].enabled = false;
                }
            }

            // Atur visual: icon statis atau sprite frame
            if (i < optionImages.Length && optionImages[i] != null)
            {
                if (hasAnim)
                {
                    optionImages[i].sprite = (q.optionAnimations[i].frames != null && q.optionAnimations[i].frames.Length > 0)
                        ? q.optionAnimations[i].frames[0]
                        : null;
                    optionImages[i].color = Color.white;
                    optionImages[i].preserveAspect = true;
                    optionImages[i].enabled = (optionImages[i].sprite != null);
                }
                else if (q.optionIcons != null && i < q.optionIcons.Length && q.optionIcons[i] != null)
                {
                    optionImages[i].sprite = q.optionIcons[i];
                    optionImages[i].color = Color.white;
                    optionImages[i].preserveAspect = true;
                    optionImages[i].enabled = true;
                }
                else
                {
                    // Tidak ada gambar/animasi: matikan komponen Image agar tidak memunculkan kotak putih polos!
                    optionImages[i].sprite = null;
                    optionImages[i].enabled = false;
                }
            }

            if (i < optionButtons.Length && optionButtons[i] != null)
            {
                optionButtons[i].interactable = true;
            }
        }
    }

    public void OnOptionClicked(int optionIndex)
    {
        if (isAnsweringLocked) return;
        SelectOption(optionIndex);
    }

    public void SelectOption(int optionIndex)
    {
        if (isAnsweringLocked || questions == null || currentQuestionIndex >= questions.Length) return;

        selectedOptionIndex = optionIndex;
        QuizQuestion q = questions[currentQuestionIndex];

        // Putar audio opsi jika ada (sangat penting untuk Soal 2!)
        if (q.optionAudios != null && optionIndex < q.optionAudios.Length && q.optionAudios[optionIndex] != null)
        {
            if (sfxAudioSource != null)
            {
                sfxAudioSource.Stop();
                sfxAudioSource.PlayOneShot(q.optionAudios[optionIndex]);
            }
        }

        UpdateSelectionOutlines();
    }

    public void UpdateSelectionOutlines()
    {
        Color selectedBlue = new Color(0.01f, 0.52f, 0.78f); // #0284C7
        Color normalBorder = new Color(0.8f, 0.84f, 0.88f); // #CBD5E1
        Color normalText = new Color(0.15f, 0.20f, 0.28f);

        Color selectedFill = new Color(0.90f, 0.97f, 1.0f);
        Color normalFill = Color.white;

        if (optionButtons != null)
        {
            for (int i = 0; i < optionButtons.Length; i++)
            {
                if (optionButtons[i] != null)
                {
                    bool isSel = (i == selectedOptionIndex);
                    bool hasRoundedBorder = (optionCardBorderImages != null && i < optionCardBorderImages.Length && optionCardBorderImages[i] != null);

                    if (hasRoundedBorder)
                    {
                        optionCardBorderImages[i].color = isSel ? selectedBlue : normalBorder;
                    }
                    else
                    {
                        Image btnImg = optionButtons[i].GetComponent<Image>();
                        if (btnImg != null) btnImg.color = isSel ? selectedFill : normalFill;
                    }

                    if (optionTexts != null && i < optionTexts.Length && optionTexts[i] != null)
                    {
                        optionTexts[i].color = isSel ? selectedBlue : normalText;
                    }
                }
            }
        }

        if (optionCardOutlines != null)
        {
            for (int i = 0; i < optionCardOutlines.Length; i++)
            {
                if (optionCardOutlines[i] != null)
                {
                    bool isSel = (i == selectedOptionIndex);
                    optionCardOutlines[i].effectColor = isSel ? selectedBlue : normalBorder;
                    optionCardOutlines[i].effectDistance = isSel ? new Vector2(4.5f, -4.5f) : new Vector2(3f, -3f);
                }
            }
        }
    }

    public void OnConfirmAnswerClicked()
    {
        if (isAnsweringLocked || questions == null || currentQuestionIndex >= questions.Length) return;

        if (selectedOptionIndex < 0)
        {
            if (feedbackText != null)
            {
                feedbackText.color = new Color(0.95f, 0.55f, 0.1f);
                feedbackText.text = "Sentuh salah satu kartu dulu lalu tekan tombol Pilih!";
            }
            return;
        }

        QuizQuestion q = questions[currentQuestionIndex];
        if (selectedOptionIndex == q.correctOptionIndex)
        {
            StartCoroutine(HandleCorrectAnswer(q));
        }
        else
        {
            StartCoroutine(HandleWrongAnswer(q));
        }
    }

    public IEnumerator SlideInBuGuru()
    {
        if (buGuruCard == null) yield break;

        Vector2 targetPos = new Vector2(50f, buGuruCard.anchoredPosition.y);
        Vector2 startPos = new Vector2(-220f, targetPos.y);
        buGuruCard.anchoredPosition = startPos;

        float duration = 0.5f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float ease = 1f - Mathf.Pow(1f - t, 3f);
            buGuruCard.anchoredPosition = Vector2.LerpUnclamped(startPos, targetPos, ease);
            yield return null;
        }

        buGuruCard.anchoredPosition = targetPos;
    }

    private IEnumerator HandleCorrectAnswer(QuizQuestion q)
    {
        isAnsweringLocked = true;

        if (feedbackText != null)
        {
            feedbackText.color = new Color(0.10f, 0.65f, 0.25f);
            feedbackText.text = q.correctFeedback;
        }

        if (currentQuestionIndex == 1)
        {
            yield return new WaitForSeconds(0.6f);
        }

        if (VoiceOverManager.Instance != null)
        {
            VoiceOverManager.Instance.PlayVO_05_FeedbackBenar();
        }

        if (sfxAudioSource != null && correctSfx != null)
        {
            sfxAudioSource.PlayOneShot(correctSfx);
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore(15);
        }

        yield return new WaitForSeconds(1.8f);

        currentQuestionIndex++;
        DisplayCurrentQuestion();
    }

    private IEnumerator HandleWrongAnswer(QuizQuestion q)
    {
        isAnsweringLocked = true;

        if (feedbackText != null)
        {
            feedbackText.color = new Color(0.90f, 0.20f, 0.20f);
            feedbackText.text = q.wrongFeedback;
        }

        if (currentQuestionIndex == 1)
        {
            yield return new WaitForSeconds(0.8f);
        }

        if (VoiceOverManager.Instance != null)
        {
            VoiceOverManager.Instance.PlayVO_06_FeedbackSalah();
        }

        if (sfxAudioSource != null && wrongSfx != null)
        {
            sfxAudioSource.PlayOneShot(wrongSfx);
        }

        yield return new WaitForSeconds(1.2f);
        isAnsweringLocked = false;
    }

    private void CompleteQuiz()
    {
        Debug.Log("[Level1QuizManager] Kuis selesai! Menutup panel kuis dan melanjut ke Level 2...");
        if (quizPanel != null)
        {
            quizPanel.SetActive(false);
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnLevel1QuizFinished();
        }
    }
}
