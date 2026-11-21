#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// Tool 1-Klik untuk mengonfigurasi dan memperbarui seluruh UI Gameplay Simulasi Evakuasi (LECVEL_NewMap):
/// 1. HUD Gameplay (Banner Misi, Timer, Tombol Pause, Avatar Bu Guru Slide-in)
/// 2. Modal Pause / Pengaturan (UI_02_SettingsPause)
/// 3. Kuis Level 1 (UI_03_Gameplay_Level1_QuizVN)
/// 4. Popup Level Complete (UI_04_LevelComplete_Transition)
/// 5. Popup Skor Akhir / Rapor Pahlawan Cilik (UI_07_FinalScore_Assessment)
/// Mengadopsi style kartun Upin & Ipin / TK Playful ramah anak.
/// </summary>
public static class EvacuationSetupEditor
{
    [MenuItem("Tools/Setup Game Manager & UI Evakuasi (Upin-Ipin Style)")]
    public static void SetupEvacuationSystemMenu()
    {
        SetupEvacuationSystem(false);
    }

    public static void SetupEvacuationSystem(bool showDialog = false)
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

        Sprite spGuru = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/avatar_guru.png");

        // 5. HUD Atas (Teks Misi, Timer, Tombol Pause)
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
            hudRect.sizeDelta = new Vector2(-40f, 90f);

            // Banner Misi (Merah / Oranye Upin & Ipin Style)
            GameObject bannerObj = CreateUI("MissionBanner", hudObj.transform);
            RectTransform bannerRect = bannerObj.GetComponent<RectTransform>();
            bannerRect.anchorMin = new Vector2(0.12f, 0.15f);
            bannerRect.anchorMax = new Vector2(0.72f, 0.95f);
            bannerRect.offsetMin = Vector2.zero;
            bannerRect.offsetMax = Vector2.zero;
            Image bannerImg = bannerObj.AddComponent<Image>();
            bannerImg.color = new Color(0.94f, 0.27f, 0.24f); // Merah ceria
            AddOutline(bannerObj, new Color(0.12f, 0.16f, 0.22f), 3f);

            Text instrText = CreateText("InstructionText", bannerObj.transform, "Misi: Cari kolong meja dan RUNDUK untuk berlindung!", 20, FontStyle.Bold, Color.white, defaultFont);
            missionUI.instructionText = instrText;

            // Timer Badge
            GameObject timerObj = CreateUI("TimerBadge", hudObj.transform);
            RectTransform timerRect = timerObj.GetComponent<RectTransform>();
            timerRect.anchorMin = new Vector2(0.74f, 0.15f);
            timerRect.anchorMax = new Vector2(0.88f, 0.95f);
            timerRect.offsetMin = Vector2.zero;
            timerRect.offsetMax = Vector2.zero;
            Image timerImg = timerObj.AddComponent<Image>();
            timerImg.color = new Color(1f, 0.98f, 0.92f); // Cream
            AddOutline(timerObj, new Color(0.12f, 0.16f, 0.22f), 3f);

            Text timerText = CreateText("TimerText", timerObj.transform, "00:00", 24, FontStyle.Bold, new Color(0.12f, 0.16f, 0.22f), defaultFont);
            missionUI.timerText = timerText;

