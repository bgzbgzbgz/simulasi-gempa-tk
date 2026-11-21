#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Tool otomatis untuk membuat dan menyusun Scene Menu Utama (MainMenu.unity) secara lengkap,
/// mengadopsi style kartun Upin & Ipin / TK Playful ramah anak:
/// 1. UI_01_MainMenu: Background kelas TK, papan tulis Siaga Gempa, tombol chunky 3D.
/// 2. UI_01B_CharacterSelect: Modal pemilihan karakter putra & putri.
/// 3. UI_01C_PlayerNameInput: Modal input nama panggilan player dengan ucapan Bu Guru.
/// 4. UI_02_SettingsPause: Modal pengaturan volume BGM, SFX, dan toggle Voice Over.
/// 5. Popup Panduan: 4 langkah mitigasi bencana gempa bumi anak TK.
/// </summary>
public static class MainMenuBuilderEditor
{
    private const string ScenePath = "Assets/Scenes/MainMenu.unity";
    private const string SimulationScenePath = "Assets/Scenes/LECVEL_NewMap.unity";

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
        // 1. Buat Scene Baru
        Scene currentActive = SceneManager.GetActiveScene();
        if (currentActive.isDirty)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        }

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 2. Kamera Utama
        GameObject camObj = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        Camera cam = camObj.GetComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.12f, 0.16f, 0.22f);
        cam.orthographic = true;
        cam.orthographicSize = 5f;

        // 3. SoundManager
        if (Object.FindFirstObjectByType<SoundManager>() == null)
        {
            new GameObject("SoundManager", typeof(SoundManager));
        }

        // 4. EventSystem
        GameObject esObj = GameObject.Find("EventSystem");
        if (esObj == null) esObj = new GameObject("EventSystem");
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
        if (inputAsset != null)
        {
            inputModule.actionsAsset = inputAsset;
        }
        inputModule.AssignDefaultActions();

        // 5. Canvas UI
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

        // Load Sprites yang diekspor
        Sprite spBg = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/bg_mainmenu.png");
        Sprite spBoy = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/avatar_boy.png");
        Sprite spGirl = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/avatar_girl.png");
        Sprite spGuru = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/avatar_guru.png");
        Sprite sprBtnRed = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/spr_ui_rounded_btn_red.png");
        Sprite sprBtnWhite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/spr_ui_rounded_btn_white.png");
        Sprite sprCardSubtle = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/spr_ui_rounded_card_subtle.png");

        // Background Kelas Utama
        GameObject bgObj = CreateUI("Background_Kelas", canvasObj.transform);
        SetFullStretch(bgObj.GetComponent<RectTransform>());
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.raycastTarget = false;
        if (spBg != null)
        {
            bgImg.sprite = spBg;
            bgImg.type = Image.Type.Simple;
            bgImg.preserveAspect = false;
        }
        else
        {
            bgImg.color = new Color(0.35f, 0.65f, 0.88f);
        }

        // Attach MainMenuManager
        MainMenuManager menuMgr = canvasObj.AddComponent<MainMenuManager>();
        menuMgr.simulationSceneName = "LECVEL_NewMap";

        // =========================================================================
        // PANEL 1: UI_01_MainMenu
        // =========================================================================
        GameObject panelMain = CreateUI("Panel_MainMenu", canvasObj.transform);
        SetFullStretch(panelMain.GetComponent<RectTransform>());
        menuMgr.panelMainMenu = panelMain;

        // Tag Atas Kiri: EDUKASI MITIGASI GEMPA
        GameObject tagTopLeft = CreateUI("Tag_EdukasiGempa", panelMain.transform);
        RectTransform tagRt = tagTopLeft.GetComponent<RectTransform>();
        tagRt.anchorMin = new Vector2(0f, 1f);
        tagRt.anchorMax = new Vector2(0f, 1f);
        tagRt.pivot = new Vector2(0f, 1f);
        tagRt.anchoredPosition = new Vector2(40f, -40f);
        tagRt.sizeDelta = new Vector2(260f, 44f);
        Image tagImg = tagTopLeft.AddComponent<Image>();
        if (sprBtnRed != null)
        {
            tagImg.sprite = sprBtnRed;
            tagImg.type = Image.Type.Sliced;
            tagImg.color = Color.white;
        }
        else
        {
            tagImg.color = new Color(0.94f, 0.27f, 0.24f); // Merah ceria
            AddOutline(tagTopLeft, new Color(0.12f, 0.16f, 0.22f), 3f);
        }
        CreateText("Text", tagTopLeft.transform, "EDUKASI MITIGASI GEMPA", 16, FontStyle.Bold, Color.white, fontDefault);

        // Tombol Pengaturan Cepat (Gear) Atas Kanan (Sesuai Ref: Kotak Putih Outline Gelap)
        GameObject btnGear = CreateUI("Btn_Gear_Settings", panelMain.transform);
        RectTransform gearRt = btnGear.GetComponent<RectTransform>();
        gearRt.anchorMin = new Vector2(1f, 1f);
        gearRt.anchorMax = new Vector2(1f, 1f);
        gearRt.pivot = new Vector2(1f, 1f);
        gearRt.anchoredPosition = new Vector2(-40f, -40f);
        gearRt.sizeDelta = new Vector2(56f, 56f);
        Image gearImg = btnGear.AddComponent<Image>();
        if (sprBtnWhite != null)
        {
            gearImg.sprite = sprBtnWhite;
            gearImg.type = Image.Type.Sliced;
            gearImg.color = Color.white;
        }
        else
        {
            gearImg.color = Color.white;
            AddOutline(btnGear, new Color(0.12f, 0.16f, 0.22f), 3f);
        }
        Button gearButton = btnGear.AddComponent<Button>();
        UnityEventTools.AddPersistentListener(gearButton.onClick, menuMgr.OpenSettings);
        CreateText("Icon", btnGear.transform, "⚙", 30, FontStyle.Bold, new Color(0.12f, 0.16f, 0.22f), fontDefault);

        // Jika spBg null, buat papan tulis kapur manual sebagai fallback
        if (spBg == null)
        {
            GameObject blackboard = CreateUI("Blackboard_Title", panelMain.transform);
            RectTransform bbRt = blackboard.GetComponent<RectTransform>();
            bbRt.anchorMin = new Vector2(0.5f, 0.5f);
            bbRt.anchorMax = new Vector2(0.5f, 0.5f);
            bbRt.anchoredPosition = new Vector2(0f, 80f);
            bbRt.sizeDelta = new Vector2(650f, 320f);
            Image bbImg = blackboard.AddComponent<Image>();
            bbImg.color = new Color(0.12f, 0.22f, 0.16f, 0.88f);
            AddOutline(blackboard, new Color(0.55f, 0.35f, 0.15f), 12f);

            CreateText("Title1", blackboard.transform, "Siaga", 80, FontStyle.Bold, new Color(1f, 0.98f, 0.92f), fontDefault, new Vector2(0f, 60f), new Vector2(600f, 100f));
            CreateText("Title2", blackboard.transform, "Gempa", 92, FontStyle.Bold, new Color(1f, 0.98f, 0.92f), fontDefault, new Vector2(0f, -45f), new Vector2(600f, 110f));
            CreateText("Sub", blackboard.transform, "★ TK 'Aisyiyah 28 Surabaya ★", 20, FontStyle.Normal, new Color(1f, 0.9f, 0.5f), fontDefault, new Vector2(0f, -125f), new Vector2(600f, 40f));
        }

        // Tombol Utama: MULAI SIMULASI (Warna Ijo Chunky, Font Putih)
        Button btnMulai = CreateChunkyButton("Btn_MulaiSimulasi", panelMain.transform, "▶  MULAI SIMULASI", new Color(0.13f, 0.77f, 0.36f), new Color(0.08f, 0.53f, 0.24f), 28, fontDefault, new Vector2(-75f, -95f), new Vector2(360f, 68f), Color.white);
        UnityEventTools.AddPersistentListener(btnMulai.onClick, menuMgr.OpenCharacterSelect);

        // Baris Tombol Menu Bawah (Pengaturan, Petunjuk, Keluar - Putih dengan Font Hitam)
        GameObject rowButtons = CreateUI("Row_Buttons", panelMain.transform);
        RectTransform rowRt = rowButtons.GetComponent<RectTransform>();
        rowRt.anchorMin = new Vector2(0.5f, 0.5f);
        rowRt.anchorMax = new Vector2(0.5f, 0.5f);
        rowRt.anchoredPosition = new Vector2(-75f, -180f);
        rowRt.sizeDelta = new Vector2(550f, 60f);

        Button btnSettings = CreateChunkyButton("Btn_Pengaturan", rowButtons.transform, "Pengaturan", Color.white, new Color(0.88f, 0.9f, 0.93f), 20, fontDefault, new Vector2(-180f, 0f), new Vector2(165f, 52f), new Color(0.12f, 0.16f, 0.22f));
        UnityEventTools.AddPersistentListener(btnSettings.onClick, menuMgr.OpenSettings);

        Button btnTutorial = CreateChunkyButton("Btn_Petunjuk", rowButtons.transform, "Petunjuk", Color.white, new Color(0.88f, 0.9f, 0.93f), 20, fontDefault, new Vector2(0f, 0f), new Vector2(150f, 52f), new Color(0.12f, 0.16f, 0.22f));
        UnityEventTools.AddPersistentListener(btnTutorial.onClick, menuMgr.OpenTutorial);

        Button btnQuit = CreateChunkyButton("Btn_Keluar", rowButtons.transform, "Keluar", Color.white, new Color(0.88f, 0.9f, 0.93f), 20, fontDefault, new Vector2(170f, 0f), new Vector2(145f, 52f), new Color(0.12f, 0.16f, 0.22f));
        UnityEventTools.AddPersistentListener(btnQuit.onClick, menuMgr.QuitGame);

        // Copyright Footer (Rapi di bawah)
        CreateText("Footer_Copyright", panelMain.transform, "© 2026 TK Aisyiyah 28 Surabaya • Tugas Akhir D4 Rekayasa Perangkat Lunak PENS", 15, FontStyle.Normal, new Color(0.95f, 0.95f, 0.95f, 0.9f), fontDefault, new Vector2(0f, -335f), new Vector2(1200f, 30f));

        // =========================================================================
        // PANEL 2: UI_01B_CharacterSelect
        // =========================================================================
        GameObject panelChar = CreateModalPanel("Panel_CharacterSelect", canvasObj.transform);
        menuMgr.panelCharacterSelect = panelChar;

        GameObject cardCharBox = CreateParchmentCard("Card_CharacterSelect", panelChar.transform, new Vector2(980f, 680f));
        CreateRibbonHeader("Ribbon_Header", cardCharBox.transform, "PILIH KARAKTER", fontDefault);
        CreateText("SubPrompt", cardCharBox.transform, "Kamu Mau Bermain Sebagai Siapa Hari Ini?", 22, FontStyle.Bold, new Color(0.12f, 0.16f, 0.22f), fontDefault, new Vector2(0f, 240f), new Vector2(900f, 40f));

        // 1. Kartu Siswa Putra
        GameObject cardBoy = CreateUI("Card_Boy", cardCharBox.transform);
        RectTransform cbRt = cardBoy.GetComponent<RectTransform>();
        cbRt.anchoredPosition = new Vector2(-220f, 40f);
        cbRt.sizeDelta = new Vector2(360f, 440f);
        Image cbBg = cardBoy.AddComponent<Image>();
        cbBg.color = Color.white;
        Outline cbOut = AddOutline(cardBoy, new Color(0.13f, 0.77f, 0.36f), 4.5f);
        menuMgr.cardBoyOutline = cbOut;
        menuMgr.cardBoyBg = cbBg;

        // Avatar Boy
        GameObject avBoyObj = CreateUI("Avatar_Boy", cardBoy.transform);
        RectTransform avbRt = avBoyObj.GetComponent<RectTransform>();
        avbRt.anchoredPosition = new Vector2(0f, 40f);
        avbRt.sizeDelta = new Vector2(210f, 210f);
        Image avbImg = avBoyObj.AddComponent<Image>();
        if (spBoy != null) avbImg.sprite = spBoy;
        else avbImg.color = new Color(0.8f, 0.9f, 1f);

        // Hapus teks Ananda/Seragam. Cukup tombol PILIH SAYA
        Button btnSelectBoy = CreateChunkyButton("Btn_SelectBoy", cardBoy.transform, "PILIH SAYA ✓", new Color(0.13f, 0.77f, 0.36f), new Color(0.08f, 0.5f, 0.24f), 20, fontDefault, new Vector2(0f, -125f), new Vector2(260f, 55f), Color.white);
        UnityEventTools.AddPersistentListener(btnSelectBoy.onClick, menuMgr.SelectBoy);
        menuMgr.btnSelectBoyImage = btnSelectBoy.GetComponent<Image>();
        menuMgr.btnSelectBoyText = btnSelectBoy.GetComponentInChildren<Text>();

        // 2. Kartu Siswa Putri
        GameObject cardGirl = CreateUI("Card_Girl", cardCharBox.transform);
        RectTransform cgRt = cardGirl.GetComponent<RectTransform>();
        cgRt.anchoredPosition = new Vector2(220f, 40f);
        cgRt.sizeDelta = new Vector2(360f, 440f);
        Image cgBg = cardGirl.AddComponent<Image>();
        cgBg.color = Color.white;
        Outline cgOut = AddOutline(cardGirl, new Color(0.12f, 0.16f, 0.22f), 3f);
        menuMgr.cardGirlOutline = cgOut;
        menuMgr.cardGirlBg = cgBg;

        // Avatar Girl
        GameObject avGirlObj = CreateUI("Avatar_Girl", cardGirl.transform);
        RectTransform avgRt = avGirlObj.GetComponent<RectTransform>();
        avgRt.anchoredPosition = new Vector2(0f, 40f);
        avgRt.sizeDelta = new Vector2(210f, 210f);
        Image avgImg = avGirlObj.AddComponent<Image>();
        if (spGirl != null) avgImg.sprite = spGirl;
        else avgImg.color = new Color(1f, 0.85f, 0.92f);

        // Hapus teks Ananda/Seragam. Cukup tombol PILIH SAYA (Putih font hitam saat unselected, HIJAU jika dipilih, bukan merah/pink)
        Button btnSelectGirl = CreateChunkyButton("Btn_SelectGirl", cardGirl.transform, "PILIH SAYA", Color.white, new Color(0.88f, 0.9f, 0.93f), 20, fontDefault, new Vector2(0f, -125f), new Vector2(260f, 55f), new Color(0.12f, 0.16f, 0.22f));
        UnityEventTools.AddPersistentListener(btnSelectGirl.onClick, menuMgr.SelectGirl);
        menuMgr.btnSelectGirlImage = btnSelectGirl.GetComponent<Image>();
        menuMgr.btnSelectGirlText = btnSelectGirl.GetComponentInChildren<Text>();

        // Notifikasi / Keterangan Karakter
        Text noticeTxt = CreateText("Notice_Character", cardCharBox.transform, "Karakter terpilih: Siswa Putra (Siap Simulasi)", 17, FontStyle.Bold, new Color(0.13f, 0.77f, 0.36f), fontDefault, new Vector2(0f, -225f), new Vector2(900f, 35f));
        menuMgr.textCharacterNotice = noticeTxt;

        // Tombol Aksi Bawah Character Select
        Button btnBackToMain = CreateChunkyButton("Btn_BackToMain", cardCharBox.transform, "◀  KEMBALI", Color.white, new Color(0.8f, 0.85f, 0.9f), 20, fontDefault, new Vector2(-220f, -280f), new Vector2(240f, 55f), new Color(0.12f, 0.16f, 0.22f));
        UnityEventTools.AddPersistentListener(btnBackToMain.onClick, menuMgr.BackToMainMenu);

        Button btnNextToName = CreateChunkyButton("Btn_NextToName", cardCharBox.transform, "MASUKKAN NAMA  ▶", new Color(0.13f, 0.77f, 0.36f), new Color(0.08f, 0.5f, 0.24f), 20, fontDefault, new Vector2(220f, -280f), new Vector2(300f, 55f));
        UnityEventTools.AddPersistentListener(btnNextToName.onClick, menuMgr.OpenNameInput);

        // =========================================================================
        // PANEL 3: UI_01C_PlayerNameInput
        // =========================================================================
        GameObject panelName = CreateModalPanel("Panel_PlayerNameInput", canvasObj.transform);
        menuMgr.panelNameInput = panelName;

        GameObject cardNameBox = CreateParchmentCard("Card_NameInput", panelName.transform, new Vector2(800f, 520f));
        CreateRibbonHeader("Ribbon_Header", cardNameBox.transform, "SIAPA NAMAMU?", fontDefault);

        // Bu Guru Speech Bubble
        GameObject guruSpeechBox = CreateUI("Speech_BuGuru", cardNameBox.transform);
        RectTransform gsbRt = guruSpeechBox.GetComponent<RectTransform>();
        gsbRt.anchoredPosition = new Vector2(0f, 130f);
        gsbRt.sizeDelta = new Vector2(680f, 90f);

        // Avatar Bu Guru
        GameObject guruAv = CreateUI("Avatar_BuGuru", guruSpeechBox.transform);
        RectTransform gavRt = guruAv.GetComponent<RectTransform>();
        gavRt.anchorMin = new Vector2(0f, 0.5f);
        gavRt.anchorMax = new Vector2(0f, 0.5f);
        gavRt.pivot = new Vector2(0f, 0.5f);
        gavRt.anchoredPosition = new Vector2(10f, 0f);
        gavRt.sizeDelta = new Vector2(80f, 80f);
        Image gavImg = guruAv.AddComponent<Image>();
        if (spGuru != null) gavImg.sprite = spGuru;
        else gavImg.color = new Color(0.85f, 0.95f, 0.85f);
        AddOutline(guruAv, new Color(0.12f, 0.16f, 0.22f), 2.5f);

        // Bubble Text Bu Guru
        GameObject bubbleObj = CreateUI("Bubble_Text", guruSpeechBox.transform);
        RectTransform bblRt = bubbleObj.GetComponent<RectTransform>();
        bblRt.anchorMin = new Vector2(0f, 0f);
        bblRt.anchorMax = new Vector2(1f, 1f);
        bblRt.offsetMin = new Vector2(105f, 5f);
        bblRt.offsetMax = new Vector2(-10f, -5f);
        Image bblImg = bubbleObj.AddComponent<Image>();
        bblImg.color = Color.white;
        AddOutline(bubbleObj, new Color(0.12f, 0.16f, 0.22f), 2.5f);
        CreateText("Text", bubbleObj.transform, "Bu Guru: \"Halo Ananda! Silakan ketik nama panggilanmu ya, agar dicatat di Sertifikat Pahlawan Gempa!\"", 18, FontStyle.Bold, new Color(0.12f, 0.16f, 0.22f), fontDefault, Vector2.zero, new Vector2(530f, 60f));

        // InputField Nama
        GameObject inputObj = CreateUI("InputField_PlayerName", cardNameBox.transform);
        RectTransform inRt = inputObj.GetComponent<RectTransform>();
        inRt.anchoredPosition = new Vector2(0f, 5f);
        inRt.sizeDelta = new Vector2(620f, 75f);
        Image inImg = inputObj.AddComponent<Image>();
        inImg.color = Color.white;
        AddOutline(inputObj, new Color(0.23f, 0.51f, 0.96f), 3.5f);

        InputField inputField = inputObj.AddComponent<InputField>();
        menuMgr.inputPlayerName = inputField;

        GameObject placeholderObj = CreateUI("Placeholder", inputObj.transform);
        SetFullStretch(placeholderObj.GetComponent<RectTransform>());
        Text phTxt = placeholderObj.AddComponent<Text>();
        phTxt.text = "Ketik nama panggilanmu di sini...";
        phTxt.font = fontDefault;
        phTxt.fontSize = 24;
        phTxt.fontStyle = FontStyle.Italic;
        phTxt.color = new Color(0.6f, 0.65f, 0.75f);
        phTxt.alignment = TextAnchor.MiddleCenter;

        GameObject textInObj = CreateUI("Text", inputObj.transform);
        SetFullStretch(textInObj.GetComponent<RectTransform>());
        Text inTxt = textInObj.AddComponent<Text>();
        inTxt.font = fontDefault;
        inTxt.fontSize = 28;
        inTxt.fontStyle = FontStyle.Bold;
        inTxt.color = new Color(0.1f, 0.15f, 0.22f);
        inTxt.alignment = TextAnchor.MiddleCenter;

        inputField.textComponent = inTxt;
        inputField.placeholder = phTxt;

        CreateText("Hint", cardNameBox.transform, "Contoh: Bagas, Aisyah, Rian • Bebas ketik apa saja", 17, FontStyle.Normal, new Color(0.4f, 0.45f, 0.55f), fontDefault, new Vector2(0f, -60f), new Vector2(600f, 35f));

        // Tombol Aksi Bawah Input Name
        Button btnBackToChar = CreateChunkyButton("Btn_BackToChar", cardNameBox.transform, "◀  KEMBALI", Color.white, new Color(0.8f, 0.85f, 0.9f), 20, fontDefault, new Vector2(-180f, -150f), new Vector2(240f, 60f), new Color(0.12f, 0.16f, 0.22f));
        UnityEventTools.AddPersistentListener(btnBackToChar.onClick, menuMgr.BackToCharacterSelect);

        Button btnStartSim = CreateChunkyButton("Btn_ConfirmStart", cardNameBox.transform, "MULAI SIMULASI  ▶", new Color(0.13f, 0.77f, 0.36f), new Color(0.08f, 0.5f, 0.24f), 22, fontDefault, new Vector2(180f, -150f), new Vector2(300f, 60f));
        UnityEventTools.AddPersistentListener(btnStartSim.onClick, menuMgr.ConfirmNameAndStartSimulation);

        // =========================================================================
        // PANEL 4: UI_02_SettingsPause
        // =========================================================================
        GameObject panelSet = CreateModalPanel("Panel_Settings", canvasObj.transform);
        menuMgr.panelSettings = panelSet;

        GameObject cardSetBox = CreateParchmentCard("Card_Settings", panelSet.transform, new Vector2(760f, 560f));
        CreateRibbonHeader("Ribbon_Header", cardSetBox.transform, "PENGATURAN", fontDefault);

        // Slider Musik Latar (BGM)
        CreateText("Lbl_BGM", cardSetBox.transform, "♫ Musik Latar (BGM)", 20, FontStyle.Bold, new Color(0.12f, 0.16f, 0.22f), fontDefault, new Vector2(-150f, 130f), new Vector2(350f, 35f));
        Slider slBGM = CreateSliderUI("Slider_BGM", cardSetBox.transform, new Vector2(30f, 90f), new Vector2(500f, 30f));
        menuMgr.sliderBGM = slBGM;
        slBGM.onValueChanged.AddListener(menuMgr.OnBGMChanged);

        // Slider Efek Suara (SFX)
        CreateText("Lbl_SFX", cardSetBox.transform, "🔔 Efek Suara (SFX)", 20, FontStyle.Bold, new Color(0.12f, 0.16f, 0.22f), fontDefault, new Vector2(-150f, 40f), new Vector2(350f, 35f));
        Slider slSFX = CreateSliderUI("Slider_SFX", cardSetBox.transform, new Vector2(30f, 0f), new Vector2(500f, 30f));
        menuMgr.sliderSFX = slSFX;
        slSFX.onValueChanged.AddListener(menuMgr.OnSFXChanged);

        // Toggle Voice Over Bu Guru
        CreateText("Lbl_VO", cardSetBox.transform, "🗣 Suara Bu Guru (Voice Over)", 20, FontStyle.Bold, new Color(0.12f, 0.16f, 0.22f), fontDefault, new Vector2(-120f, -50f), new Vector2(400f, 35f));
        Toggle tgVO = CreateToggleUI("Toggle_VO", cardSetBox.transform, new Vector2(230f, -50f));
        menuMgr.toggleVoiceOver = tgVO;

        Button btnCloseSettings = CreateChunkyButton("Btn_CloseSettings", cardSetBox.transform, "SIMPAN & TUTUP", new Color(0.13f, 0.77f, 0.36f), new Color(0.08f, 0.5f, 0.24f), 22, fontDefault, new Vector2(0f, -170f), new Vector2(340f, 60f));
        UnityEventTools.AddPersistentListener(btnCloseSettings.onClick, menuMgr.CloseSettings);

        // =========================================================================
        // PANEL 5: Popup Tutorial
        // =========================================================================
        GameObject panelTut = CreateModalPanel("Panel_Tutorial", canvasObj.transform);
        menuMgr.panelTutorial = panelTut;

        GameObject cardTutBox = CreateParchmentCard("Card_Tutorial", panelTut.transform, new Vector2(880f, 620f));
        CreateRibbonHeader("Ribbon_Header", cardTutBox.transform, "PANDUAN MITIGASI GEMPA", fontDefault);

        string tutorialMsg =
            "Halo teman-teman TK 'Aisyiyah 28!\nIngat 4 langkah pahlawan cilik saat gempa:\n\n" +
            "1. TANGGAP GEMPA\n" +
            "   Saat alarm berbunyi atau bumi bergetar, jangan panik dan tetap tenang!\n\n" +
            "2. DROP & COVER (RUNDUK & LINDUNGI DIRI)\n" +
            "   Segera lari ke kolong meja terdekat, lalu tekan tombol RUNDUK.\n" +
            "   Tahan sampai guncangan gempa bumi berhenti.\n\n" +
            "3. LINDUNGI KEPALA DENGAN TAS SEKOLAH\n" +
            "   Pegang tas atau benda empuk di atas kepala untuk menghindari runtuhan.\n\n" +
            "4. EVAKUASI TERTIB KE TITIK KUMPUL\n" +
            "   Ikuti garis jalur evakuasi hijau menuju lapangan terbuka bersama Bu Guru!";

        CreateText("TutorialContent", cardTutBox.transform, tutorialMsg, 18, FontStyle.Normal, new Color(0.12f, 0.16f, 0.22f), fontDefault, new Vector2(0f, 15f), new Vector2(780f, 380f));

        Button btnCloseTut = CreateChunkyButton("Btn_CloseTut", cardTutBox.transform, "SAYA PAHAM! ✓", new Color(0.13f, 0.77f, 0.36f), new Color(0.08f, 0.5f, 0.24f), 22, fontDefault, new Vector2(0f, -220f), new Vector2(300f, 55f));
        UnityEventTools.AddPersistentListener(btnCloseTut.onClick, menuMgr.CloseTutorial);

        // Nonaktifkan panel modal awal
        panelChar.SetActive(false);
        panelName.SetActive(false);
        panelSet.SetActive(false);
        panelTut.SetActive(false);

        // Simpan Scene
        EditorSceneManager.SaveScene(scene, ScenePath);
        Debug.Log($"[MainMenuBuilder] Berhasil mengupdate MainMenu di '{ScenePath}'!");

        // Update Build Settings (Index 0: MainMenu, Index 1: LECVEL_NewMap)
        RegisterScenesInBuildSettings();

        if (showDialog)
        {
            EditorUtility.DisplayDialog("Sukses!", "Scene MainMenu berhasil dibangun dengan style Upin-Ipin TK Playful!\n\nAlur UI:\n1. Main Menu (Papan Tulis & Tombol Upin-Ipin)\n2. Pilih Karakter (Putra & Putri)\n3. Input Nama Pemain (Bu Guru Speech)\n4. Pengaturan Audio & Voice Over\n5. Masuk ke Simulasi 'LECVEL_NewMap'", "Mantap!");
        }
    }

    private static void RegisterScenesInBuildSettings()
    {
        EditorBuildSettingsScene[] scenes = new EditorBuildSettingsScene[]
        {
            new EditorBuildSettingsScene(ScenePath, true),
            new EditorBuildSettingsScene(SimulationScenePath, true)
        };
        EditorBuildSettings.scenes = scenes;
    }

    #region UI Helper Methods

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
        scrim.color = new Color(0.04f, 0.07f, 0.17f, 0.65f); // Dark scrim
        return panel;
    }

    private static GameObject CreateParchmentCard(string name, Transform parent, Vector2 size)
    {
        GameObject card = CreateUI(name, parent);
        RectTransform rt = card.GetComponent<RectTransform>();
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = size;

        Sprite spr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/spr_ui_rounded_card_parchment.png");
        Image img = card.AddComponent<Image>();
        if (spr != null)
        {
            img.sprite = spr;
            img.type = Image.Type.Sliced;
            img.color = Color.white;
        }
        else
        {
            img.color = new Color(1f, 0.984f, 0.922f); // Cream #FFFBEB
            AddOutline(card, new Color(0.12f, 0.16f, 0.22f), 4f); // 4px dark stroke
        }
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
        rt.sizeDelta = new Vector2(420f, 55f);

        Sprite spr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/spr_ui_rounded_btn_yellow.png");
        Image img = ribbon.AddComponent<Image>();
        if (spr != null)
        {
            img.sprite = spr;
            img.type = Image.Type.Sliced;
            img.color = Color.white;
        }
        else
        {
            img.color = new Color(0.98f, 0.75f, 0.14f); // Yellow ribbon #FBBF24
            AddOutline(ribbon, new Color(0.12f, 0.16f, 0.22f), 3.5f);
        }

        CreateText("Text", ribbon.transform, title, 22, FontStyle.Bold, new Color(0.12f, 0.16f, 0.22f), font, Vector2.zero, new Vector2(400f, 45f));
    }

    private static Button CreateChunkyButton(string name, Transform parent, string label, Color topColor, Color bevelColor, int fontSize, Font font, Vector2 pos, Vector2 size, Color? textColor = null)
    {
        GameObject btnObj = CreateUI(name, parent);
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        Sprite spr = null;
        if (topColor == Color.white)
            spr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/spr_ui_rounded_btn_white.png");
        else
            spr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/spr_ui_rounded_btn_green.png");

        Image img = btnObj.AddComponent<Image>();
        if (spr != null)
        {
            img.sprite = spr;
            img.type = Image.Type.Sliced;
            img.color = Color.white;
        }
        else
        {
            img.color = topColor;
            img.raycastTarget = true;
            AddOutline(btnObj, new Color(0.12f, 0.16f, 0.22f), 3.5f);
        }

        Button btn = btnObj.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.highlightedColor = new Color(0.95f, 0.95f, 0.95f);
        cb.pressedColor = new Color(0.85f, 0.85f, 0.85f);
        btn.colors = cb;

        GameObject txtObj = CreateUI("Text", btnObj.transform);
        SetFullStretch(txtObj.GetComponent<RectTransform>());
        Text txt = txtObj.AddComponent<Text>();
        txt.text = label;
        txt.font = font;
        txt.fontSize = fontSize;
        txt.fontStyle = FontStyle.Bold;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = textColor ?? Color.white;
        txt.raycastTarget = false;

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
        txt.raycastTarget = false;
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

        // Background track
        GameObject bgTrack = CreateUI("Background", sliderObj.transform);
        SetFullStretch(bgTrack.GetComponent<RectTransform>());
        Image bgImg = bgTrack.AddComponent<Image>();
        bgImg.color = new Color(0.85f, 0.88f, 0.92f);
        AddOutline(bgTrack, new Color(0.12f, 0.16f, 0.22f), 2f);

        // Fill area
        GameObject fillArea = CreateUI("Fill Area", sliderObj.transform);
        SetFullStretch(fillArea.GetComponent<RectTransform>());
        GameObject fill = CreateUI("Fill", fillArea.transform);
        SetFullStretch(fill.GetComponent<RectTransform>());
        Image fillImg = fill.AddComponent<Image>();
        fillImg.color = new Color(0.23f, 0.51f, 0.96f);
        slider.fillRect = fill.GetComponent<RectTransform>();

        // Handle
        GameObject handleArea = CreateUI("Handle Slide Area", sliderObj.transform);
        SetFullStretch(handleArea.GetComponent<RectTransform>());
        GameObject handle = CreateUI("Handle", handleArea.transform);
        RectTransform hRt = handle.GetComponent<RectTransform>();
        hRt.sizeDelta = new Vector2(30f, 40f);
        Image hImg = handle.AddComponent<Image>();
        hImg.color = Color.white;
        AddOutline(handle, new Color(0.12f, 0.16f, 0.22f), 2f);
        slider.handleRect = hRt;

        return slider;
    }

    private static Toggle CreateToggleUI(string name, Transform parent, Vector2 pos)
    {
        GameObject tgObj = CreateUI(name, parent);
        RectTransform tgRt = tgObj.GetComponent<RectTransform>();
        tgRt.anchoredPosition = pos;
        tgRt.sizeDelta = new Vector2(60f, 35f);

        Toggle toggle = tgObj.AddComponent<Toggle>();
        toggle.isOn = true;

        Image bg = tgObj.AddComponent<Image>();
        bg.color = new Color(0.13f, 0.77f, 0.36f); // Green on
        AddOutline(tgObj, new Color(0.12f, 0.16f, 0.22f), 2f);
        toggle.targetGraphic = bg;

        GameObject check = CreateUI("Checkmark", tgObj.transform);
        RectTransform cRt = check.GetComponent<RectTransform>();
        cRt.anchoredPosition = new Vector2(12f, 0f);
        cRt.sizeDelta = new Vector2(25f, 25f);
        Image cImg = check.AddComponent<Image>();
        cImg.color = Color.white;
        toggle.graphic = cImg;

        return toggle;
    }

    #endregion
}
#endif
