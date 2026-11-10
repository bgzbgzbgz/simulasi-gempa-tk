#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// Tool 1-Klik untuk membuat GameManager, Canvas HUD Misi, Panel Kuis Level 1,
/// Popup Level Complete, dan Popup Skor Akhir secara otomatis di scene aktif.
/// </summary>
public static class EvacuationSetupEditor
{
    [MenuItem("Tools/Setup Game Manager & UI Evakuasi")]
    public static void SetupEvacuationSystem()
    {
        // 1. Pastikan EventSystem tersedia
        EnsureEventSystem();

        // 2. Pastikan Canvas tersedia
        Canvas canvas = EnsureCanvas();

        // 3. Pastikan GameManager ada di scene
        GameManager gameManager = Object.FindFirstObjectByType<GameManager>();
        if (gameManager == null)
        {
            GameObject gmObj = new GameObject("GameManager", typeof(GameManager));
            gameManager = gmObj.GetComponent<GameManager>();
            Undo.RegisterCreatedObjectUndo(gmObj, "Create GameManager");
        }

        // 4. Cari atau buat MissionUI di bawah Canvas
        MissionUI missionUI = Object.FindFirstObjectByType<MissionUI>();
        if (missionUI == null)
        {
            GameObject uiRoot = new GameObject("Mission_UI_System", typeof(RectTransform), typeof(MissionUI));
            uiRoot.transform.SetParent(canvas.transform, false);

            RectTransform rootRect = uiRoot.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            missionUI = uiRoot.GetComponent<MissionUI>();
            Undo.RegisterCreatedObjectUndo(uiRoot, "Create Mission UI");
        }

        Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                        ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

        // 5. Buat HUD Atas (Teks Misi & Timer)
        Transform existingHUD = missionUI.transform.Find("HUD_Top");
        if (existingHUD == null)
        {
            GameObject hudObj = new GameObject("HUD_Top", typeof(RectTransform));
            hudObj.transform.SetParent(missionUI.transform, false);

            RectTransform hudRect = hudObj.GetComponent<RectTransform>();
            hudRect.anchorMin = new Vector2(0f, 1f);
            hudRect.anchorMax = new Vector2(1f, 1f);
            hudRect.pivot = new Vector2(0.5f, 1f);
            hudRect.anchoredPosition = new Vector2(0f, -20f);
            hudRect.sizeDelta = new Vector2(-40f, 100f);

            // Background banner misi
            GameObject bannerObj = new GameObject("MissionBanner", typeof(RectTransform), typeof(Image));
            bannerObj.transform.SetParent(hudObj.transform, false);
            RectTransform bannerRect = bannerObj.GetComponent<RectTransform>();
            bannerRect.anchorMin = new Vector2(0.1f, 0.2f);
            bannerRect.anchorMax = new Vector2(0.75f, 1f);
            bannerRect.offsetMin = Vector2.zero;
            bannerRect.offsetMax = Vector2.zero;
            Image bannerImg = bannerObj.GetComponent<Image>();
            bannerImg.color = new Color(0.08f, 0.1f, 0.14f, 0.75f);

            // Teks Misi
            GameObject textObj = new GameObject("InstructionText", typeof(RectTransform), typeof(Text));
            textObj.transform.SetParent(bannerObj.transform, false);
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(15f, 5f);
            textRect.offsetMax = new Vector2(-15f, -5f);
            Text instrText = textObj.GetComponent<Text>();
            instrText.font = defaultFont;
            instrText.fontSize = 20;
            instrText.alignment = TextAnchor.MiddleLeft;
            instrText.color = Color.white;
            instrText.text = "Misi: Cari kolong meja untuk berlindung!";
            missionUI.instructionText = instrText;

            // Timer Badge
            GameObject timerObj = new GameObject("TimerBadge", typeof(RectTransform), typeof(Image));
            timerObj.transform.SetParent(hudObj.transform, false);
            RectTransform timerRect = timerObj.GetComponent<RectTransform>();
            timerRect.anchorMin = new Vector2(0.78f, 0.2f);
            timerRect.anchorMax = new Vector2(0.98f, 1f);
            timerRect.offsetMin = Vector2.zero;
            timerRect.offsetMax = Vector2.zero;
            Image timerImg = timerObj.GetComponent<Image>();
            timerImg.color = new Color(0.15f, 0.18f, 0.25f, 0.85f);

            GameObject timerTextObj = new GameObject("TimerText", typeof(RectTransform), typeof(Text));
            timerTextObj.transform.SetParent(timerObj.transform, false);
            RectTransform timerTextRect = timerTextObj.GetComponent<RectTransform>();
            timerTextRect.anchorMin = Vector2.zero;
            timerTextRect.anchorMax = Vector2.one;
            timerTextRect.offsetMin = Vector2.zero;
            timerTextRect.offsetMax = Vector2.zero;
            Text timerText = timerTextObj.GetComponent<Text>();
            timerText.font = defaultFont;
            timerText.fontSize = 24;
            timerText.fontStyle = FontStyle.Bold;
            timerText.alignment = TextAnchor.MiddleCenter;
            timerText.color = new Color(1f, 0.85f, 0.3f);
            timerText.text = "00:00";
            missionUI.timerText = timerText;
        }

        // 6. Buat Progress Bar Berlindung (Hold Timer Slider)
        Transform existingHold = missionUI.transform.Find("HoldProgressContainer");
        if (existingHold == null)
        {
            GameObject holdObj = new GameObject("HoldProgressContainer", typeof(RectTransform), typeof(Image));
            holdObj.transform.SetParent(missionUI.transform, false);
            RectTransform holdRect = holdObj.GetComponent<RectTransform>();
            holdRect.anchorMin = new Vector2(0.3f, 0.25f);
            holdRect.anchorMax = new Vector2(0.7f, 0.32f);
            holdRect.offsetMin = Vector2.zero;
            holdRect.offsetMax = Vector2.zero;
            Image holdBg = holdObj.GetComponent<Image>();
            holdBg.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);

            GameObject labelObj = new GameObject("HoldLabel", typeof(RectTransform), typeof(Text));
            labelObj.transform.SetParent(holdObj.transform, false);
            RectTransform labelRect = labelObj.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 1f);
            labelRect.anchorMax = new Vector2(1f, 1.8f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            Text labelText = labelObj.GetComponent<Text>();
            labelText.font = defaultFont;
            labelText.fontSize = 18;
            labelText.fontStyle = FontStyle.Bold;
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.color = new Color(0.4f, 1f, 0.5f);
            labelText.text = "TETAP BERLINDUNG...";

            GameObject sliderObj = new GameObject("HoldSlider", typeof(RectTransform), typeof(Slider));
            sliderObj.transform.SetParent(holdObj.transform, false);
            RectTransform sliderRect = sliderObj.GetComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0.05f, 0.2f);
            sliderRect.anchorMax = new Vector2(0.95f, 0.8f);
            sliderRect.offsetMin = Vector2.zero;
            sliderRect.offsetMax = Vector2.zero;
            Slider slider = sliderObj.GetComponent<Slider>();
            slider.interactable = false;
            slider.transition = Selectable.Transition.None;

            GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(sliderObj.transform, false);
            RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = Vector2.zero;
            fillAreaRect.offsetMax = Vector2.zero;

            GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            RectTransform fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            Image fillImage = fill.GetComponent<Image>();
            fillImage.color = new Color(0.2f, 0.9f, 0.4f);

            slider.fillRect = fillRect;
            slider.value = 0f;

            missionUI.holdProgressSlider = slider;
            missionUI.holdProgressContainer = holdObj;
            holdObj.SetActive(false);
        }

        // 7. Buat Panel Kuis Level 1 (3 Pilihan Bencana / Soal)
        Level1QuizManager quizMgr = Object.FindFirstObjectByType<Level1QuizManager>();
        Transform existingQuiz = missionUI.transform.Find("Panel_Level1_Quiz");
        if (existingQuiz == null)
        {
            GameObject quizPanelObj = CreateModalPanel("Panel_Level1_Quiz", missionUI.transform);
            quizMgr = quizPanelObj.AddComponent<Level1QuizManager>();

            GameObject card = CreateCard("Card_Quiz", quizPanelObj.transform, new Vector2(980f, 520f));

            Text qPrompt = CreateText("Question_Prompt", card.transform, "Soal 1/3:\nRumah manakah yang sedang terkena gempa bumi?", 26, FontStyle.Bold, new Color(1f, 0.9f, 0.3f), defaultFont, new Vector2(0f, 180f), new Vector2(900f, 80f));
            quizMgr.promptText = qPrompt;
            quizMgr.quizPanel = quizPanelObj;

            // 3 Tombol Opsi Pilihan A, B, C
            float[] xOffsets = new float[] { -300f, 0f, 300f };
            string[] defaultLabels = new string[] { "Pilihan A\n(Kebakaran)", "Pilihan B\n(Gempa Bumi)", "Pilihan C\n(Tsunami)" };
            Color[] cardColors = new Color[] { new Color(0.22f, 0.28f, 0.4f), new Color(0.22f, 0.28f, 0.4f), new Color(0.22f, 0.28f, 0.4f) };

            for (int i = 0; i < 3; i++)
            {
                Button btn = CreateButton($"Btn_Option_{i}", card.transform, defaultLabels[i], cardColors[i], defaultFont, new Vector2(xOffsets[i], 10f), new Vector2(270f, 180f));
                quizMgr.optionButtons[i] = btn;
                quizMgr.optionTexts[i] = btn.GetComponentInChildren<Text>();
                quizMgr.optionTexts[i].fontSize = 20;
            }

            Text feedback = CreateText("FeedbackText", card.transform, "", 22, FontStyle.Bold, Color.white, defaultFont, new Vector2(0f, -140f), new Vector2(800f, 50f));
            quizMgr.feedbackText = feedback;

            quizMgr.SetupDefaultQuestions();
            gameManager.quizManager = quizMgr;
        }

        // 8. Buat Modal Popup Level Complete
        Transform existingComplete = missionUI.transform.Find("Popup_LevelComplete");
        if (existingComplete == null)
        {
            GameObject compObj = CreateModalPanel("Popup_LevelComplete", missionUI.transform);
            missionUI.levelCompletePanel = compObj;

            GameObject card = CreateCard("Card", compObj.transform, new Vector2(600f, 320f));

            Text title = CreateText("Title", card.transform, "LEVEL SELESAI!", 32, FontStyle.Bold, new Color(0.3f, 0.9f, 0.5f), defaultFont, new Vector2(0f, 90f), new Vector2(500f, 60f));
            missionUI.levelCompleteTitleText = title;

            Text desc = CreateText("Desc", card.transform, "Bagus sekali! Tahap ini berhasil diselesaikan dengan baik.", 20, FontStyle.Normal, Color.white, defaultFont, new Vector2(0f, 20f), new Vector2(500f, 70f));
            missionUI.levelCompleteDescText = desc;

            Button nextBtn = CreateButton("Btn_NextLevel", card.transform, "LANJUT KE LEVEL BERIKUTNYA ▶", new Color(0.18f, 0.65f, 0.35f), defaultFont, new Vector2(0f, -75f), new Vector2(380f, 55f));
            missionUI.nextLevelButton = nextBtn;

            compObj.SetActive(false);
        }

        // 9. Buat Modal Popup Skor Akhir
        Transform existingScore = missionUI.transform.Find("Popup_FinalScore");
        if (existingScore == null)
        {
            GameObject scoreObj = CreateModalPanel("Popup_FinalScore", missionUI.transform);
            missionUI.finalScorePanel = scoreObj;

            GameObject card = CreateCard("Card", scoreObj.transform, new Vector2(650f, 440f));

            CreateText("Title", card.transform, "SIMULASI SELESAI!", 32, FontStyle.Bold, new Color(1f, 0.85f, 0.2f), defaultFont, new Vector2(0f, 150f), new Vector2(500f, 50f));

            Text stars = CreateText("Stars", card.transform, "★ ★ ★", 48, FontStyle.Bold, new Color(1f, 0.85f, 0.2f), defaultFont, new Vector2(0f, 90f), new Vector2(500f, 65f));
            missionUI.scoreStarsText = stars;

            Text points = CreateText("PointsText", card.transform, "Total Poin: 50 Poin", 24, FontStyle.Bold, new Color(0.4f, 0.9f, 1f), defaultFont, new Vector2(0f, 35f), new Vector2(500f, 40f));
            missionUI.scorePointsText = points;

            Text time = CreateText("TimeText", card.transform, "Total Waktu: 00:45", 22, FontStyle.Normal, Color.white, defaultFont, new Vector2(0f, -10f), new Vector2(500f, 35f));
            missionUI.scoreTimeText = time;

            Text eval = CreateText("EvalText", card.transform, "Luar Biasa! Kamu adalah Pahlawan Gempa!", 19, FontStyle.Normal, new Color(0.85f, 0.85f, 0.9f), defaultFont, new Vector2(0f, -55f), new Vector2(520f, 45f));
            missionUI.scoreEvaluationText = eval;

            Button retryBtn = CreateButton("Btn_Retry", card.transform, "ULANGI LAGI ↺", new Color(0.2f, 0.45f, 0.85f), defaultFont, new Vector2(-140f, -135f), new Vector2(240f, 50f));
            missionUI.retryButton = retryBtn;

            Button menuBtn = CreateButton("Btn_MainMenu", card.transform, "MENU UTAMA ⌂", new Color(0.5f, 0.2f, 0.25f), defaultFont, new Vector2(140f, -135f), new Vector2(240f, 50f));
            missionUI.mainMenuButton = menuBtn;

            scoreObj.SetActive(false);
        }

        // 10. Sambungkan referensi GameManager
        gameManager.missionUI = missionUI;
        if (quizMgr != null) gameManager.quizManager = quizMgr;
        BoyController boy = Object.FindFirstObjectByType<BoyController>();
        if (boy != null) gameManager.playerBoy = boy;

        gameManager.waitForStartTrigger = true;
        gameManager.enableLevel1Quiz = false;

        // Pastikan Trigger Mulai Simulasi ada di scene
        GameObject startTrigger = GameObject.Find("Trigger_Mulai_Simulasi");
        if (startTrigger == null)
        {
            startTrigger = new GameObject("Trigger_Mulai_Simulasi");
            startTrigger.transform.position = new Vector3(7.67f, 0f, -8.5f);
            BoxCollider box = startTrigger.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(3f, 2f, 3f);
            box.center = new Vector3(0f, 1f, 0f);

            CheckpointTrigger cp = startTrigger.AddComponent<CheckpointTrigger>();
            cp.isStartSimulationTrigger = true;
            cp.checkpointName = "Zona Mulai Simulasi";
            cp.gizmoColor = new Color(1f, 0.85f, 0.1f, 0.4f);

            Undo.RegisterCreatedObjectUndo(startTrigger, "Create Start Trigger");
        }

        EditorUtility.SetDirty(gameManager);
        EditorUtility.SetDirty(missionUI);

        Selection.activeGameObject = gameManager.gameObject;

        EditorUtility.DisplayDialog(
            "Setup Berhasil!",
            "Seluruh Sistem Simulasi Evakuasi Berhasil Diperbarui:\n\n" +
            "• Tanpa Kuis Awal Dadakan: Simulasi dimulai saat pemain melangkah ke 'Trigger_Mulai_Simulasi'.\n" +
            "• Level 1-2-3 HUD: Timer, petunjuk misi, hold progress bar.\n" +
            "• Popup Selesai & Skor: Perolehan bintang, poin akumulasi, tombol lanjut/ulangi.\n" +
            "• Terhubung otomatis ke GameManager & BoyController.",
            "Mantap");
    }

    private static Canvas EnsureCanvas()
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("UI Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            Undo.RegisterCreatedObjectUndo(canvasObj, "Create Canvas");
        }
        return canvas;
    }

    private static void EnsureEventSystem()
    {
        EventSystem es = Object.FindFirstObjectByType<EventSystem>();
        if (es == null)
        {
            GameObject esObj = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            Undo.RegisterCreatedObjectUndo(esObj, "Create EventSystem");
        }
    }

    private static GameObject CreateModalPanel(string name, Transform parent)
    {
        GameObject modal = new GameObject(name, typeof(RectTransform), typeof(Image));
        modal.transform.SetParent(parent, false);

        RectTransform rect = modal.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image img = modal.GetComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.75f);
        return modal;
    }

    private static GameObject CreateCard(string name, Transform parent, Vector2 size)
    {
        GameObject card = new GameObject(name, typeof(RectTransform), typeof(Image));
        card.transform.SetParent(parent, false);

        RectTransform rect = card.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;

        Image img = card.GetComponent<Image>();
        img.color = new Color(0.12f, 0.14f, 0.2f, 0.95f);
        return card;
    }

    private static Text CreateText(string name, Transform parent, string content, int fontSize, FontStyle style, Color color, Font font, Vector2 pos, Vector2 size)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Text));
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;

        Text txt = obj.GetComponent<Text>();
        txt.font = font;
        txt.fontSize = fontSize;
        txt.fontStyle = style;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = color;
        txt.text = content;
        return txt;
    }

    private static Button CreateButton(string name, Transform parent, string label, Color color, Font font, Vector2 pos, Vector2 size)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;

        Image img = obj.GetComponent<Image>();
        img.color = color;

        Button btn = obj.GetComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.highlightedColor = color * 1.2f;
        cb.pressedColor = color * 0.8f;
        btn.colors = cb;

        GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textObj.transform.SetParent(obj.transform, false);
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10f, 10f);
        textRect.offsetMax = new Vector2(-10f, -10f);

        Text txt = textObj.GetComponent<Text>();
        txt.font = font;
        txt.fontSize = 18;
        txt.fontStyle = FontStyle.Bold;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        txt.text = label;

        return btn;
    }
}
#endif