            // Tombol Pause
            GameObject pauseObj = CreateUI("Btn_Pause", hudObj.transform);
            RectTransform pauseRect = pauseObj.GetComponent<RectTransform>();
            pauseRect.anchorMin = new Vector2(0.90f, 0.15f);
            pauseRect.anchorMax = new Vector2(0.98f, 0.95f);
            pauseRect.offsetMin = Vector2.zero;
            pauseRect.offsetMax = Vector2.zero;
            Image pauseImg = pauseObj.AddComponent<Image>();
            pauseImg.color = new Color(1f, 0.63f, 0.16f); // Oranye
            AddOutline(pauseObj, new Color(0.12f, 0.16f, 0.22f), 3f);
            Button pauseBtn = pauseObj.AddComponent<Button>();
            CreateText("Icon", pauseObj.transform, "❚❚", 24, FontStyle.Bold, Color.white, defaultFont);
            missionUI.pauseButton = pauseBtn;
        }

        // 6. Bu Guru HUD Avatar (Kiri Layar - Slide In)
        Transform existingGuru = missionUI.transform.Find("HUD_BuGuru");
        if (existingGuru == null)
        {
            GameObject guruHUDObj = CreateUI("HUD_BuGuru", missionUI.transform);
            RectTransform ghRt = guruHUDObj.GetComponent<RectTransform>();
            ghRt.anchorMin = new Vector2(0f, 0.5f);
            ghRt.anchorMax = new Vector2(0f, 0.5f);
            ghRt.pivot = new Vector2(0f, 0.5f);
            ghRt.anchoredPosition = new Vector2(20f, 80f);
            ghRt.sizeDelta = new Vector2(250f, 160f);

            // Card BG
            Image ghBg = guruHUDObj.AddComponent<Image>();
            ghBg.color = new Color(1f, 0.99f, 0.97f);
            AddOutline(guruHUDObj, new Color(0.12f, 0.16f, 0.22f), 3.5f);

            // Circle Avatar
            GameObject avCircle = CreateUI("AvatarCircle", guruHUDObj.transform);
            RectTransform acRt = avCircle.GetComponent<RectTransform>();
            acRt.anchoredPosition = new Vector2(0f, 25f);
            acRt.sizeDelta = new Vector2(90f, 90f);
            Image acImg = avCircle.AddComponent<Image>();
            acImg.color = new Color(0.85f, 0.98f, 0.88f);
            AddOutline(avCircle, new Color(0.13f, 0.77f, 0.36f), 2.5f);

            // Portrait Bu Guru
            GameObject avImgObj = CreateUI("Portrait", avCircle.transform);
            SetFullStretch(avImgObj.GetComponent<RectTransform>());
            Image pImg = avImgObj.AddComponent<Image>();
            if (spGuru != null) pImg.sprite = spGuru;

            CreateText("NameTxt", guruHUDObj.transform, "Bu Guru", 18, FontStyle.Bold, new Color(0.12f, 0.16f, 0.22f), defaultFont, new Vector2(0f, -38f), new Vector2(200f, 30f));
            CreateText("StatusTxt", guruHUDObj.transform, "🔊 Voice Over", 14, FontStyle.Normal, new Color(0.13f, 0.65f, 0.3f), defaultFont, new Vector2(0f, -62f), new Vector2(200f, 25f));

            missionUI.buGuruHUD = ghRt;
        }

        // 7. Modal Pause / Settings
        Transform existingPause = missionUI.transform.Find("Popup_PauseSettings");
        if (existingPause == null)
        {
            GameObject pausePanelObj = CreateModalPanel("Popup_PauseSettings", missionUI.transform);
            missionUI.pausePanel = pausePanelObj;

            GameObject card = CreateParchmentCard("Card_Pause", pausePanelObj.transform, new Vector2(680f, 480f));
            CreateRibbonHeader("Ribbon_Header", card.transform, "PENGATURAN", defaultFont);

            CreateText("LabelBGM", card.transform, "♫ Musik Latar (BGM)", 18, FontStyle.Bold, new Color(0.12f, 0.16f, 0.22f), defaultFont, new Vector2(-120f, 100f), new Vector2(300f, 30f));
            Slider slBGM = CreateSliderUI("Slider_BGM", card.transform, new Vector2(30f, 70f), new Vector2(450f, 28f));
            missionUI.pauseBgmSlider = slBGM;

            CreateText("LabelSFX", card.transform, "🔔 Efek Suara (SFX)", 18, FontStyle.Bold, new Color(0.12f, 0.16f, 0.22f), defaultFont, new Vector2(-120f, 20f), new Vector2(300f, 30f));
            Slider slSFX = CreateSliderUI("Slider_SFX", card.transform, new Vector2(30f, -10f), new Vector2(450f, 28f));
            missionUI.pauseSfxSlider = slSFX;

            Button btnResume = CreateChunkyButton("Btn_Resume", card.transform, "▶  LANJUTKAN SIMULASI", new Color(0.13f, 0.77f, 0.36f), new Color(0.08f, 0.5f, 0.24f), 20, defaultFont, new Vector2(0f, -75f), new Vector2(450f, 55f));
            missionUI.resumeButton = btnResume;

            Button btnRestart = CreateChunkyButton("Btn_Ulangi", card.transform, "🔄 Ulangi Level", new Color(0.98f, 0.75f, 0.14f), new Color(0.85f, 0.47f, 0.04f), 18, defaultFont, new Vector2(-120f, -150f), new Vector2(210f, 50f));
            missionUI.pauseRetryButton = btnRestart;

            Button btnHome = CreateChunkyButton("Btn_MenuUtama", card.transform, "🏠 Menu Utama", new Color(0.23f, 0.51f, 0.96f), new Color(0.12f, 0.32f, 0.75f), 18, defaultFont, new Vector2(120f, -150f), new Vector2(210f, 50f));
            missionUI.pauseMainMenuButton = btnHome;

            pausePanelObj.SetActive(false);
        }

        // 8. Progress Bar Berlindung (Hold Timer Slider)
        Transform existingHold = missionUI.transform.Find("HoldProgressContainer");
        if (existingHold == null)
        {
            GameObject holdObj = CreateUI("HoldProgressContainer", missionUI.transform);
            RectTransform holdRect = holdObj.GetComponent<RectTransform>();
            holdRect.anchorMin = new Vector2(0.5f, 0.5f);
            holdRect.anchorMax = new Vector2(0.5f, 0.5f);
            holdRect.anchoredPosition = new Vector2(0f, 150f);
            holdRect.sizeDelta = new Vector2(600f, 90f);
            Image holdBg = holdObj.AddComponent<Image>();
            holdBg.color = new Color(1f, 0.98f, 0.92f); // Cream
            AddOutline(holdObj, new Color(0.12f, 0.16f, 0.22f), 3.5f);

            CreateText("HoldLabel", holdObj.transform, "TAHAN TOMBOL RUNDUK...", 20, FontStyle.Bold, new Color(0.12f, 0.16f, 0.22f), defaultFont, new Vector2(0f, 20f), new Vector2(560f, 30f));

            GameObject sliderObj = CreateUI("HoldSlider", holdObj.transform);
            RectTransform sliderRect = sliderObj.GetComponent<RectTransform>();
            sliderRect.anchoredPosition = new Vector2(0f, -18f);
            sliderRect.sizeDelta = new Vector2(540f, 30f);
            Slider slider = sliderObj.AddComponent<Slider>();
            slider.interactable = false;

            GameObject fillArea = CreateUI("Fill Area", sliderObj.transform);
            SetFullStretch(fillArea.GetComponent<RectTransform>());
            GameObject fill = CreateUI("Fill", fillArea.transform);
            SetFullStretch(fill.GetComponent<RectTransform>());
            Image fillImage = fill.AddComponent<Image>();
            fillImage.color = new Color(0.13f, 0.77f, 0.36f); // Hijau cerah

            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.value = 0f;

            missionUI.holdProgressSlider = slider;
            missionUI.holdProgressContainer = holdObj;
            holdObj.SetActive(false);
        }

        // 9. Modal Popup Level Complete (UI_04_LevelComplete_Transition)
        Transform existingComplete = missionUI.transform.Find("Popup_LevelComplete");
        if (existingComplete == null)
        {
            GameObject compObj = CreateModalPanel("Popup_LevelComplete", missionUI.transform);
            missionUI.levelCompletePanel = compObj;

            GameObject card = CreateParchmentCard("Card_LevelComplete", compObj.transform, new Vector2(680f, 420f));

            // Tag Misi Berhasil
            GameObject tagObj = CreateUI("Tag_Berhasil", card.transform);
            RectTransform tagRt = tagObj.GetComponent<RectTransform>();
            tagRt.anchoredPosition = new Vector2(0f, 155f);
            tagRt.sizeDelta = new Vector2(220f, 44f);
            Image tagImg = tagObj.AddComponent<Image>();
            tagImg.color = new Color(0.13f, 0.77f, 0.36f);
            AddOutline(tagObj, new Color(0.12f, 0.16f, 0.22f), 3f);
            CreateText("Txt", tagObj.transform, "MISI BERHASIL!", 18, FontStyle.Bold, Color.white, defaultFont);

            Text title = CreateText("Title", card.transform, "HEBAT KAMU, ANANDA!", 30, FontStyle.Bold, new Color(0.12f, 0.16f, 0.22f), defaultFont, new Vector2(0f, 75f), new Vector2(600f, 50f));
            missionUI.levelCompleteTitleText = title;

            Text desc = CreateText("Desc", card.transform, "Kamu sudah pintar mengenali benda berbahaya saat gempa!", 18, FontStyle.Normal, new Color(0.35f, 0.4f, 0.5f), defaultFont, new Vector2(0f, 15f), new Vector2(580f, 60f));
            missionUI.levelCompleteDescText = desc;

            // Score Pill
            GameObject pillObj = CreateUI("Score_Pill", card.transform);
            RectTransform pillRt = pillObj.GetComponent<RectTransform>();
            pillRt.anchoredPosition = new Vector2(0f, -40f);
            pillRt.sizeDelta = new Vector2(320f, 40f);
            Image pillImg = pillObj.AddComponent<Image>();
            pillImg.color = new Color(0.99f, 0.95f, 0.78f);
            AddOutline(pillObj, new Color(0.95f, 0.78f, 0.25f), 2f);
            CreateText("Txt", pillObj.transform, "★ SKOR: +30 POIN MITIGASI", 16, FontStyle.Bold, new Color(0.7f, 0.35f, 0.05f), defaultFont);

            Button nextBtn = CreateChunkyButton("Btn_NextLevel", card.transform, "LANJUT KE LEVEL BERIKUTNYA  ▶", new Color(0.13f, 0.77f, 0.36f), new Color(0.08f, 0.5f, 0.24f), 20, defaultFont, new Vector2(0f, -125f), new Vector2(450f, 60f));
            missionUI.nextLevelButton = nextBtn;

            compObj.SetActive(false);
        }

        // 10. Modal Popup Skor Akhir (UI_07_FinalScore_Assessment)
        Transform existingScore = missionUI.transform.Find("Popup_FinalScore");
        if (existingScore == null)
        {
            GameObject scoreObj = CreateModalPanel("Popup_FinalScore", missionUI.transform);
            missionUI.finalScorePanel = scoreObj;

            GameObject card = CreateParchmentCard("Card_FinalScore", scoreObj.transform, new Vector2(920f, 680f));
            CreateRibbonHeader("Ribbon_Header", card.transform, "📜 RAPOR PAHLAWAN CILIK", defaultFont);

            Text stars = CreateText("Stars", card.transform, "★ ★ ★", 56, FontStyle.Bold, new Color(0.98f, 0.75f, 0.14f), defaultFont, new Vector2(0f, 235f), new Vector2(400f, 65f));
            AddOutline(stars.gameObject, new Color(0.12f, 0.16f, 0.22f), 3f);
            missionUI.scoreStarsText = stars;

            CreateText("Title", card.transform, "SELAMAT! KAMU JADI PAHLAWAN CILIK SIAGA GEMPA!", 20, FontStyle.Bold, new Color(0.12f, 0.16f, 0.22f), defaultFont, new Vector2(0f, 175f), new Vector2(850f, 35f));
            CreateText("Subtitle", card.transform, "TK AISYIYAH 28 SURABAYA • LULUS SIMULASI MANDIRI", 15, FontStyle.Bold, new Color(0.85f, 0.47f, 0.04f), defaultFont, new Vector2(0f, 145f), new Vector2(850f, 30f));

            // 3 Pilar Skor Level
            float[] pillarX = new float[] { -270f, 0f, 270f };
            string[] pillarTitles = new string[] { "Kuis Bahaya Gempa", "Tanggap Lindung", "Evakuasi Lapangan" };
            string[] pillarLevels = new string[] { "LEVEL 1", "LEVEL 2", "LEVEL 3" };
            Color[] pillarColors = new Color[] { new Color(0.94f, 0.98f, 1f), new Color(1f, 0.98f, 0.92f), new Color(0.94f, 0.99f, 0.96f) };
            Color[] tagColors = new Color[] { new Color(0.08f, 0.45f, 0.8f), new Color(0.95f, 0.55f, 0.1f), new Color(0.13f, 0.77f, 0.36f) };

            for (int i = 0; i < 3; i++)
            {
                GameObject pObj = CreateUI($"Pillar_{i + 1}", card.transform);
                RectTransform pRt = pObj.GetComponent<RectTransform>();
                pRt.anchoredPosition = new Vector2(pillarX[i], 40f);
                pRt.sizeDelta = new Vector2(250f, 150f);
                Image pImg = pObj.AddComponent<Image>();
                pImg.color = pillarColors[i];
                AddOutline(pObj, new Color(0.12f, 0.16f, 0.22f), 3f);

                GameObject pTag = CreateUI("Tag", pObj.transform);
                RectTransform ptRt = pTag.GetComponent<RectTransform>();
                ptRt.anchoredPosition = new Vector2(0f, 55f);
                ptRt.sizeDelta = new Vector2(110f, 28f);
                Image ptImg = pTag.AddComponent<Image>();
                ptImg.color = tagColors[i];
                AddOutline(pTag, new Color(0.12f, 0.16f, 0.22f), 2f);
                CreateText("Txt", pTag.transform, pillarLevels[i], 13, FontStyle.Bold, Color.white, defaultFont);

                CreateText("Title", pObj.transform, pillarTitles[i], 16, FontStyle.Bold, new Color(0.12f, 0.16f, 0.22f), defaultFont, new Vector2(0f, 20f), new Vector2(230f, 30f));
                Text pScore = CreateText("Score", pObj.transform, i == 0 ? "30 / 30 Poin" : "35 / 35 Poin", 20, FontStyle.Bold, tagColors[i], defaultFont, new Vector2(0f, -18f), new Vector2(230f, 35f));

                if (i == 0) missionUI.scorePillar1Text = pScore;
                else if (i == 1) missionUI.scorePillar2Text = pScore;
                else missionUI.scorePillar3Text = pScore;
            }

            // Pita Total Skor Emas
            GameObject ribbonScore = CreateUI("Ribbon_TotalScore", card.transform);
            RectTransform rsRt = ribbonScore.GetComponent<RectTransform>();
            rsRt.anchoredPosition = new Vector2(0f, -65f);
            rsRt.sizeDelta = new Vector2(790f, 55f);
            Image rsImg = ribbonScore.AddComponent<Image>();
            rsImg.color = new Color(0.99f, 0.94f, 0.54f); // Golden butter
            AddOutline(ribbonScore, new Color(0.12f, 0.16f, 0.22f), 3f);
            Text points = CreateText("PointsText", ribbonScore.transform, "🏆 TOTAL SKOR KELULUSAN: 100 / 100 (SEMPURNA!)", 20, FontStyle.Bold, new Color(0.47f, 0.25f, 0.05f), defaultFont);
            missionUI.scorePointsText = points;

            Text time = CreateText("TimeText", card.transform, "⏱️ Waktu Selesai: 00:54 Detik • Sangat Tanggap & Mandiri", 16, FontStyle.Bold, new Color(0.35f, 0.4f, 0.5f), defaultFont, new Vector2(0f, -110f), new Vector2(800f, 28f));
            missionUI.scoreTimeText = time;

            // Catatan Bu Guru (Speech bubble + Avatar)
            GameObject guruNoteObj = CreateUI("Guru_Evaluation", card.transform);
            RectTransform gnRt = guruNoteObj.GetComponent<RectTransform>();
            gnRt.anchoredPosition = new Vector2(0f, -160f);
            gnRt.sizeDelta = new Vector2(790f, 65f);

            GameObject guruAv = CreateUI("Avatar", guruNoteObj.transform);
            RectTransform gaRt = guruAv.GetComponent<RectTransform>();
            gaRt.anchoredPosition = new Vector2(-350f, 0f);
            gaRt.sizeDelta = new Vector2(60f, 60f);
            Image gaImg = guruAv.AddComponent<Image>();
            if (spGuru != null) gaImg.sprite = spGuru;
            else gaImg.color = new Color(0.85f, 0.98f, 0.88f);
            AddOutline(guruAv, new Color(0.12f, 0.16f, 0.22f), 2.5f);

            GameObject noteBubble = CreateUI("Bubble", guruNoteObj.transform);
            RectTransform nbRt = noteBubble.GetComponent<RectTransform>();
            nbRt.anchoredPosition = new Vector2(35f, 0f);
            nbRt.sizeDelta = new Vector2(690f, 55f);
            Image nbImg = noteBubble.AddComponent<Image>();
            nbImg.color = Color.white;
            AddOutline(noteBubble, new Color(0.12f, 0.16f, 0.22f), 2.5f);

            Text eval = CreateText("EvalText", noteBubble.transform, "Catatan Bu Guru: \"Ananda sangat pintar dan mandiri dalam mempraktikkan mitigasi bencana gempa bumi!\"", 15, FontStyle.Bold, new Color(0.12f, 0.16f, 0.22f), defaultFont);
            missionUI.scoreEvaluationText = eval;

            // Tombol Aksi Bawah Skor Akhir
            Button retryBtn = CreateChunkyButton("Btn_Retry", card.transform, "🔄  MAIN LAGI", new Color(1f, 0.63f, 0.16f), new Color(0.85f, 0.47f, 0.04f), 20, defaultFont, new Vector2(-200f, -240f), new Vector2(340f, 60f));
            missionUI.retryButton = retryBtn;

            Button menuBtn = CreateChunkyButton("Btn_MainMenu", card.transform, "🏠  MENU UTAMA", new Color(0.13f, 0.77f, 0.36f), new Color(0.08f, 0.5f, 0.24f), 20, defaultFont, new Vector2(200f, -240f), new Vector2(340f, 60f));
            missionUI.mainMenuButton = menuBtn;

            scoreObj.SetActive(false);
        }

        // 11. Sambungkan referensi GameManager
        gameManager.missionUI = missionUI;
        BoyController boy = Object.FindFirstObjectByType<BoyController>();
        if (boy != null) gameManager.playerBoy = boy;

        EditorUtility.SetDirty(gameManager);
        EditorUtility.SetDirty(missionUI);

        Debug.Log("[EvacuationSetupEditor] Seluruh UI Gameplay (HUD, Pause, LevelComplete, FinalScore) berhasil disinkronkan ke style Upin-Ipin!");
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
            GameObject esObj = new GameObject("EventSystem", typeof(EventSystem));
            var inputType = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputType != null) esObj.AddComponent(inputType);
            else esObj.AddComponent<StandaloneInputModule>();
            Undo.RegisterCreatedObjectUndo(esObj, "Create EventSystem");
        }
    }

    #region Helpers

    private static GameObject CreateUI(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static void SetFullStretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static GameObject CreateModalPanel(string name, Transform parent)
    {
        GameObject panel = CreateUI(name, parent);
        SetFullStretch(panel.GetComponent<RectTransform>());
        Image scrim = panel.AddComponent<Image>();
        scrim.color = new Color(0.04f, 0.07f, 0.17f, 0.65f);
        return panel;
    }

    private static GameObject CreateParchmentCard(string name, Transform parent, Vector2 size)
    {
        GameObject card = CreateUI(name, parent);
        RectTransform rt = card.GetComponent<RectTransform>();
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = size;

        Image img = card.AddComponent<Image>();
        img.color = new Color(1f, 0.984f, 0.922f); // Cream
        AddOutline(card, new Color(0.12f, 0.16f, 0.22f), 4f);
        return card;
    }

    private static void CreateRibbonHeader(string name, Transform parent, string title, Font font)
    {
        GameObject ribbon = CreateUI(name, parent);
        RectTransform rt = ribbon.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, 18f);
        rt.sizeDelta = new Vector2(460f, 55f);

        Image img = ribbon.AddComponent<Image>();
        img.color = new Color(0.98f, 0.75f, 0.14f);
        AddOutline(ribbon, new Color(0.12f, 0.16f, 0.22f), 3.5f);

        CreateText("Text", ribbon.transform, title, 22, FontStyle.Bold, new Color(0.12f, 0.16f, 0.22f), font);
    }

    private static Button CreateChunkyButton(string name, Transform parent, string label, Color topColor, Color bevelColor, int fontSize, Font font, Vector2 pos, Vector2 size)
    {
        GameObject btnObj = CreateUI(name, parent);
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        Image img = btnObj.AddComponent<Image>();
        img.color = topColor;
        AddOutline(btnObj, new Color(0.12f, 0.16f, 0.22f), 3.5f);

        Button btn = btnObj.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.highlightedColor = topColor * 1.1f;
        cb.pressedColor = bevelColor;
        btn.colors = cb;

        GameObject txtObj = CreateUI("Text", btnObj.transform);
        SetFullStretch(txtObj.GetComponent<RectTransform>());
        Text txt = txtObj.AddComponent<Text>();
        txt.text = label;
        txt.font = font;
        txt.fontSize = fontSize;
        txt.fontStyle = FontStyle.Bold;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;

        return btn;
    }

    private static Text CreateText(string name, Transform parent, string content, int fontSize, FontStyle style, Color color, Font font, Vector2 pos = default, Vector2 size = default)
    {
        GameObject textObj = CreateUI(name, parent);
        RectTransform rt = textObj.GetComponent<RectTransform>();
        if (size != default) rt.sizeDelta = size;
        if (pos != default) rt.anchoredPosition = pos;
        else SetFullStretch(rt);

        Text txt = textObj.AddComponent<Text>();
        txt.text = content;
        txt.font = font;
        txt.fontSize = fontSize;
        txt.fontStyle = style;
        txt.color = color;
        txt.alignment = TextAnchor.MiddleCenter;
        return txt;
    }

    private static Outline AddOutline(GameObject go, Color color, float width)
    {
        Outline ol = go.AddComponent<Outline>();
        ol.effectColor = color;
        ol.effectDistance = new Vector2(width, -width);
        return ol;
    }

    private static Slider CreateSliderUI(string name, Transform parent, Vector2 pos, Vector2 size)
    {
        GameObject sliderObj = CreateUI(name, parent);
        RectTransform sRt = sliderObj.GetComponent<RectTransform>();
        sRt.anchoredPosition = pos;
        sRt.sizeDelta = size;

        Slider slider = sliderObj.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0.75f;

        GameObject bgTrack = CreateUI("Background", sliderObj.transform);
        SetFullStretch(bgTrack.GetComponent<RectTransform>());
        Image bgImg = bgTrack.AddComponent<Image>();
        bgImg.color = new Color(0.85f, 0.88f, 0.92f);
        AddOutline(bgTrack, new Color(0.12f, 0.16f, 0.22f), 2f);

        GameObject fillArea = CreateUI("Fill Area", sliderObj.transform);
        SetFullStretch(fillArea.GetComponent<RectTransform>());
        GameObject fill = CreateUI("Fill", fillArea.transform);
        SetFullStretch(fill.GetComponent<RectTransform>());
        Image fillImg = fill.AddComponent<Image>();
        fillImg.color = new Color(0.23f, 0.51f, 0.96f);
        slider.fillRect = fill.GetComponent<RectTransform>();

        return slider;
    }

    #endregion
}
#endif
