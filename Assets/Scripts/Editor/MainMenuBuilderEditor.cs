#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Tool otomatis untuk membuat dan menyusun Scene Menu Utama (MainMenu.unity) secara lengkap,
/// mengadopsi desain High-Fidelity (Tabel 3.7 Laporan PA) dengan palet cerah ramah anak TK.
/// </summary>
public static class MainMenuBuilderEditor
{
    private const string ScenePath = "Assets/Scenes/MainMenu.unity";
    private const string SimulationScenePath = "Assets/Scenes/LECVEL_NewMap.unity";

    private static readonly Color ColDarkInk = new Color(0.10f, 0.14f, 0.22f, 1f);
    private static readonly Color ColCream = new Color(1.00f, 0.984f, 0.922f, 1f);
    private static readonly Color ColOrangeBorder = new Color(0.96f, 0.62f, 0.04f, 1f);
    private static readonly Color ColYellowRibbon = new Color(0.98f, 0.75f, 0.14f, 1f);
    private static readonly Color ColGreen = new Color(0.13f, 0.77f, 0.36f, 1f);
    private static readonly Color ColBlueBorder = new Color(0.01f, 0.52f, 0.78f, 1f);
    private static readonly Color ColRed = new Color(0.94f, 0.27f, 0.24f, 1f);
    private static readonly Color ColSubtleBorder = new Color(0.80f, 0.84f, 0.89f, 1f);
    private static readonly Color ColTrackGrey = new Color(0.89f, 0.91f, 0.94f, 1f);

    [MenuItem("Tools/UI & Mobile/Buat Scene Main Menu Lengkap (Upin-Ipin Style)")]
    public static void CreateMainMenuSceneMenu()
    {
        BuildCompleteMainMenuScene(false);
    }

    [MenuItem("Tools/UI & Mobile/Test Buka Panel Karakter")]
    public static void TestOpenCharacterSelect()
    {
        MainMenuManager mgr = Object.FindFirstObjectByType<MainMenuManager>();
        if (mgr != null)
        {
            mgr.OpenCharacterSelect();
            Debug.Log($"[Test] Panel CharacterSelect activeSelf: {mgr.panelCharacterSelect.activeSelf}");
        }
    }

    [MenuItem("Tools/UI & Mobile/Test Pilih Girl")]
    public static void TestSelectGirl()
    {
        MainMenuManager mgr = Object.FindFirstObjectByType<MainMenuManager>();
        if (mgr != null)
        {
            mgr.OpenCharacterSelect();
            mgr.SelectGirl();
            Debug.Log($"[Test] Selected Girl: {mgr.panelCharacterSelect.activeSelf}");
        }
    }

    public static void BuildCompleteMainMenuScene(bool showDialog = false)
    {
        Scene currentActive = SceneManager.GetActiveScene();
        if (currentActive.isDirty)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        }

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Kamera Utama
        GameObject camObj = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        Camera cam = camObj.GetComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.12f, 0.16f, 0.22f);
        cam.orthographic = true;
        cam.orthographicSize = 5f;

        // SoundManager & VoiceOverManager
        if (Object.FindFirstObjectByType<SoundManager>() == null)
            new GameObject("SoundManager", typeof(SoundManager));
        if (Object.FindFirstObjectByType<VoiceOverManager>() == null)
            new GameObject("VoiceOverManager", typeof(VoiceOverManager));

        // EventSystem
        GameObject esObj = GameObject.Find("EventSystem") ?? new GameObject("EventSystem");
        if (esObj.GetComponent<UnityEngine.EventSystems.EventSystem>() == null)
            esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();

        var inputModule = esObj.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        if (inputModule == null)
        {
            var oldMod = esObj.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            if (oldMod != null) Object.DestroyImmediate(oldMod);
            inputModule = esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }
        var inputAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>("Assets/InputSystem_Actions.inputactions");
        if (inputAsset != null) inputModule.actionsAsset = inputAsset;
        inputModule.AssignDefaultActions();

        // Canvas UI (1920x1080)
        GameObject canvasObj = new GameObject("Canvas_MainMenu", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        Font fontDefault = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Fredoka-Variable.ttf")
                        ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                        ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

        Sprite spBg = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/bg_mainmenu.png");
        Sprite spBoy = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/avatar_boy.png");
        Sprite spGirl = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/avatar_girl.png");
        Sprite spGuru = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/avatar_guru.png");
        Sprite spRoundedFill = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/spr_ui_rounded_fill.png");
        Sprite spBtnRed = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/spr_ui_rounded_btn_red.png");
        Sprite spBtnWhite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/spr_ui_rounded_btn_white.png");
        Sprite spBtnGreen = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/spr_ui_rounded_btn_green.png");
        Sprite spBtnYellow = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/spr_ui_rounded_btn_yellow.png");

        // Background Kelas Utama
        GameObject bgObj = CreateUI("Background_Kelas", canvasObj.transform);
        SetFullStretch(bgObj.GetComponent<RectTransform>());
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.raycastTarget = false;
        if (spBg != null) { bgImg.sprite = spBg; bgImg.type = Image.Type.Simple; bgImg.preserveAspect = false; }
        else bgImg.color = new Color(0.35f, 0.65f, 0.88f);

        MainMenuManager menuMgr = canvasObj.AddComponent<MainMenuManager>();
        menuMgr.simulationSceneName = "LECVEL_NewMap";
        menuMgr.lockGirlCharacter = true;

        // =========================================================================
        // PANEL 1: Layar Menu Utama (Sesuai Tabel 3.7 No. 1)
        // =========================================================================
        GameObject panelMain = CreateUI("Panel_MainMenu", canvasObj.transform);
        SetFullStretch(panelMain.GetComponent<RectTransform>());
        menuMgr.panelMainMenu = panelMain;

        // Tag Kiri Atas: EDUKASI MITIGASI GEMPA
        GameObject tagTopLeft = CreateUI("Tag_EdukasiGempa", panelMain.transform);
        RectTransform tagRt = tagTopLeft.GetComponent<RectTransform>();
        tagRt.anchorMin = tagRt.anchorMax = tagRt.pivot = new Vector2(0f, 1f);
        tagRt.anchoredPosition = new Vector2(60f, -48f);
        tagRt.sizeDelta = new Vector2(480f, 76f);
        ApplySlicedImage(tagTopLeft, spBtnRed, spRoundedFill, ColRed);
        CreateText("Text", tagTopLeft.transform, "EDUKASI MITIGASI GEMPA", 28, FontStyle.Bold, Color.white, fontDefault);

        // Tombol Gear Kanan Atas
        GameObject btnGear = CreateUI("Btn_Gear_Settings", panelMain.transform);
        RectTransform gearRt = btnGear.GetComponent<RectTransform>();
        gearRt.anchorMin = gearRt.anchorMax = gearRt.pivot = new Vector2(1f, 1f);
        gearRt.anchoredPosition = new Vector2(-60f, -48f);
        gearRt.sizeDelta = new Vector2(96f, 96f);
        ApplySlicedImage(btnGear, spBtnWhite, spRoundedFill, Color.white);
        Button gearButton = btnGear.AddComponent<Button>();
        UnityEventTools.AddPersistentListener(gearButton.onClick, menuMgr.OpenSettings);
        CreateGearIcon(btnGear.transform, ColDarkInk);

        // Tombol Utama: MULAI SIMULASI (Tengah bawah papan tulis)
        Button btnMulai = CreateButton("Btn_MulaiSimulasi", panelMain.transform, "MULAI SIMULASI", spBtnGreen, spRoundedFill, ColGreen, Color.white, 34, fontDefault, new Vector2(0f, -195f), new Vector2(726f, 108f));
        UnityEventTools.AddPersistentListener(btnMulai.onClick, menuMgr.OpenCharacterSelect);
        CreatePlayArrowIcon(btnMulai.transform, new Vector2(-175f, 0f), 26f, Color.white);
        RectTransform mulaiTxtRt = btnMulai.transform.Find("Text").GetComponent<RectTransform>();
        mulaiTxtRt.offsetMin = new Vector2(36f, 0f);

        // Baris 3 Tombol Bawah (Pengaturan, Petunjuk, Keluar - Sejajar simetris)
        GameObject rowButtons = CreateUI("Row_Buttons", panelMain.transform);
        RectTransform rowRt = rowButtons.GetComponent<RectTransform>();
        rowRt.anchorMin = rowRt.anchorMax = rowRt.pivot = new Vector2(0.5f, 0.5f);
        rowRt.anchoredPosition = new Vector2(0f, -325f);
        rowRt.sizeDelta = new Vector2(1060f, 84f);

        Button btnSettings = CreateButton("Btn_Pengaturan", rowButtons.transform, "Pengaturan", spBtnWhite, spRoundedFill, Color.white, ColDarkInk, 28, fontDefault, new Vector2(-345f, 0f), new Vector2(300f, 76f));
        UnityEventTools.AddPersistentListener(btnSettings.onClick, menuMgr.OpenSettings);

        Button btnTutorial = CreateButton("Btn_Petunjuk", rowButtons.transform, "Petunjuk", spBtnWhite, spRoundedFill, Color.white, ColDarkInk, 28, fontDefault, new Vector2(0f, 0f), new Vector2(300f, 76f));
        UnityEventTools.AddPersistentListener(btnTutorial.onClick, menuMgr.OpenTutorial);

        Button btnQuit = CreateButton("Btn_Keluar", rowButtons.transform, "Keluar", spBtnWhite, spRoundedFill, Color.white, ColDarkInk, 28, fontDefault, new Vector2(345f, 0f), new Vector2(300f, 76f));
        UnityEventTools.AddPersistentListener(btnQuit.onClick, menuMgr.QuitGame);

        // =========================================================================
        // PANEL 2: Layar Pilih Karakter (Sesuai Tabel 3.7 No. 2 + Putri Terkunci)
        // =========================================================================
        GameObject panelChar = CreateModalPanel("Panel_CharacterSelect", canvasObj.transform);
        menuMgr.panelCharacterSelect = panelChar;

        GameObject cardCharBox = CreateBorderedCard("Card_CharacterSelect", panelChar.transform, spRoundedFill, ColDarkInk, ColCream, 6f, new Vector2(0f, -12f), new Vector2(1680f, 860f));
        CreateRibbonHeader("Ribbon_Header", cardCharBox.transform, "PILIH KARAKTERMU!", spBtnYellow, spRoundedFill, ColYellowRibbon, ColDarkInk, fontDefault, new Vector2(860f, 88f), 36);

        // 1. Kartu Putra (Aktif / Terpilih)
        GameObject cardBoy = CreateBorderedCard("Card_Boy", cardCharBox.transform, spRoundedFill, ColGreen, Color.white, 6f, new Vector2(-370f, 25f), new Vector2(560f, 560f));
        menuMgr.cardBoyBorderImage = cardBoy.GetComponent<Image>();
        menuMgr.cardBoyBg = cardBoy.transform.Find("Card_Fill").GetComponent<Image>();

        GameObject avBoyObj = CreateUI("Avatar_Boy", cardBoy.transform);
        RectTransform avbRt = avBoyObj.GetComponent<RectTransform>();
        avbRt.anchoredPosition = new Vector2(0f, 70f);
        avbRt.sizeDelta = new Vector2(310f, 310f);
        Image avbImg = avBoyObj.AddComponent<Image>();
        avbImg.preserveAspect = true;
        if (spBoy != null) avbImg.sprite = spBoy;

        Button btnSelectBoy = CreateButton("Btn_SelectBoy", cardBoy.transform, "PILIH SAYA", spRoundedFill, spRoundedFill, ColGreen, Color.white, 30, fontDefault, new Vector2(0f, -175f), new Vector2(380f, 88f));
        UnityEventTools.AddPersistentListener(btnSelectBoy.onClick, menuMgr.SelectBoy);
        menuMgr.btnSelectBoyImage = btnSelectBoy.GetComponent<Image>();
        menuMgr.btnSelectBoyText = btnSelectBoy.GetComponentInChildren<Text>();
        GameObject boyChk = CreateCheckIcon("Badge_BoyCheck", btnSelectBoy.transform, new Vector2(115f, 0f), Color.white);
        menuMgr.badgeBoySelected = boyChk;
        btnSelectBoy.transform.Find("Text").GetComponent<RectTransform>().offsetMax = new Vector2(-28f, 0f);

        // 2. Kartu Putri (Terkunci Sementara untuk Demo)
        GameObject cardGirl = CreateBorderedCard("Card_Girl", cardCharBox.transform, spRoundedFill, ColSubtleBorder, new Color(0.96f, 0.97f, 0.98f), 6f, new Vector2(370f, 25f), new Vector2(560f, 560f));
        menuMgr.cardGirlBorderImage = cardGirl.GetComponent<Image>();
        menuMgr.cardGirlBg = cardGirl.transform.Find("Card_Fill").GetComponent<Image>();

        GameObject avGirlObj = CreateUI("Avatar_Girl", cardGirl.transform);
        RectTransform avgRt = avGirlObj.GetComponent<RectTransform>();
        avgRt.anchoredPosition = new Vector2(0f, 70f);
        avgRt.sizeDelta = new Vector2(310f, 310f);
        Image avgImg = avGirlObj.AddComponent<Image>();
        avgImg.preserveAspect = true;
        avgImg.color = new Color(1f, 1f, 1f, 0.55f);
        if (spGirl != null) avgImg.sprite = spGirl;

        GameObject badgeLocked = CreateBorderedCard("Badge_GirlLocked", cardGirl.transform, spRoundedFill, ColOrangeBorder, new Color(1f, 0.95f, 0.78f), 3f, new Vector2(0f, 232f), new Vector2(320f, 52f));
        CreateText("Text", badgeLocked.transform, "SEGERA HADIR (DIKUNCI)", 20, FontStyle.Bold, new Color(0.57f, 0.25f, 0.05f), fontDefault);
        menuMgr.badgeGirlLocked = badgeLocked;

        Button btnSelectGirl = CreateButton("Btn_SelectGirl", cardGirl.transform, "TERKUNCI", spRoundedFill, spRoundedFill, ColTrackGrey, new Color(0.45f, 0.50f, 0.58f), 28, fontDefault, new Vector2(0f, -175f), new Vector2(380f, 88f));
        btnSelectGirl.interactable = false;
        UnityEventTools.AddPersistentListener(btnSelectGirl.onClick, menuMgr.SelectGirl);
        menuMgr.btnSelectGirlImage = btnSelectGirl.GetComponent<Image>();
        menuMgr.btnSelectGirlText = btnSelectGirl.GetComponentInChildren<Text>();

        // Tombol Bawah Character Select
        Button btnBackToMain = CreateButton("Btn_BackToMain", cardCharBox.transform, "KEMBALI", spBtnWhite, spRoundedFill, Color.white, ColDarkInk, 30, fontDefault, new Vector2(-570f, -345f), new Vector2(380f, 92f));
        UnityEventTools.AddPersistentListener(btnBackToMain.onClick, menuMgr.BackToMainMenu);
        CreateLeftArrowIcon(btnBackToMain.transform, new Vector2(-108f, 0f), 22f, ColDarkInk);
        btnBackToMain.transform.Find("Text").GetComponent<RectTransform>().offsetMin = new Vector2(28f, 0f);

        Button btnNextToName = CreateButton("Btn_NextToName", cardCharBox.transform, "LANJUT", spBtnGreen, spRoundedFill, ColGreen, Color.white, 32, fontDefault, new Vector2(510f, -345f), new Vector2(500f, 92f));
        UnityEventTools.AddPersistentListener(btnNextToName.onClick, menuMgr.OpenNameInput);
        CreatePlayArrowIcon(btnNextToName.transform, new Vector2(100f, 0f), 24f, Color.white);
        btnNextToName.transform.Find("Text").GetComponent<RectTransform>().offsetMax = new Vector2(-28f, 0f);

        // =========================================================================
        // PANEL 3: Layar Input Nama Siswa (Sesuai Tabel 3.7 No. 3)
        // =========================================================================
        GameObject panelName = CreateModalPanel("Panel_PlayerNameInput", canvasObj.transform);
        menuMgr.panelNameInput = panelName;

        GameObject cardNameBox = CreateBorderedCard("Card_NameInput", panelName.transform, spRoundedFill, ColOrangeBorder, ColCream, 7f, new Vector2(0f, -12f), new Vector2(1680f, 860f));
        CreateRibbonHeader("Ribbon_Header", cardNameBox.transform, "SIAPA NAMAMU?", spBtnYellow, spRoundedFill, ColYellowRibbon, ColDarkInk, fontDefault, new Vector2(860f, 88f), 36);

        // Banner Ucapan Bu Guru (Putih border abu tipis + avatar lingkaran hijau + teks singkat)
        GameObject guruSpeechBox = CreateBorderedCard("Speech_BuGuru", cardNameBox.transform, spRoundedFill, ColSubtleBorder, Color.white, 4f, new Vector2(0f, 195f), new Vector2(1380f, 210f));
        GameObject guruCircle = CreateUI("Avatar_Circle", guruSpeechBox.transform);
        RectTransform gcRt = guruCircle.GetComponent<RectTransform>();
        gcRt.anchorMin = gcRt.anchorMax = new Vector2(0f, 0.5f);
        gcRt.pivot = new Vector2(0f, 0.5f);
        gcRt.anchoredPosition = new Vector2(55f, 0f);
        gcRt.sizeDelta = new Vector2(136f, 136f);
        Image gcImg = guruCircle.AddComponent<Image>();
        gcImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        gcImg.color = ColGreen;

        GameObject guruAv = CreateUI("Avatar_BuGuru", guruCircle.transform);
        SetFullStretch(guruAv.GetComponent<RectTransform>(), 8f);
        Image gavImg = guruAv.AddComponent<Image>();
        gavImg.preserveAspect = true;
        if (spGuru != null) gavImg.sprite = spGuru;

        Text bubbleTxt = CreateText("Text", guruSpeechBox.transform, "Ketik nama panggilanmu ya!", 40, FontStyle.Bold, ColDarkInk, fontDefault, new Vector2(85f, 0f), new Vector2(1020f, 100f));
        bubbleTxt.alignment = TextAnchor.MiddleCenter;

        // InputField Nama (Putih border biru tebal, centered)
        GameObject inputObj = CreateBorderedCard("InputField_PlayerName", cardNameBox.transform, spRoundedFill, ColBlueBorder, Color.white, 6f, new Vector2(0f, -45f), new Vector2(1380f, 175f));
        InputField inputField = inputObj.AddComponent<InputField>();
        menuMgr.inputPlayerName = inputField;

        GameObject placeholderObj = CreateUI("Placeholder", inputObj.transform);
        SetFullStretch(placeholderObj.GetComponent<RectTransform>(), 30f);
        Text phTxt = placeholderObj.AddComponent<Text>();
        phTxt.text = "Ketik namamu di sini...";
        phTxt.font = fontDefault;
        phTxt.fontSize = 44;
        phTxt.fontStyle = FontStyle.Italic;
        phTxt.color = new Color(0.62f, 0.68f, 0.76f);
        phTxt.alignment = TextAnchor.MiddleCenter;

        GameObject textInObj = CreateUI("Text", inputObj.transform);
        SetFullStretch(textInObj.GetComponent<RectTransform>(), 30f);
        Text inTxt = textInObj.AddComponent<Text>();
        inTxt.font = fontDefault;
        inTxt.fontSize = 52;
        inTxt.fontStyle = FontStyle.Bold;
        inTxt.color = ColDarkInk;
        inTxt.alignment = TextAnchor.MiddleCenter;

        inputField.textComponent = inTxt;
        inputField.placeholder = phTxt;

        // Tombol Bawah Input Name
        Button btnBackToChar = CreateButton("Btn_BackToChar", cardNameBox.transform, "GANTI KARAKTER", spRoundedFill, spRoundedFill, new Color(0.93f, 0.95f, 0.97f), new Color(0.24f, 0.30f, 0.38f), 28, fontDefault, new Vector2(-505f, -315f), new Vector2(440f, 92f));
        UnityEventTools.AddPersistentListener(btnBackToChar.onClick, menuMgr.BackToCharacterSelect);
        CreateLeftArrowIcon(btnBackToChar.transform, new Vector2(-155f, 0f), 22f, new Color(0.24f, 0.30f, 0.38f));
        btnBackToChar.transform.Find("Text").GetComponent<RectTransform>().offsetMin = new Vector2(28f, 0f);

        Button btnStartSim = CreateButton("Btn_ConfirmStart", cardNameBox.transform, "MULAI BERMAIN", spBtnGreen, spRoundedFill, ColGreen, Color.white, 30, fontDefault, new Vector2(495f, -315f), new Vector2(480f, 92f));
        UnityEventTools.AddPersistentListener(btnStartSim.onClick, menuMgr.ConfirmNameAndStartSimulation);
        CreatePlayArrowIcon(btnStartSim.transform, new Vector2(150f, 0f), 24f, Color.white);
        btnStartSim.transform.Find("Text").GetComponent<RectTransform>().offsetMax = new Vector2(-28f, 0f);

        // =========================================================================
        // PANEL 4: Layar Pengaturan Audio (Sesuai Tabel 3.7 No. 8 - Tanpa Kamera Slider)
        // =========================================================================
        GameObject panelSet = CreateModalPanel("Panel_Settings", canvasObj.transform);
        menuMgr.panelSettings = panelSet;

        GameObject cardSetBox = CreateBorderedCard("Card_Settings", panelSet.transform, spRoundedFill, ColOrangeBorder, ColCream, 7f, new Vector2(0f, -12f), new Vector2(1480f, 860f));
        CreateRibbonHeader("Ribbon_Header", cardSetBox.transform, "PENGATURAN AUDIO & JEDA", spBtnYellow, spRoundedFill, ColYellowRibbon, ColDarkInk, fontDefault, new Vector2(840f, 88f), 34);

        Text lblBgm = CreateText("Lbl_BGM", cardSetBox.transform, "Musik Latar (BGM)", 34, FontStyle.Bold, ColDarkInk, fontDefault, new Vector2(-340f, 165f), new Vector2(560f, 60f));
        lblBgm.alignment = TextAnchor.MiddleLeft;
        Slider slBGM = CreateCapsuleSlider("Slider_BGM", cardSetBox.transform, spRoundedFill, ColGreen, new Vector2(270f, 165f), new Vector2(640f, 48f));
        menuMgr.sliderBGM = slBGM;

        Text lblSfx = CreateText("Lbl_SFX", cardSetBox.transform, "Efek Suara (SFX)", 34, FontStyle.Bold, ColDarkInk, fontDefault, new Vector2(-340f, 30f), new Vector2(560f, 60f));
        lblSfx.alignment = TextAnchor.MiddleLeft;
        Slider slSFX = CreateCapsuleSlider("Slider_SFX", cardSetBox.transform, spRoundedFill, ColGreen, new Vector2(270f, 30f), new Vector2(640f, 48f));
        menuMgr.sliderSFX = slSFX;

        Text lblVo = CreateText("Lbl_VO", cardSetBox.transform, "Suara Bu Guru (Voice Over)", 34, FontStyle.Bold, ColDarkInk, fontDefault, new Vector2(-340f, -110f), new Vector2(580f, 60f));
        lblVo.alignment = TextAnchor.MiddleLeft;
        Toggle tgVO = CreatePillToggle("Toggle_VO", cardSetBox.transform, spRoundedFill, fontDefault, new Vector2(465f, -110f), new Vector2(250f, 84f), menuMgr);
        menuMgr.toggleVoiceOver = tgVO;

        Button btnCloseSettings = CreateButton("Btn_CloseSettings", cardSetBox.transform, "SIMPAN & TUTUP", spBtnGreen, spRoundedFill, ColGreen, Color.white, 32, fontDefault, new Vector2(0f, -300f), new Vector2(580f, 96f));
        UnityEventTools.AddPersistentListener(btnCloseSettings.onClick, menuMgr.CloseSettings);

        // =========================================================================
        // PANEL 5: Popup Petunjuk (Sesuai Alur Gameplay Terbaru)
        // =========================================================================
        GameObject panelTut = CreateModalPanel("Panel_Tutorial", canvasObj.transform);
        menuMgr.panelTutorial = panelTut;

        GameObject cardTutBox = CreateBorderedCard("Card_Tutorial", panelTut.transform, spRoundedFill, ColOrangeBorder, ColCream, 7f, new Vector2(0f, -12f), new Vector2(1480f, 860f));
        CreateRibbonHeader("Ribbon_Header", cardTutBox.transform, "PANDUAN MITIGASI GEMPA", spBtnYellow, spRoundedFill, ColYellowRibbon, ColDarkInk, fontDefault, new Vector2(840f, 88f), 34);

        string tutorialMsg =
            "Halo teman-teman TK 'Aisyiyah 28!\nIkuti 4 langkah Pahlawan Siaga Gempa:\n\n" +
            "1. KENALI TANDA GEMPA (LEVEL 1)\n" +
            "   Berjalan ke zona Smart TV dan jawab 2 kuis gambar & suara gempa.\n\n" +
            "2. RUNDUK & LINDUNGI DIRI (LEVEL 2)\n" +
            "   Segera masuk kolong meja belajar lalu tekan tombol RUNDUK.\n" +
            "   Tahan posisi selama 5 detik sampai gempa mereda.\n\n" +
            "3. EVAKUASI TERTIB (LEVEL 3)\n" +
            "   Ikuti panah hijau chevron dan bintang menuju Titik Kumpul.\n\n" +
            "4. WASPADA GEMPA SUSULAN\n" +
            "   Tetap tenang saat ada getaran susulan sampai tiba di lapangan terbuka!";

        Text tutTxt = CreateText("TutorialContent", cardTutBox.transform, tutorialMsg, 26, FontStyle.Normal, ColDarkInk, fontDefault, new Vector2(0f, 20f), new Vector2(1280f, 560f));
        tutTxt.alignment = TextAnchor.MiddleLeft;
        tutTxt.lineSpacing = 1.15f;

        Button btnCloseTut = CreateButton("Btn_CloseTut", cardTutBox.transform, "SAYA PAHAM!", spBtnGreen, spRoundedFill, ColGreen, Color.white, 32, fontDefault, new Vector2(0f, -310f), new Vector2(460f, 90f));
        UnityEventTools.AddPersistentListener(btnCloseTut.onClick, menuMgr.CloseTutorial);

        // Nonaktifkan panel modal awal
        panelChar.SetActive(false);
        panelName.SetActive(false);
        panelSet.SetActive(false);
        panelTut.SetActive(false);

        EditorSceneManager.SaveScene(scene, ScenePath);
        Debug.Log($"[MainMenuBuilder] Berhasil mengupdate MainMenu sesuai UI Word di '{ScenePath}'!");

        RegisterScenesInBuildSettings();

        if (showDialog)
        {
            EditorUtility.DisplayDialog("Sukses!", "Scene MainMenu berhasil disamakan dengan rancangan High-Fidelity Word!", "Mantap!");
        }
    }

    private static void RegisterScenesInBuildSettings()
    {
        EditorBuildSettings.scenes = new EditorBuildSettingsScene[]
        {
            new EditorBuildSettingsScene(ScenePath, true),
            new EditorBuildSettingsScene(SimulationScenePath, true)
        };
    }

    #region UI Helper Methods

    private static GameObject CreateUI(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static void SetFullStretch(RectTransform rt, float inset = 0f)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(inset, inset);
        rt.offsetMax = new Vector2(-inset, -inset);
    }

    private static Image ApplySlicedImage(GameObject go, Sprite preferred, Sprite fallback, Color tint)
    {
        Image img = go.GetComponent<Image>() ?? go.AddComponent<Image>();
        Sprite s = preferred ?? fallback;
        if (s != null) { img.sprite = s; img.type = Image.Type.Sliced; }
        img.color = preferred != null ? Color.white : tint;
        return img;
    }

    private static GameObject CreateModalPanel(string name, Transform parent)
    {
        GameObject panel = CreateUI(name, parent);
        SetFullStretch(panel.GetComponent<RectTransform>());
        Image scrim = panel.AddComponent<Image>();
        scrim.color = new Color(0.04f, 0.07f, 0.17f, 0.68f);
        return panel;
    }

    private static GameObject CreateBorderedCard(string name, Transform parent, Sprite roundedFill, Color borderColor, Color fillColor, float thickness, Vector2 pos, Vector2 size)
    {
        GameObject outer = CreateUI(name, parent);
        RectTransform rt = outer.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        Image outerImg = outer.AddComponent<Image>();
        if (roundedFill != null) { outerImg.sprite = roundedFill; outerImg.type = Image.Type.Sliced; }
        outerImg.color = borderColor;

        GameObject fill = CreateUI("Card_Fill", outer.transform);
        SetFullStretch(fill.GetComponent<RectTransform>(), thickness);
        Image fillImg = fill.AddComponent<Image>();
        if (roundedFill != null) { fillImg.sprite = roundedFill; fillImg.type = Image.Type.Sliced; }
        fillImg.color = fillColor;
        fillImg.raycastTarget = false;
        return outer;
    }

    private static void CreateRibbonHeader(string name, Transform parent, string title, Sprite spYellow, Sprite spFill, Color bgColor, Color textColor, Font font, Vector2 size, int fontSize)
    {
        GameObject ribbon = CreateUI(name, parent);
        RectTransform rt = ribbon.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = size;
        ApplySlicedImage(ribbon, spYellow, spFill, bgColor);
        CreateText("Text", ribbon.transform, title, fontSize, FontStyle.Bold, textColor, font);
    }

    private static Button CreateButton(string name, Transform parent, string label, Sprite preferredSprite, Sprite fallbackSprite, Color tint, Color textColor, int fontSize, Font font, Vector2 pos, Vector2 size)
    {
        GameObject btnObj = CreateUI(name, parent);
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        Image img = btnObj.AddComponent<Image>();
        Sprite s = preferredSprite ?? fallbackSprite;
        if (s != null) { img.sprite = s; img.type = Image.Type.Sliced; }
        img.color = (preferredSprite != null && preferredSprite != fallbackSprite) ? Color.white : tint;

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = img;
        ColorBlock cb = btn.colors;
        cb.highlightedColor = new Color(0.95f, 0.95f, 0.95f);
        cb.pressedColor = new Color(0.85f, 0.85f, 0.85f);
        cb.disabledColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        btn.colors = cb;

        CreateText("Text", btnObj.transform, label, fontSize, FontStyle.Bold, textColor, font);
        return btn;
    }

    private static Text CreateText(string name, Transform parent, string content, int fontSize, FontStyle style, Color color, Font font, Vector2 pos = default, Vector2 size = default)
    {
        GameObject textObj = CreateUI(name, parent);
        RectTransform rt = textObj.GetComponent<RectTransform>();
        if (size != default) { rt.sizeDelta = size; rt.anchoredPosition = pos; }
        else SetFullStretch(rt);

        Text txt = textObj.AddComponent<Text>();
        txt.text = content;
        txt.font = font;
        txt.fontSize = fontSize;
        txt.fontStyle = style;
        txt.color = color;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.raycastTarget = false;
        return txt;
    }

    private static Slider CreateCapsuleSlider(string name, Transform parent, Sprite spFill, Color fillColor, Vector2 pos, Vector2 size)
    {
        GameObject sliderObj = CreateUI(name, parent);
        RectTransform sRt = sliderObj.GetComponent<RectTransform>();
        sRt.anchoredPosition = pos;
        sRt.sizeDelta = size;

        Slider slider = sliderObj.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0.75f;
        slider.transition = Selectable.Transition.None;

        GameObject bgTrack = CreateUI("Background", sliderObj.transform);
        SetFullStretch(bgTrack.GetComponent<RectTransform>());
        Image bgImg = bgTrack.AddComponent<Image>();
        if (spFill != null) { bgImg.sprite = spFill; bgImg.type = Image.Type.Sliced; }
        bgImg.color = ColTrackGrey;

        GameObject fillArea = CreateUI("Fill Area", sliderObj.transform);
        SetFullStretch(fillArea.GetComponent<RectTransform>());
        GameObject fill = CreateUI("Fill", fillArea.transform);
        SetFullStretch(fill.GetComponent<RectTransform>());
        Image fillImg = fill.AddComponent<Image>();
        if (spFill != null) { fillImg.sprite = spFill; fillImg.type = Image.Type.Sliced; }
        fillImg.color = fillColor;
        slider.fillRect = fill.GetComponent<RectTransform>();

        GameObject handleArea = CreateUI("Handle Slide Area", sliderObj.transform);
        SetFullStretch(handleArea.GetComponent<RectTransform>());
        GameObject handle = CreateUI("Handle", handleArea.transform);
        RectTransform hRt = handle.GetComponent<RectTransform>();
        hRt.sizeDelta = new Vector2(48f, size.y);
        Image hImg = handle.AddComponent<Image>();
        hImg.color = Color.clear;
        slider.handleRect = hRt;
        slider.targetGraphic = hImg;
        return slider;
    }

    private static Toggle CreatePillToggle(string name, Transform parent, Sprite spFill, Font font, Vector2 pos, Vector2 size, MainMenuManager mgr)
    {
        GameObject tgObj = CreateUI(name, parent);
        RectTransform tgRt = tgObj.GetComponent<RectTransform>();
        tgRt.anchoredPosition = pos;
        tgRt.sizeDelta = size;

        Toggle toggle = tgObj.AddComponent<Toggle>();
        toggle.isOn = true;
        toggle.transition = Selectable.Transition.None;

        Image bg = tgObj.AddComponent<Image>();
        if (spFill != null) { bg.sprite = spFill; bg.type = Image.Type.Sliced; }
        bg.color = ColGreen;
        toggle.targetGraphic = bg;
        mgr.toggleVoiceOverBg = bg;

        Text txt = CreateText("Text", tgObj.transform, "AKTIF", 30, FontStyle.Bold, Color.white, font);
        txt.rectTransform.offsetMax = new Vector2(-34f, 0f);
        mgr.toggleVoiceOverText = txt;

        GameObject chk = CreateCheckIcon("CheckIcon", tgObj.transform, new Vector2(62f, 0f), Color.white);
        mgr.toggleVoiceOverCheckIcon = chk;
        return toggle;
    }

    private static GameObject CreateCheckIcon(string name, Transform parent, Vector2 pos, Color col)
    {
        GameObject root = CreateUI(name, parent);
        RectTransform r = root.GetComponent<RectTransform>();
        r.anchoredPosition = pos;
        r.sizeDelta = new Vector2(30f, 30f);

        GameObject s1 = CreateUI("ArmShort", root.transform);
        RectTransform r1 = s1.GetComponent<RectTransform>();
        r1.anchoredPosition = new Vector2(-5f, -3f);
        r1.sizeDelta = new Vector2(14f, 5f);
        r1.localEulerAngles = new Vector3(0f, 0f, -45f);
        Image i1 = s1.AddComponent<Image>(); i1.color = col; i1.raycastTarget = false;

        GameObject s2 = CreateUI("ArmLong", root.transform);
        RectTransform r2 = s2.GetComponent<RectTransform>();
        r2.anchoredPosition = new Vector2(5f, 1f);
        r2.sizeDelta = new Vector2(24f, 5f);
        r2.localEulerAngles = new Vector3(0f, 0f, 45f);
        Image i2 = s2.AddComponent<Image>(); i2.color = col; i2.raycastTarget = false;
        return root;
    }

    private static void CreatePlayArrowIcon(Transform parent, Vector2 pos, float size, Color col)
    {
        CreateChevronArrow(parent, "Icon_ArrowRight", pos, size, col, 1f);
    }

    private static void CreateLeftArrowIcon(Transform parent, Vector2 pos, float size, Color col)
    {
        CreateChevronArrow(parent, "Icon_ArrowLeft", pos, size, col, -1f);
    }

    private static void CreateChevronArrow(Transform parent, string name, Vector2 pos, float size, Color col, float dir)
    {
        GameObject root = CreateUI(name, parent);
        RectTransform rt = root.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(size, size);

        float len = size * 0.68f;
        float thick = Mathf.Max(4f, size * 0.20f);
        float offset = len * 0.30f;

        GameObject top = CreateUI("LegTop", root.transform);
        RectTransform rt1 = top.GetComponent<RectTransform>();
        rt1.anchoredPosition = new Vector2(0f, offset);
        rt1.sizeDelta = new Vector2(len, thick);
        rt1.localEulerAngles = new Vector3(0f, 0f, -40f * dir);
        Image i1 = top.AddComponent<Image>(); i1.color = col; i1.raycastTarget = false;

        GameObject bot = CreateUI("LegBot", root.transform);
        RectTransform rt2 = bot.GetComponent<RectTransform>();
        rt2.anchoredPosition = new Vector2(0f, -offset);
        rt2.sizeDelta = new Vector2(len, thick);
        rt2.localEulerAngles = new Vector3(0f, 0f, 40f * dir);
        Image i2 = bot.AddComponent<Image>(); i2.color = col; i2.raycastTarget = false;
    }

    private static void CreateGearIcon(Transform parent, Color col)
    {
        Sprite knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        GameObject root = CreateUI("Icon_Gear", parent);
        RectTransform rt = root.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(56f, 56f);

        for (int i = 0; i < 4; i++)
        {
            GameObject tooth = CreateUI($"Tooth_{i}", root.transform);
            RectTransform trt = tooth.GetComponent<RectTransform>();
            trt.sizeDelta = new Vector2(10f, 52f);
            trt.localEulerAngles = new Vector3(0f, 0f, i * 45f);
            Image ti = tooth.AddComponent<Image>(); ti.color = col; ti.raycastTarget = false;
        }
        GameObject outer = CreateUI("Ring", root.transform);
        outer.GetComponent<RectTransform>().sizeDelta = new Vector2(38f, 38f);
        Image oi = outer.AddComponent<Image>(); oi.sprite = knob; oi.color = col; oi.raycastTarget = false;

        GameObject hole = CreateUI("Hole", root.transform);
        hole.GetComponent<RectTransform>().sizeDelta = new Vector2(18f, 18f);
        Image hi = hole.AddComponent<Image>(); hi.sprite = knob; hi.color = Color.white; hi.raycastTarget = false;
    }

    #endregion
}
#endif
