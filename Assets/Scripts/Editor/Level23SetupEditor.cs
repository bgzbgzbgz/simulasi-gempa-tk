#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tool Editor untuk memasang objektif Level 2 (berlindung di kolong meja + efek gempa)
/// dan Level 3 (titik kumpul + garis jalur evakuasi) di scene aktif, misalnya LECVEL.
/// </summary>
public static class Level23SetupEditor
{
    private const string MaterialFolder = "Assets/Materials";

    // ==========================================
    // LEVEL 1: SETUP KUIS TV (ANIMASI GIF & SUARA GEMPA)
    // ==========================================
    [InitializeOnLoadMethod]
    private static void AutoSyncOnLoad()
    {
        EditorApplication.delayCall += () =>
        {
            if (Application.isPlaying) return;
            string activeScene = EditorSceneManager.GetActiveScene().name;
            if (string.IsNullOrEmpty(activeScene) || !activeScene.Contains("LECVEL")) return;
            SetupQuizTV();
            RestyleQuizUIToFigcraft();
            SetupPlayerNameBillboard();
            SetupThirdPersonCamera();
            SetupEarthquakeEffect(false);
            SetupPauseCameraSlider();
            SetupHUDPauseButton();
            RestyleHoldProgressAndBuGuruHUD();
            RestyleLevelCompletePopup();
            RestyleFinalScorePopup();
            SetupChevronArrowsAndStars(false);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[Level23SetupEditor] Auto-setup Kuis TV, Player Name, Kamera, Efek Gempa, Pause HUD, & Word UI Popups berhasil!");
        };
    }

    [MenuItem("Tools/Simulasi Gempa/Test Buka Kuis Sekarang")]
    public static void TestOpenQuizNow()
    {
        Level1QuizManager qm = Object.FindFirstObjectByType<Level1QuizManager>(FindObjectsInactive.Include);
        if (qm != null)
        {
            qm.StartQuiz();
            Debug.Log("[Test] StartQuiz berhasil dipanggil!");
        }
    }

    [MenuItem("Tools/Simulasi Gempa/Test: Langsung Mainkan Level 3 (Evakuasi & Bintang)")]
    public static void TestPlayLevel3Directly()
    {
        if (!Application.isPlaying)
        {
            EditorApplication.isPlaying = true;
        }

        EditorApplication.delayCall += () =>
        {
            GameManager gm = Object.FindFirstObjectByType<GameManager>();
            if (gm != null)
            {
                gm.isGameActive = true;
                gm.currentLevel = 3;
                gm.hasPickedUpBag = true;
                if (gm.missionUI != null)
                {
                    gm.missionUI.SetInstruction("Level 3: Ayo ikuti panah hijau chevron & kumpulkan bintang!");
                }
            }

            BoyController boy = Object.FindFirstObjectByType<BoyController>();
            if (boy != null)
            {
                var cc = boy.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;
                boy.transform.position = new Vector3(0.40f, 0.15f, -9.95f);
                boy.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                if (cc != null) cc.enabled = true;
            }

            EvacuationGuide guide = Object.FindFirstObjectByType<EvacuationGuide>();
            if (guide != null)
            {
                guide.AutoFindStarsIfEmpty();
            }

            Debug.Log("[Test] Berhasil melompat langsung ke Level 3 dengan panah hijau chevron dan bintang aktif!");
        };
    }

    [MenuItem("Tools/Simulasi Gempa/Player: Pasang Name Billboard di Atas Kepala")]
    public static void SetupPlayerNameBillboard()
    {
        // 1. Cari karakter player di scene (boy atau karakter ber-BoyController)
        GameObject playerObj = GameObject.Find("boy");
        if (playerObj == null)
        {
            BoyController bc = Object.FindFirstObjectByType<BoyController>(FindObjectsInactive.Include);
            if (bc != null) playerObj = bc.gameObject;
        }

        if (playerObj == null)
        {
            Debug.LogWarning("[Level23SetupEditor] Karakter player ('boy') tidak ditemukan di scene saat ini.");
            return;
        }

        Undo.RecordObject(playerObj, "Setup Player Name Billboard");

        // 2. Cari tulang kepala
        Transform headTransform = null;
        Animator anim = playerObj.GetComponent<Animator>();
        if (anim != null && anim.isHuman)
        {
            headTransform = anim.GetBoneTransform(HumanBodyBones.Head);
        }
        if (headTransform == null)
        {
            foreach (Transform t in playerObj.GetComponentsInChildren<Transform>(true))
            {
                if (t.name.ToLower().Contains("head") && !t.name.ToLower().Contains("head_end"))
                {
                    headTransform = t;
                    break;
                }
            }
        }

        // 3. Buat ulang NamePlate_Billboard agar selalu fresh dengan parameter terbaru
        Transform tNamePlate = playerObj.transform.Find("NamePlate_Billboard");
        if (tNamePlate != null)
        {
            Undo.DestroyObjectImmediate(tNamePlate.gameObject);
        }

        GameObject namePlateObj = new GameObject("NamePlate_Billboard", typeof(RectTransform), typeof(Canvas));
        namePlateObj.transform.SetParent(playerObj.transform, false);
        Undo.RecordObject(namePlateObj, "Setup NamePlate Object");

        // Setup Canvas World Space
        Canvas canvas = namePlateObj.GetComponent<Canvas>() ?? namePlateObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 10;

        RectTransform rtCanvas = namePlateObj.GetComponent<RectTransform>();
        rtCanvas.sizeDelta = new Vector2(180f, 38f);
        rtCanvas.localScale = Vector3.one * 0.0035f;
        rtCanvas.localPosition = new Vector3(0f, 1.25f, 0f); // Default preview pos

        // Badge Background
        Transform tBadge = namePlateObj.transform.Find("Badge_Background");
        GameObject badgeObj = tBadge != null ? tBadge.gameObject : new GameObject("Badge_Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
        if (tBadge == null) badgeObj.transform.SetParent(namePlateObj.transform, false);

        RectTransform rtBadge = badgeObj.GetComponent<RectTransform>();
        rtBadge.anchorMin = Vector2.zero;
        rtBadge.anchorMax = Vector2.one;
        rtBadge.offsetMin = Vector2.zero;
        rtBadge.offsetMax = Vector2.zero;

        Material zAlwaysMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_NameBillboard_ZAlways.mat");
        if (zAlwaysMat == null)
        {
            var shader = Shader.Find("UI/Default");
            zAlwaysMat = new Material(shader);
            zAlwaysMat.name = "Mat_NameBillboard_ZAlways";
            zAlwaysMat.SetInt("unity_GUIZTestMode", (int)UnityEngine.Rendering.CompareFunction.Always);
            AssetDatabase.CreateAsset(zAlwaysMat, "Assets/Materials/Mat_NameBillboard_ZAlways.mat");
        }

        Sprite sprBadge = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/spr_ui_rounded_btn_green.png");
        Image imgBadge = badgeObj.GetComponent<Image>() ?? badgeObj.AddComponent<Image>();
        imgBadge.material = zAlwaysMat;
        if (sprBadge != null)
        {
            imgBadge.sprite = sprBadge;
            imgBadge.type = Image.Type.Sliced;
            imgBadge.color = Color.white;
            var oldOl = badgeObj.GetComponent<Outline>();
            if (oldOl != null) Object.DestroyImmediate(oldOl);
        }
        else
        {
            imgBadge.color = new Color(0.06f, 0.09f, 0.16f, 0.88f); // Dark translucent badge
            Outline outBadge = badgeObj.GetComponent<Outline>() ?? badgeObj.AddComponent<Outline>();
            outBadge.effectColor = new Color(0.13f, 0.77f, 0.37f, 1f); // Emerald green outline
            outBadge.effectDistance = new Vector2(1.5f, -1.5f);
        }

        // Text Player Name
        Font defaultFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Fredoka-Variable.ttf")
            ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") 
            ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

        Transform tText = badgeObj.transform.Find("Text_PlayerName");
        GameObject textObj = tText != null ? tText.gameObject : new GameObject("Text_PlayerName", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        if (tText == null) textObj.transform.SetParent(badgeObj.transform, false);

        RectTransform rtText = textObj.GetComponent<RectTransform>();
        rtText.anchorMin = Vector2.zero;
        rtText.anchorMax = Vector2.one;
        rtText.offsetMin = new Vector2(8f, 0f);
        rtText.offsetMax = new Vector2(-8f, 0f);

        Text txt = textObj.GetComponent<Text>() ?? textObj.AddComponent<Text>();
        txt.font = defaultFont;
        txt.fontSize = 20;
        txt.fontStyle = FontStyle.Bold;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        txt.material = zAlwaysMat;
        txt.horizontalOverflow = HorizontalWrapMode.Overflow;
        txt.text = "Ananda";

        // Setup PlayerNameBillboard script
        PlayerNameBillboard billboard = namePlateObj.GetComponent<PlayerNameBillboard>() ?? namePlateObj.AddComponent<PlayerNameBillboard>();
        billboard.targetTransform = headTransform != null ? headTransform : playerObj.transform;
        billboard.offset = headTransform != null ? new Vector3(0f, 0.40f, 0f) : new Vector3(0f, 1.25f, 0f);
        billboard.nameText = txt;
        billboard.defaultName = "Ananda";
        billboard.includeGenderIcon = false;
        billboard.RefreshPlayerName();

        EditorUtility.SetDirty(namePlateObj);
        EditorUtility.SetDirty(billboard);

        if (!Application.isPlaying)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
        }

        Debug.Log($"[Level23SetupEditor] Player Name Billboard berhasil dipasang di atas kepala '{playerObj.name}'!");
    }

    [MenuItem("Tools/Simulasi Gempa/Kamera: Setup Sudut & Jarak Third Person")]
    public static void SetupThirdPersonCamera()
    {
        Camera cam = Camera.main ?? Object.FindFirstObjectByType<Camera>();
        if (cam == null)
        {
            Debug.LogWarning("[Level23SetupEditor] Main Camera tidak ditemukan di scene saat ini.");
            return;
        }

        ThirdPersonCamera tpc = cam.GetComponent<ThirdPersonCamera>();
        if (tpc == null)
        {
            Debug.LogWarning("[Level23SetupEditor] Komponen ThirdPersonCamera tidak ditemukan pada Main Camera.");
            return;
        }

        SerializedObject so = new SerializedObject(tpc);
        so.Update();

        so.FindProperty("targetHeight").floatValue = 0.85f;
        so.FindProperty("distance").floatValue = 2.6f;
        so.FindProperty("startPitch").floatValue = 14f;
        so.FindProperty("minPitch").floatValue = -15f;
        so.FindProperty("maxPitch").floatValue = 45f;
        so.FindProperty("cameraHeightOffset").floatValue = 0.35f;

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(tpc);
        if (!Application.isPlaying)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
        }

        Debug.Log("[Level23SetupEditor] ThirdPersonCamera berhasil diatur ke jarak 2.6m, sudut pandang atas-belakang 14°!");
    }

    [MenuItem("Tools/Simulasi Gempa/Level 1: Setup Kuis TV")]
    public static void SetupQuizTVMenu()
    {
        SetupQuizTV(false);
    }

    public static void SetupQuizTV(bool showDialog = false)
    {
        // 1. Bersihkan diorama legacy jika ada
        GameObject diorama = GameObject.Find("Diorama_Soal1_Rumah");
        if (diorama != null)
        {
            Undo.DestroyObjectImmediate(diorama);
        }

        // 2. Cari objek TV di denah kelas
        GameObject tvObj = GameObject.Find("Smart_TV_Level1");
        if (tvObj == null) tvObj = GameObject.Find("denah_tes/Smart_TV_Level1");

        Vector3 tvPos = new Vector3(0.38f, 1.78f, -3.47f);
        if (tvObj != null)
        {
            tvPos = tvObj.transform.position;
            Selection.activeGameObject = tvObj;

            // Bersihkan collider langsung pada TV agar tidak membengkak karena scale FBX
            BoxCollider tvCol = tvObj.GetComponent<BoxCollider>();
            if (tvCol != null) Object.DestroyImmediate(tvCol);
            CheckpointTrigger cp = tvObj.GetComponent<CheckpointTrigger>();
            if (cp != null) Object.DestroyImmediate(cp);
        }

        // 3. Posisikan Trigger & Lingkaran di lantai tepat di depan TV
        Vector3 floorPos = new Vector3(tvPos.x, 0.14f, tvPos.z + 0.9f); // z = -2.57 tepat di lantai depan TV
        GameObject triggerTV = GameObject.Find("Trigger_Mulai_Simulasi");
        if (triggerTV != null)
        {
            Undo.RecordObject(triggerTV.transform, "Move TV Trigger");
            triggerTV.transform.position = floorPos;
            triggerTV.SetActive(true);

            BoxCollider bc = triggerTV.GetComponent<BoxCollider>();
            if (bc != null)
            {
                bc.size = new Vector3(3f, 2.5f, 3f);
                bc.center = new Vector3(0f, 1f, 0f);
            }
        }

        // 4. Hubungkan Level1QuizManager
        ConfigureQuizSprites();
        RestyleQuizUIToFigcraft();
        Level1QuizManager quizManager = Object.FindFirstObjectByType<Level1QuizManager>(FindObjectsInactive.Include);
        if (quizManager != null)
        {
            Undo.RecordObject(quizManager, "Setup Kuis TV");
            quizManager.gameObject.SetActive(true);

            if (quizManager.optionButtons != null && quizManager.optionButtons.Length == 3)
            {
                quizManager.optionImages = new Image[3];
                for (int i = 0; i < 3; i++)
                {
                    if (quizManager.optionButtons[i] != null)
                    {
                        Transform tDisp = quizManager.optionButtons[i].transform.Find("Image_Display");
                        quizManager.optionImages[i] = tDisp != null ? tDisp.GetComponent<Image>() : quizManager.optionButtons[i].GetComponent<Image>();
                    }
                }
            }

            quizManager.SetupDefaultQuestions();
            quizManager.AutoAssignAssetsInEditor();

            if (quizManager.quizPanel != null)
            {
                quizManager.quizPanel.SetActive(false);
            }
            EditorUtility.SetDirty(quizManager);
        }

        // 5. Pastikan GameManager mengaktifkan alur kuis TV
        GameManager gm = Object.FindFirstObjectByType<GameManager>(FindObjectsInactive.Include);
        if (gm != null)
        {
            Undo.RecordObject(gm, "Enable Quiz Flow");
            gm.enableLevel1Quiz = true;
            gm.waitForStartTrigger = true;
            gm.waitingInstruction = "Level 1: Ayo berjalan ke lingkaran zona TV untuk mulai belajar tanda gempa bumi!";
            EditorUtility.SetDirty(gm);
        }

        
        if (!Application.isPlaying)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
        }

        if (showDialog)
        {
            EditorUtility.DisplayDialog("Setup Kuis TV Selesai!",
                $"Zona TV berhasil disetup tepat di depan TV ({tvPos.x:F2}, {tvPos.y:F2}, {tvPos.z:F2}):\n\n" +
                "• Lingkaran penanda berada di lantai tepat di depan TV.\n" +
                "• TV fisik juga memiliki sensor trigger.\n" +
                "• Soal 1: Animasi Rumah (Api, Gempa, Banjir) dari Blender (GIF).\n" +
                "• Soal 2: Suara Gempa (Sirine, Gemuruh Gempa, Air Banjir).\n\n" +
                "Silakan tekan Play ▶️ dan jalan ke depan TV!", "Mantap!");
        }
    }

    public static void ConfigureQuizSprites()
    {
        string[] folders = new string[] {
            "Assets/Sprites/Quiz",
            "Assets/Sprites/Quiz/Fire",
            "Assets/Sprites/Quiz/Quake",
            "Assets/Sprites/Quiz/Flood",
            "Assets/Sprites/UI"
        };
        bool changed = false;
        try
        {
            AssetDatabase.StartAssetEditing();
            foreach (string folder in folders)
            {
                if (!System.IO.Directory.Exists(folder)) continue;
                string[] files = System.IO.Directory.GetFiles(folder, "*.png");
                foreach (string file in files)
                {
                    string assetPath = file.Replace('\\', '/');
                    TextureImporter ti = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                    if (ti != null && ti.textureType != TextureImporterType.Sprite)
                    {
                        ti.textureType = TextureImporterType.Sprite;
                        ti.spriteImportMode = SpriteImportMode.Single;
                        ti.mipmapEnabled = false;
                        ti.SaveAndReimport();
                        changed = true;
                    }
                }
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        if (changed)
        {
            AssetDatabase.Refresh();
        }
    }

    [MenuItem("Tools/Simulasi Gempa/Restyle Quiz UI to Figcraft")]
    public static void RestyleQuizUIToFigcraft()
    {
        Level1QuizManager quizManager = Object.FindFirstObjectByType<Level1QuizManager>(FindObjectsInactive.Include);
        GameObject panelQuiz = quizManager != null ? quizManager.gameObject : null;
        if (panelQuiz == null)
        {
            MissionUI mui = Object.FindFirstObjectByType<MissionUI>(FindObjectsInactive.Include);
            if (mui != null)
            {
                Transform t = mui.transform.Find("Panel_Level1_Quiz");
                if (t != null) panelQuiz = t.gameObject;
            }
        }
        if (panelQuiz == null)
        {
            Debug.LogError("[Level23SetupEditor] Panel_Level1_Quiz tidak ditemukan di scene!");
            return;
        }

        Undo.RecordObject(panelQuiz, "Restyle Quiz UI to Word High-Fidelity");
        panelQuiz.SetActive(true);

        Font defaultFont = GetProjectFont();
        Sprite spRoundedFill = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/spr_ui_rounded_fill.png");
        Sprite spKnob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

        Color colDarkInk = new Color(0.09f, 0.13f, 0.24f);
        Color colCream = new Color(1.00f, 0.984f, 0.922f);
        Color colBorderSubtle = new Color(0.80f, 0.84f, 0.89f);
        Color colGreen = new Color(0.13f, 0.77f, 0.36f);
        Color colBlue = new Color(0.01f, 0.52f, 0.78f);

        RectTransform rtPanel = panelQuiz.GetComponent<RectTransform>();
        if (rtPanel != null) SetFullStretch(rtPanel);

        Image panelImg = panelQuiz.GetComponent<Image>() ?? panelQuiz.AddComponent<Image>();
        panelImg.color = new Color(0.06f, 0.09f, 0.17f, 0.72f);

        // -------------------------------------------------------------
        // 1. BU GURU CARD (KIRI)
        // -------------------------------------------------------------
        Transform tGuru = panelQuiz.transform.Find("Card_BuGuru");
        GameObject cardGuru = tGuru != null ? tGuru.gameObject : new GameObject("Card_BuGuru", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        if (tGuru == null)
        {
            cardGuru.transform.SetParent(panelQuiz.transform, false);
            cardGuru.transform.SetSiblingIndex(0);
        }
        Undo.RecordObject(cardGuru, "Setup Card Bu Guru");

        RectTransform rtGuru = cardGuru.GetComponent<RectTransform>();
        rtGuru.anchorMin = rtGuru.anchorMax = new Vector2(0f, 0.5f);
        rtGuru.pivot = new Vector2(0f, 0.5f);
        rtGuru.anchoredPosition = new Vector2(45f, 55f);
        rtGuru.sizeDelta = new Vector2(240f, 360f);

        ApplyBorderedCard(cardGuru, spRoundedFill, new Color(0.25f, 0.30f, 0.38f), Color.white, 5f);

        Transform tCircle = cardGuru.transform.Find("Avatar_Circle");
        GameObject objCircle = tCircle != null ? tCircle.gameObject : new GameObject("Avatar_Circle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        if (tCircle == null) objCircle.transform.SetParent(cardGuru.transform, false);
        RectTransform rtCircle = objCircle.GetComponent<RectTransform>();
        rtCircle.anchorMin = rtCircle.anchorMax = new Vector2(0.5f, 0.5f);
        rtCircle.anchoredPosition = new Vector2(0f, 58f);
        rtCircle.sizeDelta = new Vector2(146f, 146f);
        Image imgCircle = objCircle.GetComponent<Image>();
        if (spKnob != null) imgCircle.sprite = spKnob;
        imgCircle.color = colGreen;

        Transform tAva = objCircle.transform.Find("Image_Avatar");
        GameObject objAva = tAva != null ? tAva.gameObject : new GameObject("Image_Avatar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        if (tAva == null) objAva.transform.SetParent(objCircle.transform, false);
        SetFullStretch(objAva.GetComponent<RectTransform>(), 8f);
        Image imgAva = objAva.GetComponent<Image>();
        imgAva.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/avatar_guru.png");
        imgAva.color = Color.white;
        imgAva.preserveAspect = true;

        Transform tName = cardGuru.transform.Find("Text_Name");
        GameObject objName = tName != null ? tName.gameObject : new GameObject("Text_Name", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        if (tName == null) objName.transform.SetParent(cardGuru.transform, false);
        RectTransform rtName = objName.GetComponent<RectTransform>();
        rtName.anchorMin = rtName.anchorMax = new Vector2(0.5f, 0.5f);
        rtName.anchoredPosition = new Vector2(0f, -52f);
        rtName.sizeDelta = new Vector2(210f, 40f);
        Text txtName = objName.GetComponent<Text>();
        txtName.text = "Bu Guru";
        txtName.font = defaultFont;
        txtName.fontSize = 28;
        txtName.fontStyle = FontStyle.Bold;
        txtName.alignment = TextAnchor.MiddleCenter;
        txtName.color = colDarkInk;

        // Speaker graphic (replaces broken Unicode emoji)
        Transform tSpeaker = cardGuru.transform.Find("Icon_Speaker");
        GameObject objSpeaker = tSpeaker != null ? tSpeaker.gameObject : new GameObject("Icon_Speaker", typeof(RectTransform));
        if (tSpeaker == null) objSpeaker.transform.SetParent(cardGuru.transform, false);
        Text legacySpeakerTxt = objSpeaker.GetComponent<Text>();
        if (legacySpeakerTxt != null) Object.DestroyImmediate(legacySpeakerTxt);
        CanvasRenderer legacySpeakerCr = objSpeaker.GetComponent<CanvasRenderer>();
        if (legacySpeakerCr != null && objSpeaker.GetComponent<Image>() == null) Object.DestroyImmediate(legacySpeakerCr);
        RectTransform rtSpeaker = objSpeaker.GetComponent<RectTransform>();
        rtSpeaker.anchorMin = rtSpeaker.anchorMax = new Vector2(0.5f, 0.5f);
        rtSpeaker.anchoredPosition = new Vector2(0f, -108f);
        rtSpeaker.sizeDelta = new Vector2(46f, 34f);
        BuildSpeakerShape(objSpeaker.transform, new Color(0.50f, 0.58f, 0.68f));

        // -------------------------------------------------------------
        // 2. CARD QUIZ (BUKU TULIS KREM SESUAI TABEL 3.7 NO. 4)
        // -------------------------------------------------------------
        Transform tQuiz = panelQuiz.transform.Find("Card_Quiz");
        GameObject cardQuiz = tQuiz != null ? tQuiz.gameObject : new GameObject("Card_Quiz", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        if (tQuiz == null) cardQuiz.transform.SetParent(panelQuiz.transform, false);
        Undo.RecordObject(cardQuiz, "Setup Card Quiz Notebook");

        RectTransform rtQuiz = cardQuiz.GetComponent<RectTransform>();
        rtQuiz.anchorMin = rtQuiz.anchorMax = new Vector2(0.5f, 0.5f);
        rtQuiz.pivot = new Vector2(0.5f, 0.5f);
        rtQuiz.anchoredPosition = new Vector2(145f, 0f);
        rtQuiz.sizeDelta = new Vector2(1460f, 900f);

        ApplyBorderedCard(cardQuiz, spRoundedFill, colBorderSubtle, colCream, 5f);

        // 6 Binder Holes
        Transform tHoles = cardQuiz.transform.Find("Binder_Holes");
        GameObject objHoles = tHoles != null ? tHoles.gameObject : new GameObject("Binder_Holes", typeof(RectTransform));
        if (tHoles == null) objHoles.transform.SetParent(cardQuiz.transform, false);
        SetFullStretch(objHoles.GetComponent<RectTransform>());
        objHoles.transform.SetSiblingIndex(1);

        for (int i = 0; i < 6; i++)
        {
            Transform tHole = objHoles.transform.Find($"Hole_{i}");
            GameObject objHole = tHole != null ? tHole.gameObject : new GameObject($"Hole_{i}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            if (tHole == null) objHole.transform.SetParent(objHoles.transform, false);
            RectTransform rtHole = objHole.GetComponent<RectTransform>();
            rtHole.anchorMin = rtHole.anchorMax = new Vector2(0.5f, 0.5f);
            rtHole.anchoredPosition = new Vector2(-682f, 325f - i * 130f);
            rtHole.sizeDelta = new Vector2(30f, 30f);
            Image imgHole = objHole.GetComponent<Image>();
            if (spKnob != null) imgHole.sprite = spKnob;
            imgHole.color = new Color(0.62f, 0.67f, 0.74f);
            imgHole.raycastTarget = false;
        }

        // Red Margin Line
        Transform tLine = cardQuiz.transform.Find("Margin_Line_Red");
        GameObject objLine = tLine != null ? tLine.gameObject : new GameObject("Margin_Line_Red", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        if (tLine == null) objLine.transform.SetParent(cardQuiz.transform, false);
        objLine.transform.SetSiblingIndex(2);
        RectTransform rtLine = objLine.GetComponent<RectTransform>();
        rtLine.anchorMin = rtLine.anchorMax = new Vector2(0.5f, 0.5f);
        rtLine.anchoredPosition = new Vector2(-635f, 0f);
        rtLine.sizeDelta = new Vector2(4f, 840f);
        Image imgLine = objLine.GetComponent<Image>();
        imgLine.color = new Color(0.94f, 0.27f, 0.27f, 0.85f);
        imgLine.raycastTarget = false;

        // Question Prompt
        Transform tPrompt = cardQuiz.transform.Find("Question_Prompt") ?? cardQuiz.transform.Find("Text_Prompt");
        GameObject objPrompt = tPrompt != null ? tPrompt.gameObject : new GameObject("Question_Prompt", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        objPrompt.name = "Question_Prompt";
        if (tPrompt == null) objPrompt.transform.SetParent(cardQuiz.transform, false);
        RectTransform rtPrompt = objPrompt.GetComponent<RectTransform>();
        rtPrompt.anchorMin = rtPrompt.anchorMax = new Vector2(0.5f, 0.5f);
        rtPrompt.anchoredPosition = new Vector2(35f, 345f);
        rtPrompt.sizeDelta = new Vector2(1200f, 90f);
        Text txtPrompt = objPrompt.GetComponent<Text>();
        txtPrompt.text = "Rumah manakah yang sedang terkena bahaya gempa bumi?";
        txtPrompt.font = defaultFont;
        txtPrompt.fontSize = 36;
        txtPrompt.fontStyle = FontStyle.Bold;
        txtPrompt.alignment = TextAnchor.MiddleCenter;
        txtPrompt.color = colDarkInk;

        // Feedback Text (Biru ajakan di atas tombol Pilih)
        Transform tFeedback = cardQuiz.transform.Find("FeedbackText") ?? cardQuiz.transform.Find("Text_Feedback");
        GameObject objFeedback = tFeedback != null ? tFeedback.gameObject : new GameObject("FeedbackText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        objFeedback.name = "FeedbackText";
        if (tFeedback == null) objFeedback.transform.SetParent(cardQuiz.transform, false);
        RectTransform rtFeedback = objFeedback.GetComponent<RectTransform>();
        rtFeedback.anchorMin = rtFeedback.anchorMax = new Vector2(0.5f, 0.5f);
        rtFeedback.anchoredPosition = new Vector2(35f, -240f);
        rtFeedback.sizeDelta = new Vector2(1200f, 48f);
        Text txtFeedback = objFeedback.GetComponent<Text>();
        txtFeedback.text = "Pilih salah satu gambar lalu tekan tombol Pilih!";
        txtFeedback.font = defaultFont;
        txtFeedback.fontSize = 26;
        txtFeedback.fontStyle = FontStyle.Bold;
        txtFeedback.alignment = TextAnchor.MiddleCenter;
        txtFeedback.color = colBlue;

        // -------------------------------------------------------------
        // 3. 3 POLAROID OPTION CARDS (CRISP ROUNDED BORDER + SELOTIP KUNING)
        // -------------------------------------------------------------
        float[] posX = new float[] { -390f, 35f, 460f };
        Button[] btns = new Button[3];
        Image[] imgs = new Image[3];
        Text[] txts = new Text[3];
        Image[] borders = new Image[3];

        for (int i = 0; i < 3; i++)
        {
            Transform tOpt = cardQuiz.transform.Find($"Btn_Option_{i}");
            GameObject objOpt = tOpt != null ? tOpt.gameObject : new GameObject($"Btn_Option_{i}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            if (tOpt == null) objOpt.transform.SetParent(cardQuiz.transform, false);
            Undo.RecordObject(objOpt, $"Setup Option {i}");

            RectTransform rtOpt = objOpt.GetComponent<RectTransform>();
            rtOpt.anchorMin = rtOpt.anchorMax = new Vector2(0.5f, 0.5f);
            rtOpt.anchoredPosition = new Vector2(posX[i], 45f);
            rtOpt.sizeDelta = new Vector2(385f, 430f);

            var oldOl = objOpt.GetComponent<Outline>();
            if (oldOl != null) Object.DestroyImmediate(oldOl);

            ApplyBorderedCard(objOpt, spRoundedFill, colBorderSubtle, Color.white, 6f);
            Image borderImg = objOpt.GetComponent<Image>();
            borderImg.raycastTarget = true;
            borders[i] = borderImg;
            btns[i] = objOpt.GetComponent<Button>() ?? objOpt.AddComponent<Button>();
            btns[i].targetGraphic = borderImg;
            btns[i].transition = Selectable.Transition.None;

            // Yellow Masking Tape
            Transform tTape = objOpt.transform.Find("Tape_Sticker");
            GameObject objTape = tTape != null ? tTape.gameObject : new GameObject("Tape_Sticker", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            if (tTape == null) objTape.transform.SetParent(objOpt.transform, false);
            RectTransform rtTape = objTape.GetComponent<RectTransform>();
            rtTape.anchorMin = rtTape.anchorMax = new Vector2(0.5f, 0.5f);
            rtTape.anchoredPosition = new Vector2(0f, 214f);
            rtTape.sizeDelta = new Vector2(140f, 38f);
            rtTape.localEulerAngles = Vector3.zero;
            Image imgTape = objTape.GetComponent<Image>();
            imgTape.color = new Color(0.99f, 0.90f, 0.35f, 0.92f);
            imgTape.raycastTarget = false;

            // Image Display
            Transform tDisp = objOpt.transform.Find("Image_Display");
            GameObject objDisp = tDisp != null ? tDisp.gameObject : new GameObject("Image_Display", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(AnimatedImageUI));
            if (tDisp == null) objDisp.transform.SetParent(objOpt.transform, false);
            RectTransform rtDisp = objDisp.GetComponent<RectTransform>();
            rtDisp.anchorMin = rtDisp.anchorMax = new Vector2(0.5f, 0.5f);
            rtDisp.anchoredPosition = new Vector2(0f, 30f);
            rtDisp.sizeDelta = new Vector2(325f, 285f);
            Image imgDisp = objDisp.GetComponent<Image>() ?? objDisp.AddComponent<Image>();
            imgDisp.preserveAspect = true;
            imgDisp.enabled = (imgDisp.sprite != null);
            imgDisp.raycastTarget = false;
            imgs[i] = imgDisp;

            // Text Label
            Transform tLbl = objOpt.transform.Find("Text_Label");
            GameObject objLbl = tLbl != null ? tLbl.gameObject : new GameObject("Text_Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            if (tLbl == null) objLbl.transform.SetParent(objOpt.transform, false);
            RectTransform rtLbl = objLbl.GetComponent<RectTransform>();
            rtLbl.anchorMin = rtLbl.anchorMax = new Vector2(0.5f, 0.5f);
            rtLbl.anchoredPosition = new Vector2(0f, -162f);
            rtLbl.sizeDelta = new Vector2(340f, 48f);
            Text txtLbl = objLbl.GetComponent<Text>();
            txtLbl.font = defaultFont;
            txtLbl.fontSize = 25;
            txtLbl.fontStyle = FontStyle.Bold;
            txtLbl.alignment = TextAnchor.MiddleCenter;
            txtLbl.color = colDarkInk;
            txtLbl.raycastTarget = false;
            txts[i] = txtLbl;

            // Hide any legacy Text child
            Transform legacyTxt = objOpt.transform.Find("Text");
            if (legacyTxt != null) legacyTxt.gameObject.SetActive(false);

            objTape.transform.SetAsLastSibling();
        }

        // -------------------------------------------------------------
        // 4. PILIH BUTTON (CONFIRM ANSWER)
        // -------------------------------------------------------------
        Transform oldBtnConfirm = panelQuiz.transform.Find("Btn_ConfirmAnswer");
        if (oldBtnConfirm != null) oldBtnConfirm.SetParent(cardQuiz.transform, false);

        Transform tConfirm = cardQuiz.transform.Find("Btn_ConfirmAnswer");
        GameObject objConfirm = tConfirm != null ? tConfirm.gameObject : new GameObject("Btn_ConfirmAnswer", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        if (tConfirm == null) objConfirm.transform.SetParent(cardQuiz.transform, false);
        Undo.RecordObject(objConfirm, "Setup Confirm Button");

        RectTransform rtConfirm = objConfirm.GetComponent<RectTransform>();
        rtConfirm.anchorMin = rtConfirm.anchorMax = new Vector2(0.5f, 0.5f);
        rtConfirm.anchoredPosition = new Vector2(35f, -342f);
        rtConfirm.sizeDelta = new Vector2(420f, 92f);

        Sprite sprBtnGreen = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/spr_ui_rounded_btn_green.png");
        Image imgConfirm = objConfirm.GetComponent<Image>() ?? objConfirm.AddComponent<Image>();
        if (sprBtnGreen != null)
        {
            imgConfirm.sprite = sprBtnGreen;
            imgConfirm.type = Image.Type.Sliced;
            imgConfirm.color = Color.white;
        }
        else
        {
            imgConfirm.color = colGreen;
        }
        var confirmOl = objConfirm.GetComponent<Outline>();
        if (confirmOl != null) Object.DestroyImmediate(confirmOl);

        Transform tConfirmTxt = objConfirm.transform.Find("Text");
        GameObject objConfirmTxt = tConfirmTxt != null ? tConfirmTxt.gameObject : new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        if (tConfirmTxt == null) objConfirmTxt.transform.SetParent(objConfirm.transform, false);
        SetFullStretch(objConfirmTxt.GetComponent<RectTransform>());
        Text txtConfirm = objConfirmTxt.GetComponent<Text>();
        txtConfirm.text = "Pilih";
        txtConfirm.font = defaultFont;
        txtConfirm.fontSize = 32;
        txtConfirm.fontStyle = FontStyle.Bold;
        txtConfirm.alignment = TextAnchor.MiddleCenter;
        txtConfirm.color = Color.white;

        // -------------------------------------------------------------
        // 5. HUBUNGKAN LEVEL1QUIZMANAGER
        // -------------------------------------------------------------
        if (quizManager == null) quizManager = panelQuiz.GetComponent<Level1QuizManager>();
        if (quizManager == null) quizManager = panelQuiz.AddComponent<Level1QuizManager>();
        Undo.RecordObject(quizManager, "Connect Word Quiz Elements");

        quizManager.quizPanel = panelQuiz;
        quizManager.promptText = txtPrompt;
        quizManager.feedbackText = txtFeedback;
        quizManager.confirmAnswerButton = objConfirm.GetComponent<Button>();
        quizManager.buGuruCard = rtGuru;
        quizManager.optionButtons = btns;
        quizManager.optionImages = imgs;
        quizManager.optionTexts = txts;
        quizManager.optionCardBorderImages = borders;
        quizManager.optionCardOutlines = new Outline[3];

        quizManager.SetupDefaultQuestions();
        quizManager.AutoAssignAssetsInEditor();

        panelQuiz.SetActive(false);

        EditorUtility.SetDirty(panelQuiz);
        EditorUtility.SetDirty(quizManager);

        if (!Application.isPlaying)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
        }

        Debug.Log("[Level23SetupEditor] Restyle Quiz UI sesuai rancangan Word berhasil!");
    }

    // ==========================================
    // BERSIHKAN DIORAMA DAN PASTIKAN ALUR NORMAL
    // ==========================================
    // [MenuItem("Tools/Reset/Kembalikan Alur Normal (Hapus Diorama dan Kuis)")]
    public static void ResetAndCleanupDiorama()
    {
        // 1. Hapus Diorama jika ada di scene
        GameObject diorama = GameObject.Find("Diorama_Soal1_Rumah");
        if (diorama != null)
        {
            Undo.DestroyObjectImmediate(diorama);
        }

        // 2. Nonaktifkan Panel Kuis
        GameObject quizPanel = GameObject.Find("Panel_Level1_Quiz");
        if (quizPanel != null)
        {
            Undo.RecordObject(quizPanel, "Hide Quiz Panel");
            quizPanel.SetActive(false);
        }

        // 3. Matikan enableLevel1Quiz di GameManager
        GameManager gm = Object.FindFirstObjectByType<GameManager>();
        if (gm != null)
        {
            Undo.RecordObject(gm, "Set Normal Flow");
            gm.enableLevel1Quiz = false;
            EditorUtility.SetDirty(gm);
        }

        // 4. Pastikan karakter bisa jalan
        BoyController boy = Object.FindFirstObjectByType<BoyController>();
        if (boy != null)
        {
            boy.enabled = true;
            EditorUtility.SetDirty(boy);
        }

        
        if (!Application.isPlaying)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
        }

        EditorUtility.DisplayDialog("Reset Selesai",
            "Scene berhasil dikembalikan ke kondisi normal:\n" +
            "- Diorama kuis telah dibersihkan\n" +
            "- Panel kuis dinonaktifkan\n" +
            "- Kontrol karakter aktif\n" +
            "- Simulasi langsung mulai dari zona TV di kelas!", "OK");
    }

    // ==========================================
    // BERSIHKAN MEJA KELAS (SISAKAN MEJA 2)
    // ==========================================
    // [MenuItem("Tools/Level 2: Bersihkan Meja Kelas (Sisakan 1 Meja Belajar 2)")]
    public static void CleanClassroomDesks()
    {
        string[] desksToHide = new string[]
        {
            "Meja_Belajar_1", "Meja_Belajar_3",
            "Kursi_1_1", "Kursi_1_2", "Kursi_1_3", "Kursi_1_4",
            "Kursi_3_1", "Kursi_3_2", "Kursi_3_3", "Kursi_3_4"
        };
        int hiddenCount = 0;
        foreach (string deskName in desksToHide)
        {
            GameObject deskObj = GameObject.Find(deskName);
            if (deskObj != null)
            {
                Undo.RecordObject(deskObj, "Sembunyikan Meja Kelas");
                deskObj.SetActive(false);
                hiddenCount++;
            }
        }

        GameObject keepTable = GameObject.Find("Meja_Belajar_2");
        if (keepTable != null)
        {
            keepTable.SetActive(true);
            Selection.activeGameObject = keepTable;
            SceneView.lastActiveSceneView?.FrameSelected();
        }

        
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorUtility.DisplayDialog("Meja Kelas Dibersihkan",
            $"Berhasil menonaktifkan {hiddenCount} objek meja dan kursi lain.\n" +
            "Hanya 'Meja_Belajar_2' yang sekarang aktif untuk objektif kolong Level 2!", "OK");
    }

    // ==========================================
    // LEVEL 2: OBJEKTIF BERLINDUNG DI KOLONG MEJA & DAUN MEJA PADAT
    // ==========================================
    [MenuItem("Tools/Simulasi Gempa/Level 2: Pasang Objektif Kolong Meja")]
    public static void SetupShelterObjective()
    {
        int created = 0;
        foreach (GameObject desk in Selection.gameObjects)
        {
            if (!TryGetBounds(desk, out Bounds b))
            {
                Debug.LogWarning($"[Level23Setup] '{desk.name}' tidak punya Renderer, dilewati.");
                continue;
            }

            // 1. Bersihkan collider manual yang mungkin memblokir seluruh meja
            Collider manualCol = desk.GetComponent<Collider>();
            if (manualCol != null)
            {
                Object.DestroyImmediate(manualCol);
            }

            // 2. Buat / Perbarui Collider Daun Meja PADAT (HANYA di bagian atas, tebal 8cm, kolong tetap bolong)
            string solidName = $"Solid_Daun_Meja_{desk.name}";
            GameObject solidTop = GameObject.Find(solidName);
            if (solidTop == null)
            {
                solidTop = new GameObject(solidName);
                Undo.RegisterCreatedObjectUndo(solidTop, "Pasang Daun Meja Padat");
            }
            float topThickness = 0.08f;
            solidTop.transform.position = new Vector3(b.center.x, b.max.y - (topThickness * 0.5f), b.center.z);
            BoxCollider solidBox = solidTop.GetComponent<BoxCollider>();
            if (solidBox == null) solidBox = solidTop.AddComponent<BoxCollider>();
            solidBox.isTrigger = false; // SOLID! Menahan anak yang berdiri agar TIDAK BISA tembus meja
            solidBox.size = new Vector3(b.size.x, topThickness, b.size.z);

            // 3. Buat / Perbarui Zona Trigger Kolong Meja (Sensor Misi Drop & Cover)
            string zoneName = $"Objektif_Level2_{desk.name}";
            GameObject zone = GameObject.Find(zoneName);
            if (zone == null)
            {
                zone = new GameObject(zoneName);
                Undo.RegisterCreatedObjectUndo(zone, "Pasang Objektif Level 2");
            }
            float kolongHeight = Mathf.Max(0.2f, b.size.y - topThickness);
            zone.transform.position = new Vector3(b.center.x, b.min.y + (kolongHeight * 0.5f), b.center.z);

            BoxCollider triggerBox = zone.GetComponent<BoxCollider>();
            if (triggerBox == null) triggerBox = zone.AddComponent<BoxCollider>();
            triggerBox.isTrigger = true; // TRIGGER! Tembus pandang agar anak bisa masuk
            triggerBox.size = new Vector3(b.size.x + 0.3f, kolongHeight, b.size.z + 0.3f);

            CheckpointTrigger checkpoint = zone.GetComponent<CheckpointTrigger>();
            if (checkpoint == null) checkpoint = zone.AddComponent<CheckpointTrigger>();
            checkpoint.targetLevel = 2;
            checkpoint.checkpointName = "Kolong Meja";
            checkpoint.requireCrouch = true;
            checkpoint.requiredHoldTime = 5f;

            created++;
        }

        if (created == 0)
        {
            EditorUtility.DisplayDialog("Pilih Meja Dulu",
                "Pilih meja (misal 'Meja_Belajar_2') di Hierarchy, lalu jalankan menu ini lagi.", "OK");
            return;
        }

        EditorUtility.DisplayDialog("Setup Meja Berhasil Sempurna!",
            $"{created} meja berhasil disetup otomatis:\n\n" +
            "1. DAUN MEJA PADAT: Karakter berdiri TIDAK BISA nembus meja.\n" +
            "2. KOLONG BOLONG: Karakter menunduk BISA MASUK ke kolong dengan lancar.\n" +
            "3. SENSOR LEVEL 2: Deteksi Drop & Cover 5 detik otomatis aktif.", "Mantap!");
    }

    [MenuItem("Tools/Simulasi Gempa/Level 2: Pasang Efek Gempa")]
    public static void SetupEarthquakeEffectMenu()
    {
        SetupEarthquakeEffect(true);
    }

    public static void SetupEarthquakeEffect(bool showDialog = false)
    {
        EarthquakeEffect existing = Object.FindFirstObjectByType<EarthquakeEffect>(FindObjectsInactive.Include);
        if (existing != null)
        {
            if (showDialog) EditorUtility.DisplayDialog("Sudah Ada", "Efek gempa sudah ada di scene ini.", "OK");
            return;
        }

        GameObject quakeObj = new GameObject("Efek_Gempa", typeof(EarthquakeEffect), typeof(AudioSource));
        Undo.RegisterCreatedObjectUndo(quakeObj, "Pasang Efek Gempa");

        AudioSource audio = quakeObj.GetComponent<AudioSource>();
        audio.playOnAwake = false;
        audio.loop = true;
        AudioClip rumbleClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/sfx_earthquake_rumble.wav")
            ?? AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Quiz/Soal2_Suara_B_Gempa.wav");
        if (rumbleClip != null) audio.clip = rumbleClip;

        EarthquakeEffect eq = quakeObj.GetComponent<EarthquakeEffect>();
        SerializedObject so = new SerializedObject(eq);
        so.Update();
        so.FindProperty("rumbleAudio").objectReferenceValue = audio;
        so.FindProperty("quakeLevel").intValue = 2;
        so.FindProperty("shakeAmplitude").floatValue = 0.04f; // Default lembut ramah anak
        so.FindProperty("shakeFrequency").floatValue = 22f;

        // Overlay layar penuh di bawah HUD, supaya instruksi misi tetap terbaca
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas != null)
        {
            Transform tOverlay = canvas.transform.Find("Overlay_Gempa");
            GameObject overlayObj = tOverlay != null ? tOverlay.gameObject : new GameObject("Overlay_Gempa", typeof(RectTransform), typeof(Image));
            if (tOverlay == null)
            {
                Undo.RegisterCreatedObjectUndo(overlayObj, "Pasang Overlay Gempa");
                overlayObj.transform.SetParent(canvas.transform, false);
                overlayObj.transform.SetAsFirstSibling();
            }

            RectTransform rect = overlayObj.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image overlay = overlayObj.GetComponent<Image>();
            overlay.color = new Color(0.45f, 0.25f, 0.1f, 0f);
            overlay.raycastTarget = false;
            so.FindProperty("overlayImage").objectReferenceValue = overlay;
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(quakeObj);
        EditorUtility.SetDirty(eq);

        if (showDialog)
        {
            EditorUtility.DisplayDialog("Efek Gempa Terpasang",
                "Gempa otomatis aktif selama Level 2.\n\n" +
                "Opsional di Inspector 'Efek_Gempa':\n" +
                "• AudioSource → isi AudioClip suara gemuruh\n" +
                "• Falling Objects → seret benda yang jatuh (beri Rigidbody, centang Is Kinematic)", "Siap");
        }
    }

    [MenuItem("Tools/Simulasi Gempa/UI: Pasang Slider Kamera di Menu Pause")]
    public static void SetupPauseCameraSlider()
    {
        MissionUI mui = Object.FindFirstObjectByType<MissionUI>(FindObjectsInactive.Include);
        if (mui == null || mui.pausePanel == null) return;

        Transform card = mui.pausePanel.transform.Find("Card_Pause");
        if (card == null) return;

        Font defaultFont = GetProjectFont();
        Sprite spRoundedFill = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/spr_ui_rounded_fill.png");
        Sprite spBtnYellow = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/spr_ui_rounded_btn_yellow.png");
        Sprite spBtnGreen = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/spr_ui_rounded_btn_green.png");
        Sprite spBtnWhite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/spr_ui_rounded_btn_white.png");

        Color colDarkInk = new Color(0.10f, 0.14f, 0.22f);
        Color colCream = new Color(1.00f, 0.984f, 0.922f);
        Color colOrange = new Color(0.96f, 0.62f, 0.04f);
        Color colGreen = new Color(0.13f, 0.77f, 0.36f);
        Color colBlue = new Color(0.01f, 0.52f, 0.78f);

        RectTransform rtPanel = mui.pausePanel.GetComponent<RectTransform>();
        if (rtPanel != null) SetFullStretch(rtPanel);
        Image backdrop = mui.pausePanel.GetComponent<Image>();
        if (backdrop != null) backdrop.color = new Color(0.04f, 0.07f, 0.17f, 0.68f);

        RectTransform rtCard = card.GetComponent<RectTransform>();
        if (rtCard != null)
        {
            rtCard.anchoredPosition = new Vector2(0f, -12f);
            rtCard.sizeDelta = new Vector2(1420f, 860f);
        }
        ApplyBorderedCard(card.gameObject, spRoundedFill, colOrange, colCream, 7f);

        Transform tSliderQuake = card.Find("Slider_EarthquakeIntensity");
        if (tSliderQuake != null) Undo.DestroyObjectImmediate(tSliderQuake.gameObject);
        Transform tLabelQuake = card.Find("LabelEarthquake");
        if (tLabelQuake != null) Undo.DestroyObjectImmediate(tLabelQuake.gameObject);

        // Ribbon Header
        RectTransform rtRibbon = card.Find("Ribbon_Header") as RectTransform;
        if (rtRibbon != null)
        {
            rtRibbon.anchorMin = rtRibbon.anchorMax = new Vector2(0.5f, 1f);
            rtRibbon.pivot = new Vector2(0.5f, 0.5f);
            rtRibbon.anchoredPosition = Vector2.zero;
            rtRibbon.sizeDelta = new Vector2(820f, 86f);
            var rOl = rtRibbon.GetComponent<Outline>();
            if (rOl != null) Object.DestroyImmediate(rOl);
            Image rImg = rtRibbon.GetComponent<Image>();
            if (rImg != null && spBtnYellow != null) { rImg.sprite = spBtnYellow; rImg.type = Image.Type.Sliced; rImg.color = Color.white; }
            Text rTxt = rtRibbon.Find("Text")?.GetComponent<Text>();
            if (rTxt != null) { rTxt.text = "PENGATURAN AUDIO & JEDA"; rTxt.font = defaultFont; rTxt.fontSize = 34; rTxt.fontStyle = FontStyle.Bold; rTxt.color = colDarkInk; }
        }

        // BGM & SFX Rows
        StylePauseLabel(card.Find("LabelBGM"), "Musik Latar (BGM)", defaultFont, colDarkInk, new Vector2(-340f, 225f), new Vector2(540f, 54f));
        Slider sldBgm = card.Find("Slider_BGM")?.GetComponent<Slider>();
        if (sldBgm != null)
        {
            StyleCapsuleSlider(sldBgm, spRoundedFill, colGreen, new Vector2(260f, 225f), new Vector2(620f, 44f));
            mui.pauseBgmSlider = sldBgm;
        }

        StylePauseLabel(card.Find("LabelSFX"), "Efek Suara (SFX)", defaultFont, colDarkInk, new Vector2(-340f, 115f), new Vector2(540f, 54f));
        Slider sldSfx = card.Find("Slider_SFX")?.GetComponent<Slider>();
        if (sldSfx != null)
        {
            StyleCapsuleSlider(sldSfx, spRoundedFill, colGreen, new Vector2(260f, 115f), new Vector2(620f, 44f));
            mui.pauseSfxSlider = sldSfx;
        }

        // Voice Over Pill Row
        Transform tLblVo = card.Find("LabelVO");
        GameObject lblVoObj = tLblVo != null ? tLblVo.gameObject : new GameObject("LabelVO", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        if (tLblVo == null) lblVoObj.transform.SetParent(card, false);
        StylePauseLabel(lblVoObj.transform, "Suara Bu Guru (Voice Over)", defaultFont, colDarkInk, new Vector2(-340f, 5f), new Vector2(560f, 54f));

        Transform tVoTg = card.Find("Toggle_VoiceOver");
        GameObject voTgObj = tVoTg != null ? tVoTg.gameObject : new GameObject("Toggle_VoiceOver", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Toggle));
        if (tVoTg == null) voTgObj.transform.SetParent(card, false);
        RectTransform rtVoTg = voTgObj.GetComponent<RectTransform>();
        rtVoTg.anchorMin = rtVoTg.anchorMax = new Vector2(0.5f, 0.5f);
        rtVoTg.anchoredPosition = new Vector2(445f, 5f);
        rtVoTg.sizeDelta = new Vector2(240f, 78f);
        Image voBg = voTgObj.GetComponent<Image>();
        if (spRoundedFill != null) { voBg.sprite = spRoundedFill; voBg.type = Image.Type.Sliced; }
        voBg.color = colGreen;
        Toggle voToggle = voTgObj.GetComponent<Toggle>() ?? voTgObj.AddComponent<Toggle>();
        voToggle.transition = Selectable.Transition.None;
        voToggle.targetGraphic = voBg;

        Transform tVoTxt = voTgObj.transform.Find("Text");
        GameObject voTxtObj = tVoTxt != null ? tVoTxt.gameObject : new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        if (tVoTxt == null) voTxtObj.transform.SetParent(voTgObj.transform, false);
        RectTransform rtVoTxt = voTxtObj.GetComponent<RectTransform>();
        rtVoTxt.anchorMin = Vector2.zero; rtVoTxt.anchorMax = Vector2.one;
        rtVoTxt.offsetMin = Vector2.zero; rtVoTxt.offsetMax = new Vector2(-32f, 0f);
        Text voTxt = voTxtObj.GetComponent<Text>();
        voTxt.text = "AKTIF";
        voTxt.font = defaultFont;
        voTxt.fontSize = 28;
        voTxt.fontStyle = FontStyle.Bold;
        voTxt.alignment = TextAnchor.MiddleCenter;
        voTxt.color = Color.white;
        voTxt.raycastTarget = false;

        GameObject voChk = BuildCheckIcon("CheckIcon", voTgObj.transform, new Vector2(58f, 0f), Color.white);
        mui.pauseVoiceOverToggle = voToggle;
        mui.pauseVoiceOverBg = voBg;
        mui.pauseVoiceOverText = voTxt;
        mui.pauseVoiceOverCheckIcon = voChk;

        // Camera Distance Row
        Transform tLabel = card.Find("LabelCamera");
        GameObject labelObj = tLabel != null ? tLabel.gameObject : new GameObject("LabelCamera", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        if (tLabel == null) labelObj.transform.SetParent(card, false);
        StylePauseLabel(labelObj.transform, "Jarak Kamera (Zoom)", defaultFont, colDarkInk, new Vector2(-340f, -105f), new Vector2(540f, 54f));

        Transform tSlider = card.Find("Slider_CameraDistance");
        if (tSlider == null && sldSfx != null)
        {
            GameObject sliderObj = Object.Instantiate(sldSfx.gameObject, card, false);
            sliderObj.name = "Slider_CameraDistance";
            tSlider = sliderObj.transform;
        }
        if (tSlider != null)
        {
            Slider sldCam = tSlider.GetComponent<Slider>();
            if (sldCam != null)
            {
                StyleCapsuleSlider(sldCam, spRoundedFill, colBlue, new Vector2(230f, -105f), new Vector2(540f, 44f));
                sldCam.minValue = 1.2f;
                sldCam.maxValue = 5.0f;
                sldCam.value = 3.5f;
                mui.pauseCameraDistanceSlider = sldCam;
            }
            Transform tVal = tSlider.Find("Text_Value");
            GameObject valObj = tVal != null ? tVal.gameObject : new GameObject("Text_Value", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            if (tVal == null) valObj.transform.SetParent(tSlider, false);
            RectTransform rtVal = valObj.GetComponent<RectTransform>();
            rtVal.anchorMin = rtVal.anchorMax = new Vector2(1f, 0.5f);
            rtVal.pivot = new Vector2(0f, 0.5f);
            rtVal.anchoredPosition = new Vector2(16f, 0f);
            rtVal.sizeDelta = new Vector2(100f, 44f);
            Text txtVal = valObj.GetComponent<Text>();
            txtVal.font = defaultFont;
            txtVal.fontSize = 26;
            txtVal.fontStyle = FontStyle.Bold;
            txtVal.color = colDarkInk;
            txtVal.alignment = TextAnchor.MiddleLeft;
            txtVal.text = "3.5m";
            mui.pauseCameraDistanceValueText = txtVal;
        }

        // Bottom Buttons
        StyleActionButton(card.Find("Btn_Resume"), "LANJUTKAN BERMAIN", spBtnGreen, spRoundedFill, colGreen, Color.white, 28, defaultFont, new Vector2(0f, -225f), new Vector2(580f, 82f));
        StyleActionButton(card.Find("Btn_Ulangi"), "Ulangi", spBtnWhite, spRoundedFill, Color.white, colDarkInk, 26, defaultFont, new Vector2(-190f, -330f), new Vector2(340f, 78f));
        StyleActionButton(card.Find("Btn_MenuUtama"), "Menu Utama", spBtnWhite, spRoundedFill, Color.white, colDarkInk, 26, defaultFont, new Vector2(190f, -330f), new Vector2(340f, 78f));

        EditorUtility.SetDirty(card.gameObject);
        EditorUtility.SetDirty(mui);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    [MenuItem("Tools/Simulasi Gempa/UI: Pasang Tombol Pause di HUD Gameplay")]
    public static void SetupHUDPauseButton()
    {
        MissionUI mui = Object.FindFirstObjectByType<MissionUI>(FindObjectsInactive.Include);
        if (mui == null) return;

        Transform hudTop = mui.transform.Find("HUD_Top") ?? mui.transform.parent?.Find("HUD_Top");
        if (hudTop == null)
        {
            GameObject g = GameObject.Find("HUD_Top");
            if (g != null) hudTop = g.transform;
        }
        if (hudTop == null) return;

        Font defaultFont = GetProjectFont();
        Sprite spRoundedFill = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/spr_ui_rounded_fill.png");
        Sprite sprHudDark = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/spr_ui_rounded_hud_dark.png");
        Sprite sprPause = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/spr_ui_rounded_btn_orange.png");

        // 1. MissionBanner kiri atas
        Transform banner = hudTop.Find("MissionBanner");
        if (banner != null)
        {
            RectTransform rtBanner = banner.GetComponent<RectTransform>();
            if (rtBanner != null)
            {
                rtBanner.anchorMin = rtBanner.anchorMax = rtBanner.pivot = new Vector2(0f, 1f);
                rtBanner.anchoredPosition = new Vector2(35f, -30f);
                rtBanner.sizeDelta = new Vector2(880f, 88f);
            }
            if (sprHudDark != null)
            {
                var bImg = banner.GetComponent<Image>();
                if (bImg != null) { bImg.sprite = sprHudDark; bImg.type = Image.Type.Sliced; bImg.color = Color.white; }
            }
            Transform tInstr = banner.Find("InstructionText");
            if (tInstr != null)
            {
                RectTransform rtInstr = tInstr.GetComponent<RectTransform>();
                if (rtInstr != null)
                {
                    rtInstr.anchorMin = Vector2.zero; rtInstr.anchorMax = Vector2.one;
                    rtInstr.offsetMin = new Vector2(26f, 8f); rtInstr.offsetMax = new Vector2(-26f, -8f);
                }
                Text txtInstr = tInstr.GetComponent<Text>();
                if (txtInstr != null)
                {
                    txtInstr.font = defaultFont;
                    txtInstr.alignment = TextAnchor.MiddleLeft;
                    txtInstr.fontSize = 24;
                    txtInstr.fontStyle = FontStyle.Bold;
                    txtInstr.color = Color.white;
                }
            }
        }

        // 2. TimerBadge kanan atas
        Transform timerBadge = hudTop.Find("TimerBadge");
        if (timerBadge != null)
        {
            RectTransform rtTimer = timerBadge.GetComponent<RectTransform>();
            if (rtTimer != null)
            {
                rtTimer.anchorMin = rtTimer.anchorMax = rtTimer.pivot = new Vector2(1f, 1f);
                rtTimer.anchoredPosition = new Vector2(-150f, -30f);
                rtTimer.sizeDelta = new Vector2(180f, 88f);
            }
            if (sprHudDark != null)
            {
                var tImg = timerBadge.GetComponent<Image>();
                if (tImg != null) { tImg.sprite = sprHudDark; tImg.type = Image.Type.Sliced; tImg.color = Color.white; }
            }
            Text txtTimer = timerBadge.Find("TimerText")?.GetComponent<Text>();
            if (txtTimer != null)
            {
                txtTimer.font = defaultFont;
                txtTimer.fontSize = 32;
                txtTimer.fontStyle = FontStyle.Bold;
                txtTimer.color = new Color(0.98f, 0.75f, 0.14f);
            }
        }

        // 3. Tombol Pause kanan atas dengan 2 bar putih (||) bebas glyph rusak
        Transform tPause = hudTop.Find("Btn_Pause");
        GameObject objPause = tPause != null ? tPause.gameObject : new GameObject("Btn_Pause", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        if (tPause == null) objPause.transform.SetParent(hudTop, false);

        RectTransform rtBtn = objPause.GetComponent<RectTransform>();
        rtBtn.anchorMin = rtBtn.anchorMax = rtBtn.pivot = new Vector2(1f, 1f);
        rtBtn.anchoredPosition = new Vector2(-35f, -30f);
        rtBtn.sizeDelta = new Vector2(92f, 92f);

        Image imgBtn = objPause.GetComponent<Image>() ?? objPause.AddComponent<Image>();
        var olPause = objPause.GetComponent<Outline>();
        if (olPause != null) Object.DestroyImmediate(olPause);
        if (sprPause != null) { imgBtn.sprite = sprPause; imgBtn.type = Image.Type.Sliced; imgBtn.color = Color.white; }
        else imgBtn.color = new Color(0.98f, 0.65f, 0.11f);

        Image imgHudTop = hudTop.GetComponent<Image>();
        if (imgHudTop != null) { imgHudTop.color = Color.clear; imgHudTop.enabled = false; }

        Transform tIcon = objPause.transform.Find("Text");
        if (tIcon != null)
        {
            Text txtIcon = tIcon.GetComponent<Text>();
            if (txtIcon != null) txtIcon.text = "";
        }

        CreatePauseBar(objPause.transform, "PauseBar_L", spRoundedFill, new Vector2(-11f, 0f));
        CreatePauseBar(objPause.transform, "PauseBar_R", spRoundedFill, new Vector2(11f, 0f));

        Button btn = objPause.GetComponent<Button>() ?? objPause.AddComponent<Button>();
        if (btn.onClick.GetPersistentEventCount() == 0)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(mui.OpenPauseMenu);
        }
        mui.pauseButton = btn;

        EditorUtility.SetDirty(hudTop.gameObject);
        EditorUtility.SetDirty(mui);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    public static void RestyleHoldProgressAndBuGuruHUD()
    {
        MissionUI mui = Object.FindFirstObjectByType<MissionUI>(FindObjectsInactive.Include);
        if (mui == null) return;

        Font defaultFont = GetProjectFont();
        Sprite spRoundedFill = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/spr_ui_rounded_fill.png");
        Sprite spKnob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

        Color colGreen = new Color(0.13f, 0.77f, 0.36f);
        Color colNavy = new Color(0.12f, 0.16f, 0.22f);
        Color colRedBorder = new Color(0.94f, 0.27f, 0.24f);
        Color colDarkRed = new Color(0.72f, 0.11f, 0.11f);

        // 1. HoldProgressContainer (Tabel 3.7 No. 5: "Bertahan: 3.4 / 5.0 dtk")
        GameObject holdObj = mui.holdProgressContainer ?? mui.transform.Find("HoldProgressContainer")?.gameObject;
        if (holdObj != null)
        {
            RectTransform rtHold = holdObj.GetComponent<RectTransform>();
            rtHold.anchorMin = rtHold.anchorMax = new Vector2(0.5f, 0f);
            rtHold.pivot = new Vector2(0.5f, 0.5f);
            rtHold.anchoredPosition = new Vector2(0f, 145f);
            rtHold.sizeDelta = new Vector2(740f, 72f);

            ApplyBorderedCard(holdObj, spRoundedFill, Color.white, colNavy, 4f);

            Transform tSlider = holdObj.transform.Find("HoldSlider");
            if (tSlider != null)
            {
                RectTransform rtSld = tSlider.GetComponent<RectTransform>();
                SetFullStretch(rtSld, 6f);
                Transform tFillArea = tSlider.Find("Fill Area");
                if (tFillArea != null) SetFullStretch(tFillArea.GetComponent<RectTransform>());
                Transform tFill = tFillArea != null ? tFillArea.Find("Fill") : null;
                if (tFill != null)
                {
                    Image fillImg = tFill.GetComponent<Image>();
                    if (fillImg != null)
                    {
                        if (spRoundedFill != null) { fillImg.sprite = spRoundedFill; fillImg.type = Image.Type.Sliced; }
                        fillImg.color = colGreen;
                    }
                }
            }

            Transform tLabel = holdObj.transform.Find("HoldLabel");
            if (tLabel != null)
            {
                tLabel.SetAsLastSibling();
                SetFullStretch(tLabel.GetComponent<RectTransform>());
                Text txtLabel = tLabel.GetComponent<Text>();
                if (txtLabel != null)
                {
                    txtLabel.font = defaultFont;
                    txtLabel.fontSize = 28;
                    txtLabel.fontStyle = FontStyle.Bold;
                    txtLabel.alignment = TextAnchor.MiddleCenter;
                    txtLabel.color = Color.white;
                    txtLabel.text = "Bertahan: 0.0 / 5.0 dtk";
                    mui.holdProgressText = txtLabel;
                }
            }
            mui.holdProgressContainer = holdObj;
        }

        // 2. HUD_BuGuru Toast (Tabel 3.7 No. 6: Pill putih border merah horizontal)
        RectTransform rtToast = mui.buGuruHUD ?? mui.transform.Find("HUD_BuGuru")?.GetComponent<RectTransform>();
        if (rtToast != null)
        {
            GameObject toastObj = rtToast.gameObject;
            var oldOl = toastObj.GetComponent<Outline>();
            if (oldOl != null) Object.DestroyImmediate(oldOl);

            rtToast.anchorMin = rtToast.anchorMax = new Vector2(0f, 1f);
            rtToast.pivot = new Vector2(0f, 1f);
            rtToast.anchoredPosition = new Vector2(35f, -142f);
            rtToast.sizeDelta = new Vector2(760f, 142f);

            ApplyBorderedCard(toastObj, spRoundedFill, colRedBorder, Color.white, 6f);

            Transform legacyName = toastObj.transform.Find("NameTxt");
            if (legacyName != null) legacyName.gameObject.SetActive(false);
            Transform legacyStatus = toastObj.transform.Find("StatusTxt");
            if (legacyStatus != null) legacyStatus.gameObject.SetActive(false);

            Transform tCircle = toastObj.transform.Find("AvatarCircle");
            GameObject circleObj = tCircle != null ? tCircle.gameObject : new GameObject("AvatarCircle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            if (tCircle == null) circleObj.transform.SetParent(toastObj.transform, false);
            var oldCircleOl = circleObj.GetComponent<Outline>();
            if (oldCircleOl != null) Object.DestroyImmediate(oldCircleOl);
            RectTransform rtCircle = circleObj.GetComponent<RectTransform>();
            rtCircle.anchorMin = rtCircle.anchorMax = new Vector2(0f, 0.5f);
            rtCircle.pivot = new Vector2(0f, 0.5f);
            rtCircle.anchoredPosition = new Vector2(24f, 0f);
            rtCircle.sizeDelta = new Vector2(96f, 96f);
            Image imgCircle = circleObj.GetComponent<Image>();
            if (spKnob != null) imgCircle.sprite = spKnob;
            imgCircle.color = colGreen;

            Transform tPortrait = circleObj.transform.Find("Portrait");
            GameObject portObj = tPortrait != null ? tPortrait.gameObject : new GameObject("Portrait", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            if (tPortrait == null) portObj.transform.SetParent(circleObj.transform, false);
            SetFullStretch(portObj.GetComponent<RectTransform>(), 6f);
            Image imgPort = portObj.GetComponent<Image>();
            imgPort.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/avatar_guru.png");
            imgPort.color = Color.white;
            imgPort.preserveAspect = true;
            mui.buGuruAvatarImg = imgPort;

            Transform tSpeech = toastObj.transform.Find("SpeechText");
            GameObject speechObj = tSpeech != null ? tSpeech.gameObject : new GameObject("SpeechText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            if (tSpeech == null) speechObj.transform.SetParent(toastObj.transform, false);
            RectTransform rtSpeech = speechObj.GetComponent<RectTransform>();
            rtSpeech.anchorMin = Vector2.zero; rtSpeech.anchorMax = Vector2.one;
            rtSpeech.offsetMin = new Vector2(142f, 14f); rtSpeech.offsetMax = new Vector2(-24f, -14f);
            Text txtSpeech = speechObj.GetComponent<Text>();
            txtSpeech.font = defaultFont;
            txtSpeech.fontSize = 28;
            txtSpeech.fontStyle = FontStyle.Bold;
            txtSpeech.alignment = TextAnchor.MiddleLeft;
            txtSpeech.color = colDarkRed;
            txtSpeech.text = "Gempa Susulan! Merunduk dulu ya!";
            mui.buGuruSpeechText = txtSpeech;

            toastObj.SetActive(false);
        }

        EditorUtility.SetDirty(mui);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    // ==========================================
    // LEVEL 3: TAS PELINDUNG KEPALA & TITIK KUMPUL
    // ==========================================
    // [MenuItem("Tools/Level 3: Terapkan Model 3D Backpack dari Blender ke Scene")]
    public static void ApplyBackpack3DModel()
    {
        GameObject fbxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/backpack.fbx");
        if (fbxPrefab == null)
        {
            EditorUtility.DisplayDialog("Model Belum Terdeteksi", "File Assets/backpack.fbx belum siap di Unity. Tekan Ctrl+R di Unity lalu coba klik menu ini lagi.", "OK");
            return;
        }

        GameObject existingBag = GameObject.Find("Tas_Evakuasi");
        Vector3 position = existingBag != null ? existingBag.transform.position : new Vector3(0.38f, 0.5f, -8.0f);
        Quaternion rotation = existingBag != null ? existingBag.transform.rotation : Quaternion.identity;

        if (existingBag != null)
        {
            Undo.DestroyObjectImmediate(existingBag);
        }

        GameObject bag = (GameObject)PrefabUtility.InstantiatePrefab(fbxPrefab);
        Undo.RegisterCreatedObjectUndo(bag, "Pasang Model 3D Tas");
        bag.name = "Tas_Evakuasi";
        bag.transform.position = position;
        bag.transform.rotation = rotation;
        bag.transform.localScale = Vector3.one;

        BoxCollider box = bag.GetComponent<BoxCollider>();
        if (box == null) box = bag.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(1.8f, 1.8f, 1.8f);

        BagPickup pickup = bag.GetComponent<BagPickup>();
        if (pickup == null) pickup = bag.AddComponent<BagPickup>();
        pickup.bagName = "Tas Sekolah 3D";
        pickup.headOffset = new Vector3(0f, 0.15f, 0f);
        pickup.headRotation = new Vector3(0f, 0f, 0f);
        pickup.customScale = new Vector3(0.85f, 0.85f, 0.85f);

        Selection.activeGameObject = bag;

        EditorUtility.DisplayDialog("Model 3D Tas Berhasil Dipasang!",
            "Objek 'Tas_Evakuasi' sekarang sudah menggunakan model 3D Backpack asli dari Blender!\n\n" +
            "• Posisi tas tetap berada di samping meja kelas.\n" +
            "• Pada Level 3, saat anak mendekati tas, model 3D ini otomatis menempel di kepala karakter.", "Keren!");
    }

    [MenuItem("Tools/Simulasi Gempa/Level 3: Pasang Objek Tas di Kelas")]
    public static void SetupBagPickup()
    {
        Vector3 position;
        GameObject selected = Selection.activeGameObject;
        if (selected != null && TryGetBounds(selected, out Bounds b))
        {
            position = new Vector3(b.center.x, b.max.y + 0.15f, b.center.z);
        }
        else if (selected != null)
        {
            position = selected.transform.position + Vector3.up * 0.4f;
        }
        else
        {
            GameObject desk = GameObject.Find("Meja") ?? GameObject.Find("Meja Siswa 1") ?? GameObject.Find("Table");
            if (desk != null && TryGetBounds(desk, out Bounds db))
            {
                position = new Vector3(db.center.x, db.max.y + 0.15f, db.center.z);
            }
            else
            {
                BoyController boy = Object.FindFirstObjectByType<BoyController>();
                position = boy != null ? boy.transform.position + boy.transform.forward * 1.5f + Vector3.up * 0.4f : new Vector3(0f, 0.4f, 0f);
            }
        }

        GameObject fbxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/backpack.fbx");
        GameObject bag;
        if (fbxPrefab != null)
        {
            bag = (GameObject)PrefabUtility.InstantiatePrefab(fbxPrefab);
            bag.transform.localScale = Vector3.one;
        }
        else
        {
            bag = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bag.transform.localScale = new Vector3(0.35f, 0.45f, 0.2f);
            bag.GetComponent<Renderer>().sharedMaterial = GetOrCreateMaterial("Mat_TasSekolah", new Color(1f, 0.55f, 0.1f));
        }

        Undo.RegisterCreatedObjectUndo(bag, "Pasang Objek Tas");
        bag.name = "Tas_Evakuasi";
        bag.transform.position = position;

        BoxCollider box = bag.GetComponent<BoxCollider>();
        if (box == null) box = bag.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(1.8f, 1.8f, 1.8f);

        BagPickup pickup = bag.GetComponent<BagPickup>();
        if (pickup == null) pickup = bag.AddComponent<BagPickup>();
        pickup.bagName = "Tas Siswa";
        pickup.headOffset = new Vector3(0f, 0.15f, 0f);
        pickup.customScale = new Vector3(0.85f, 0.85f, 0.85f);

        Selection.activeGameObject = bag;

        EditorUtility.DisplayDialog("Objek Tas Terpasang",
            "Objek 'Tas_Evakuasi' berhasil dibuat!\n\n" +
            "• Geser posisinya ke atas meja atau kursi siswa.\n" +
            "• Pada Level 3, pemain mendekati tas ini untuk mengambilnya.\n" +
            "• Setelah terambil, tas menempel di kepala dan jalur evakuasi ke titik kumpul akan aktif.", "Siap");
    }

    [MenuItem("Tools/Simulasi Gempa/Level 3: Pasang Titik Kumpul di Posisi Terpilih")]
    public static void SetupAssemblyPoint()
    {
        Vector3 position;
        GameObject selected = Selection.activeGameObject;
        if (selected != null && TryGetBounds(selected, out Bounds b))
        {
            position = new Vector3(b.center.x, b.max.y, b.center.z); // di atas permukaan lantai terpilih
        }
        else if (selected != null)
        {
            position = selected.transform.position;
        }
        else if (SceneView.lastActiveSceneView != null)
        {
            position = SceneView.lastActiveSceneView.pivot;
        }
        else
        {
            EditorUtility.DisplayDialog("Pilih Lokasi Dulu",
                "Pilih lantai lapangan / objek di lokasi titik kumpul, lalu jalankan menu ini lagi.", "OK");
            return;
        }

        GameObject point = new GameObject("Titik_Kumpul_Level3");
        Undo.RegisterCreatedObjectUndo(point, "Pasang Titik Kumpul");
        point.transform.position = position;

        BoxCollider box = point.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(4f, 2.5f, 4f);
        box.center = new Vector3(0f, 1.25f, 0f);

        CheckpointTrigger checkpoint = point.AddComponent<CheckpointTrigger>();
        checkpoint.targetLevel = 3;
        checkpoint.checkpointName = "Titik Kumpul";
        checkpoint.requireCrouch = false;
        checkpoint.requiredHoldTime = 0f;
        checkpoint.requireBag = true;

        // Penanda lantai hijau supaya titik kumpul terlihat jelas oleh anak
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        marker.name = "Penanda_Titik_Kumpul";
        Object.DestroyImmediate(marker.GetComponent<Collider>());
        marker.transform.SetParent(point.transform, false);
        marker.transform.localPosition = new Vector3(0f, 0.02f, 0f);
        marker.transform.localScale = new Vector3(4f, 0.01f, 4f);
        marker.GetComponent<Renderer>().sharedMaterial = GetOrCreateMaterial("Mat_TitikKumpul", new Color(0.2f, 0.8f, 0.3f));

        SetupEvacuationGuide(point.transform);
        Selection.activeGameObject = point;

        EditorUtility.DisplayDialog("Titik Kumpul Terpasang",
            "Titik kumpul Level 3 dan garis jalur evakuasi dibuat.\n\n" +
            "Supaya garis berbelok lewat pintu (tidak menembus tembok):\n" +
            "1. Pilih objek denah (denah_tes)\n" +
            "2. Add Component → NavMesh Surface\n" +
            "3. Klik Bake\n\n" +
            "Geser 'Titik_Kumpul_Level3' bila posisinya belum pas.", "Siap");
    }

    [MenuItem("Tools/Simulasi Gempa/Level 3: Pasang Trigger Gempa Susulan")]
    public static void SetupAftershockTrigger()
    {
        // 1. Prioritaskan tempelkan langsung ke Bintang_WP_04 atau waypoint terpilih
        GameObject targetObj = Selection.activeGameObject;
        if (targetObj == null || !targetObj.name.Contains("WP"))
        {
            targetObj = GameObject.Find("Bintang_WP_04") ?? GameObject.Find("WP_04_UjungGangAspal") ?? GameObject.Find("Bintang_WP_04_UjungGangAspal");
        }

        // Hapus objek standalone lama jika ada agar tidak double
        GameObject oldStandalone = GameObject.Find("Trigger_Gempa_Susulan");
        if (oldStandalone != null && oldStandalone != targetObj)
        {
            Undo.DestroyObjectImmediate(oldStandalone);
        }

        if (targetObj != null)
        {
            Undo.RecordObject(targetObj, "Tempelkan Gempa Susulan");
            AftershockTrigger trigger = targetObj.GetComponent<AftershockTrigger>();
            if (trigger == null)
            {
                trigger = Undo.AddComponent<AftershockTrigger>(targetObj);
            }
            trigger.targetLevel = 3;
            trigger.onlyActiveInLevel3 = false;
            trigger.useDistanceCheck = true;
            trigger.triggerRadius = 2.5f;
            trigger.aftershockDuration = 5f;
            trigger.shakeAmplitude = 0.08f;

            AudioSource audioSource = targetObj.GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = Undo.AddComponent<AudioSource>(targetObj);
                audioSource.playOnAwake = false;
            }
            trigger.voiceAudioSource = audioSource;

            Selection.activeGameObject = targetObj;
            EditorSceneManager.MarkSceneDirty(targetObj.scene);

            EditorUtility.DisplayDialog("Gempa Susulan Ditempelkan!",
                $"Komponen AftershockTrigger berhasil dipasang langsung pada '{targetObj.name}'!\n\n" +
                "• Mode: Radius Jarak 2.5m (Horizontal 2D)\n" +
                "• 100% PASTI TERPICU saat anak berjalan melewati pintu kelas, tanpa resiko meleset dari collider!\n" +
                "• (Opsional) Pasang file 'VO_09b_Level3_GempaSusulan.wav' ke slot Voice Over Clip.", "Mantap!");
            return;
        }

        // Fallback jika objek WP_03 tidak ditemukan
        Vector3 position = SceneView.lastActiveSceneView != null ? SceneView.lastActiveSceneView.pivot : Vector3.zero;
        GameObject triggerObj = new GameObject("Trigger_Gempa_Susulan");
        Undo.RegisterCreatedObjectUndo(triggerObj, "Pasang Trigger Gempa Susulan");
        triggerObj.transform.position = position;

        AftershockTrigger trig = triggerObj.AddComponent<AftershockTrigger>();
        trig.targetLevel = 3;
        trig.onlyActiveInLevel3 = false;
        trig.useDistanceCheck = true;
        trig.triggerRadius = 2.5f;
        trig.aftershockDuration = 5f;
        trig.shakeAmplitude = 0.08f;

        Selection.activeGameObject = triggerObj;
        EditorUtility.DisplayDialog("Trigger Gempa Susulan Terpasang",
            "Objek 'Trigger_Gempa_Susulan' berhasil dibuat dengan radius deteksi 2.5m.", "OK");
    }

    // [MenuItem("Tools/Level 3: Buat Waypoints Jalur Evakuasi Manual (Bebas Tembus Tembok)")]
    public static void GenerateEvacuationWaypoints()
    {
        EvacuationGuide guide = Object.FindFirstObjectByType<EvacuationGuide>(FindObjectsInactive.Include);
        if (guide != null)
        {
            Undo.RecordObject(guide.gameObject, "Buat Waypoints Jalur Evakuasi");
            guide.AutoPopulateWaypointsIfEmpty();
            Selection.activeGameObject = guide.gameObject;
            
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            EditorUtility.DisplayDialog("Waypoint Selesai Dibuat!",
                "Titik-titik belokan (waypoints) evakuasi telah dibuat di bawah objek 'Jalur_Evakuasi'!\n\n" +
                "• Jalur 100% mengikuti titik-titik ini.\n" +
                "• Kamu bisa menggeser tiap titik (WP_01 s/d WP_06) langsung di jendela Scene View Unity menggunakan Move Tool (W).", "Mantap!");
        }
    }

    // ==========================================
    // KARAKTER: PERBAIKI COLLIDER & GERAKAN BOY
    // ==========================================
    // [MenuItem("Tools/Karakter: Perbaiki Collider & Gerakan Boy (Reset & Sembuhkan)")]
    public static void FixBoyControllerAndMovement()
    {
        BoyController boy = Object.FindFirstObjectByType<BoyController>(FindObjectsInactive.Include);
        if (boy == null)
        {
            EditorUtility.DisplayDialog("Tidak Ditemukan", "Objek BoyController tidak ditemukan di scene.", "OK");
            return;
        }

        Undo.RecordObject(boy.gameObject, "Perbaiki Karakter Boy");

        CharacterController cc = boy.GetComponent<CharacterController>();
        if (cc != null)
        {
            Undo.RecordObject(cc, "Perbaiki CharacterController");
            cc.height = 0.99f;
            cc.center = new Vector3(0f, 0.52f, 0f);
            cc.radius = 0.22f;
            cc.stepOffset = 0.25f;
            cc.skinWidth = 0.02f;
            cc.minMoveDistance = 0.001f;
            EditorUtility.SetDirty(cc);
        }

        Animator anim = boy.GetComponentInChildren<Animator>();
        if (anim != null)
        {
            Undo.RecordObject(anim, "Nonaktifkan Root Motion");
            anim.applyRootMotion = false;
            EditorUtility.SetDirty(anim);
        }

        SerializedObject so = new SerializedObject(boy);
        SerializedProperty spSpeed = so.FindProperty("moveSpeed");
        if (spSpeed != null && spSpeed.floatValue < 3.5f)
        {
            spSpeed.floatValue = 4f;
        }
        SerializedProperty spCrouchH = so.FindProperty("crouchHeight");
        if (spCrouchH != null)
        {
            spCrouchH.floatValue = 0.52f;
        }
        SerializedProperty spCrouchY = so.FindProperty("crouchCenterY");
        if (spCrouchY != null)
        {
            spCrouchY.floatValue = 0.30f;
        }
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(boy);

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(boy.gameObject.scene);

        EditorUtility.DisplayDialog("Karakter Boy Berhasil Disembuhkan!",
            "Pengaturan karakter telah diperbaiki:\n\n" +
            "1. Collider Height: 0.99m (tidak lagi 0 atau amblas ke lantai)\n" +
            "2. Radius & SkinWidth: 0.22m & 0.02m (ramping, tidak gampang nyangkut)\n" +
            "3. Root Motion: Dinonaktifkan (animator tidak lagi mengunci langkah)\n" +
            "4. Kecepatan Jalan: 4 m/s (responsif dan lincah)\n" +
            "5. Kamera & Joystick: Otomatis terhubung\n\n" +
            "Silakan Play dan tes jalan karakter sekarang!", "Mantap!");
    }

    // ==========================================
    // KARAKTER: PASANG MATERIAL & TEKSTUR BOY
    // ==========================================
    // [MenuItem("Tools/Karakter: Pasang Material & Tekstur Boy (Auto-Setup)")]
    public static void SetupBoyMaterialAndTexture()
    {
        string texturePath = "Assets/Textures/Mat_Boy_BaseColor_Clean.png";
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        if (tex == null)
        {
            AssetDatabase.Refresh();
            tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        }

        string matPath = "Assets/Materials/Mat_Boy.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null)
        {
            Shader urpShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Universal Render Pipeline/Simple Lit") ?? Shader.Find("Standard");
            mat = new Material(urpShader) { name = "Mat_Boy" };
            AssetDatabase.CreateAsset(mat, matPath);
        }

        if (tex != null)
        {
            mat.SetTexture("_BaseMap", tex);
            mat.SetTexture("_MainTex", tex);
            mat.color = Color.white;
            EditorUtility.SetDirty(mat);
        }

        // Cari Boy di scene dan pasang material
        int appliedCount = 0;
        foreach (var smr in Object.FindObjectsByType<SkinnedMeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            string sName = smr.name.ToLower();
            string pName = smr.transform.parent != null ? smr.transform.parent.name.ToLower() : "";
            string rName = smr.transform.root != null ? smr.transform.root.name.ToLower() : "";
            if (sName.Contains("cartoon") || sName.Contains("boy") || pName.Contains("boy") || rName.Contains("boy"))
            {
                Undo.RecordObject(smr, "Assign Boy Material");
                smr.sharedMaterial = mat;
                EditorUtility.SetDirty(smr);
                appliedCount++;
            }
        }

        foreach (var go in Selection.gameObjects)
        {
            foreach (var smr in go.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                Undo.RecordObject(smr, "Assign Boy Material");
                smr.sharedMaterial = mat;
                EditorUtility.SetDirty(smr);
                appliedCount++;
            }
        }

        if (appliedCount > 0)
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            EditorUtility.DisplayDialog("Sukses!", $"Material '{mat.name}' berhasil dipasang ke {appliedCount} objek karakter Boy!\nSekarang karakter sudah berwarna rapi.", "Mantap");
        }
        else
        {
            EditorUtility.DisplayDialog("Material Siap", $"Material '{mat.name}' siap di '{matPath}'. Silakan pilih karakter Boy di Hierarchy lalu klik menu ini lagi.", "OK");
        }
        AssetDatabase.SaveAssets();
    }

    // ==========================================
    // LEVEL 3: BAKE OTOMATIS NAVMESH DENAH
    // ==========================================
    [MenuItem("Tools/Simulasi Gempa/Level 3: Bake NavMesh Jalur Evakuasi")]
    public static void BakeSchoolNavMesh()
    {
        // Cari objek denah sekolah di scene
        GameObject denah = GameObject.Find("denah_tes") ?? GameObject.Find("denah_lengkap");
        if (denah == null)
        {
            denah = GameObject.Find("Environment_Buildings_Baru") ?? GameObject.Find("denah");
        }

        if (denah == null)
        {
            EditorUtility.DisplayDialog("Perhatian", "Objek denah sekolah tidak ditemukan di scene saat ini.\nBuka scene LECVEL terlebih dahulu.", "OK");
            return;
        }

        var navSurfaceType = System.Type.GetType("Unity.AI.Navigation.NavMeshSurface, Unity.AI.Navigation");
        if (navSurfaceType != null)
        {
            Component surface = denah.GetComponent(navSurfaceType);
            if (surface == null)
            {
                surface = denah.AddComponent(navSurfaceType);
            }
            var buildNavMeshMethod = navSurfaceType.GetMethod("BuildNavMesh");
            if (buildNavMeshMethod != null)
            {
                buildNavMeshMethod.Invoke(surface, null);
                EditorUtility.DisplayDialog("Sukses!", "NavMesh denah sekolah berhasil di-Bake!\n\nGaris penunjuk jalur evakuasi Level 3 sekarang akan berbelok mulus mengikuti pintu kelas menuju lapangan, tidak menembus tembok.", "Mantap");
                return;
            }
        }
        UnityEditor.AI.NavMeshBuilder.BuildNavMesh();
        EditorUtility.DisplayDialog("Sukses!", "NavMesh berhasil di-Bake!", "OK");
    }

    [MenuItem("Tools/Simulasi Gempa/Level 3: Hapus NavMesh Biru Muda Permanen")]
    public static void RemoveNavMeshPermanently()
    {
        // 1. Hapus objek NavMeshSurface di scene
        GameObject envSurface = GameObject.Find("NavMeshSurface_Environment");
        if (envSurface != null)
        {
            Undo.DestroyObjectImmediate(envSurface);
        }

        var navSurfaceType = System.Type.GetType("Unity.AI.Navigation.NavMeshSurface, Unity.AI.Navigation");
        if (navSurfaceType != null)
        {
            var surfaces = Object.FindObjectsByType(navSurfaceType, FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var s in surfaces)
            {
                if (s is Component c && c != null)
                {
                    Undo.DestroyObjectImmediate(c.gameObject.name.Contains("NavMesh") ? (Object)c.gameObject : c);
                }
            }
        }

        // 2. Bersihkan NavMesh cache
        UnityEditor.AI.NavMeshBuilder.ClearAllNavMeshes();

        if (!Application.isPlaying)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
        }

        EditorUtility.DisplayDialog("NavMesh Berhasil Dihapus Permanen",
            "Objek NavMeshSurface dan lapisan biru muda berhasil dihapus permanen dari scene!\n\n" +
            "Jalur evakuasi tetap berjalan 100% menggunakan titik waypoint manual (WP 1 - WP 6) yang sudah kamu buat.",
            "Mantap");
    }

    [MenuItem("Tools/Simulasi Gempa/Level 3: Setup Panah Hijau & Bintang Evakuasi")]
    public static void SetupChevronArrowsAndStarsMenu()
    {
        SetupChevronArrowsAndStars(true);
    }

    public static void SetupChevronArrowsAndStars(bool showDialog = false)
    {
        EvacuationGuide guide = Object.FindFirstObjectByType<EvacuationGuide>(FindObjectsInactive.Include);
        if (guide == null)
        {
            if (showDialog) EditorUtility.DisplayDialog("Info", "EvacuationGuide tidak ditemukan di scene.", "OK");
            return;
        }

        Undo.RecordObject(guide, "Setup Panah Hijau & Bintang Evakuasi");

        Material matChevron = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_EvacuationChevron_Strip.mat");
        Material matStar = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_EvacuationStar_Glowing.mat");
        AudioClip sfxCollect = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/sfx_bag_pickup.wav");

        // 1. Setup LineRenderer pada Jalur_Evakuasi
        LineRenderer line = guide.GetComponent<LineRenderer>();
        if (line != null)
        {
            Undo.RecordObject(line, "Setup LineRenderer Panah Hijau");
            line.sharedMaterial = matChevron;
            line.alignment = LineAlignment.View;
            line.textureMode = LineTextureMode.Tile;
            line.numCornerVertices = 4;
            line.numCapVertices = 4;
            line.startWidth = 0.75f;
            line.endWidth = 0.75f;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        guide.transform.rotation = Quaternion.identity;

        // 2. Setup Grup Bintang Evakuasi
        Transform tStarsGroup = guide.transform.Find("Bintang_Evakuasi_Group");
        if (tStarsGroup != null)
        {
            Undo.DestroyObjectImmediate(tStarsGroup.gameObject);
        }

        GameObject starsGroupObj = new GameObject("Bintang_Evakuasi_Group");
        starsGroupObj.transform.SetParent(guide.transform, false);
        Undo.RegisterCreatedObjectUndo(starsGroupObj, "Create Bintang Evakuasi Group");

        // Ambil posisi waypoints
        List<Vector3> starPositions = new List<Vector3>();
        for (int i = 0; i < guide.transform.childCount; i++)
        {
            Transform child = guide.transform.GetChild(i);
            if (child.name.StartsWith("WP_"))
            {
                starPositions.Add(child.position);
            }
        }

        GameObject ap = GameObject.Find("Titik_Kumpul_Level3");
        if (ap != null)
        {
            starPositions.Add(ap.transform.position);
        }

        List<EvacuationStar> starList = new List<EvacuationStar>();

        for (int i = 0; i < starPositions.Count; i++)
        {
            Vector3 pos = starPositions[i];
            Vector3 spawnPos = new Vector3(pos.x, 0.85f, pos.z);

            string starName = (i < starPositions.Count - 1) ? $"Bintang_WP_{i + 1:D2}" : "Bintang_TitikKumpul";
            GameObject starObj = new GameObject(starName);
            starObj.transform.SetParent(starsGroupObj.transform, false);
            starObj.transform.position = spawnPos;

            // Quad A
            GameObject qA = GameObject.CreatePrimitive(PrimitiveType.Quad);
            qA.name = "Quad_A";
            qA.transform.SetParent(starObj.transform, false);
            qA.transform.localPosition = Vector3.zero;
            qA.transform.localScale = Vector3.one * 0.75f;
            MeshRenderer rA = qA.GetComponent<MeshRenderer>();
            rA.sharedMaterial = matStar;
            rA.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Object.DestroyImmediate(qA.GetComponent<Collider>());

            // Quad B (Crossed at 90 deg)
            GameObject qB = GameObject.CreatePrimitive(PrimitiveType.Quad);
            qB.name = "Quad_B";
            qB.transform.SetParent(starObj.transform, false);
            qB.transform.localPosition = Vector3.zero;
            qB.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            qB.transform.localScale = Vector3.one * 0.75f;
            MeshRenderer rB = qB.GetComponent<MeshRenderer>();
            rB.sharedMaterial = matStar;
            rB.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Object.DestroyImmediate(qB.GetComponent<Collider>());

            // Point Light untuk efek cahaya berkilau
            GameObject ltObj = new GameObject("Glow_Light");
            ltObj.transform.SetParent(starObj.transform, false);
            ltObj.transform.localPosition = Vector3.zero;
            Light lt = ltObj.AddComponent<Light>();
            lt.type = LightType.Point;
            lt.color = new Color(1f, 0.88f, 0.25f);
            lt.intensity = 1.6f;
            lt.range = 2.8f;
            lt.shadows = LightShadows.None;

            // EvacuationStar Component
            EvacuationStar starComp = starObj.AddComponent<EvacuationStar>();
            starComp.SetAnchorPosition(spawnPos);
            SerializedObject soStar = new SerializedObject(starComp);
            soStar.FindProperty("collectSound").objectReferenceValue = sfxCollect;
            soStar.FindProperty("starLight").objectReferenceValue = lt;
            soStar.FindProperty("pickupRadius").floatValue = 1.4f;
            soStar.FindProperty("bobHeight").floatValue = 0.12f;
            soStar.FindProperty("bobSpeed").floatValue = 2.8f;
            soStar.FindProperty("rotationSpeed").floatValue = 85f;
            soStar.ApplyModifiedProperties();

            starList.Add(starComp);
        }

        // Tautkan ke EvacuationGuide
        SerializedObject soGuide = new SerializedObject(guide);
        SerializedProperty starsProp = soGuide.FindProperty("evacuationStars");
        starsProp.ClearArray();
        for (int i = 0; i < starList.Count; i++)
        {
            starsProp.InsertArrayElementAtIndex(i);
            starsProp.GetArrayElementAtIndex(i).objectReferenceValue = starList[i];
        }
        var pMat = soGuide.FindProperty("arrowMaterial");
        if (pMat != null) pMat.objectReferenceValue = matChevron;
        var pWidth = soGuide.FindProperty("lineWidth");
        if (pWidth != null) pWidth.floatValue = 0.75f;
        var pHeight = soGuide.FindProperty("heightAboveGround");
        if (pHeight != null) pHeight.floatValue = 0.22f;
        var pDensity = soGuide.FindProperty("arrowDensity");
        if (pDensity != null) pDensity.floatValue = 0.35f;
        var pSpeed = soGuide.FindProperty("arrowScrollSpeed");
        if (pSpeed != null) pSpeed.floatValue = 2.2f;
        var pReach = soGuide.FindProperty("waypointReachRadius");
        if (pReach != null) pReach.floatValue = 2.2f;
        soGuide.ApplyModifiedProperties();

        if (!Application.isPlaying)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
        }

        if (showDialog)
        {
            Debug.Log($"[Level23SetupEditor] Sukses! Berhasil menyiapkan panah hijau chevron & {starList.Count} bintang bercahaya untuk Level 3!");
        }
    }

    private static void SetupEvacuationGuide(Transform assemblyPoint)
    {
        EvacuationGuide guide = Object.FindFirstObjectByType<EvacuationGuide>();
        if (guide == null)
        {
            GameObject guideObj = new GameObject("Jalur_Evakuasi", typeof(LineRenderer), typeof(EvacuationGuide));
            Undo.RegisterCreatedObjectUndo(guideObj, "Pasang Jalur Evakuasi");

            LineRenderer line = guideObj.GetComponent<LineRenderer>();
            line.widthMultiplier = 0.25f;
            line.numCornerVertices = 4;
            line.positionCount = 0;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.sharedMaterial = GetOrCreateMaterial("Mat_JalurEvakuasi", new Color(1f, 0.85f, 0.1f));

            guide = guideObj.GetComponent<EvacuationGuide>();
        }

        SerializedObject so = new SerializedObject(guide);
        so.FindProperty("assemblyPoint").objectReferenceValue = assemblyPoint;
        so.ApplyModifiedProperties();
    }

    // ==========================================
    // HELPER
    // ==========================================
    private static bool TryGetBounds(GameObject go, out Bounds bounds)
    {
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
        bounds = new Bounds();
        if (renderers.Length == 0) return false;

        bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }
        return true;
    }

    private static Material GetOrCreateMaterial(string name, Color color)
    {
        string path = $"{MaterialFolder}/{name}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null) return mat;

        if (!AssetDatabase.IsValidFolder(MaterialFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Materials");
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
        mat = new Material(shader) { name = name, color = color };
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    [MenuItem("Tools/Simulasi Gempa/Restyle Level Complete Popup")]
    public static void RestyleLevelCompletePopup()
    {
        MissionUI mui = Object.FindFirstObjectByType<MissionUI>(FindObjectsInactive.Include);
        if (mui == null) return;

        GameObject panel = mui.levelCompletePanel ?? (mui.transform.Find("Popup_LevelComplete") ?? mui.transform.parent?.Find("Popup_LevelComplete"))?.gameObject;
        if (panel == null) return;

        Undo.RecordObject(panel, "Restyle Level Complete Popup");
        panel.SetActive(true);

        Font defaultFont = GetProjectFont();
        Sprite spRoundedFill = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/spr_ui_rounded_fill.png");
        Sprite sprBtnYellow = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/spr_ui_rounded_btn_yellow.png");
        Sprite sprBtnGreen = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/spr_ui_rounded_btn_green.png");

        Color colDarkInk = new Color(0.09f, 0.13f, 0.24f);
        Color colCream = new Color(1.00f, 0.984f, 0.922f);
        Color colOrange = new Color(0.96f, 0.62f, 0.04f);
        Color colGreen = new Color(0.13f, 0.77f, 0.36f);

        RectTransform rtPanel = panel.GetComponent<RectTransform>();
        if (rtPanel != null) SetFullStretch(rtPanel);
        Image imgBackdrop = panel.GetComponent<Image>() ?? panel.AddComponent<Image>();
        imgBackdrop.color = new Color(0.04f, 0.07f, 0.17f, 0.68f);

        Transform tCard = panel.transform.Find("Card");
        GameObject objCard = tCard != null ? tCard.gameObject : new GameObject("Card", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        if (tCard == null) objCard.transform.SetParent(panel.transform, false);
        Undo.RecordObject(objCard, "Setup Card");

        RectTransform rtCard = objCard.GetComponent<RectTransform>();
        rtCard.anchorMin = rtCard.anchorMax = rtCard.pivot = new Vector2(0.5f, 0.5f);
        rtCard.anchoredPosition = new Vector2(0f, -10f);
        rtCard.sizeDelta = new Vector2(1240f, 700f);

        ApplyBorderedCard(objCard, spRoundedFill, colOrange, colCream, 7f);

        // Ribbon Header
        Transform tRibbon = objCard.transform.Find("Ribbon_Header");
        GameObject objRibbon = tRibbon != null ? tRibbon.gameObject : new GameObject("Ribbon_Header", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        if (tRibbon == null) objRibbon.transform.SetParent(objCard.transform, false);
        var olRibbon = objRibbon.GetComponent<Outline>();
        if (olRibbon != null) Object.DestroyImmediate(olRibbon);

        RectTransform rtRibbon = objRibbon.GetComponent<RectTransform>();
        rtRibbon.anchorMin = rtRibbon.anchorMax = new Vector2(0.5f, 1f);
        rtRibbon.pivot = new Vector2(0.5f, 0.5f);
        rtRibbon.anchoredPosition = Vector2.zero;
        rtRibbon.sizeDelta = new Vector2(720f, 84f);

        Image imgRibbon = objRibbon.GetComponent<Image>() ?? objRibbon.AddComponent<Image>();
        if (sprBtnYellow != null) { imgRibbon.sprite = sprBtnYellow; imgRibbon.type = Image.Type.Sliced; imgRibbon.color = Color.white; }
        else imgRibbon.color = colOrange;

        Transform tRibbonTxt = objRibbon.transform.Find("Text");
        GameObject objRibbonTxt = tRibbonTxt != null ? tRibbonTxt.gameObject : new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        if (tRibbonTxt == null) objRibbonTxt.transform.SetParent(objRibbon.transform, false);
        SetFullStretch(objRibbonTxt.GetComponent<RectTransform>());
        Text txtRibbon = objRibbonTxt.GetComponent<Text>();
        txtRibbon.text = "TAHAP SELESAI!";
        txtRibbon.font = defaultFont;
        txtRibbon.fontSize = 34;
        txtRibbon.fontStyle = FontStyle.Bold;
        txtRibbon.alignment = TextAnchor.MiddleCenter;
        txtRibbon.color = colDarkInk;

        // Clear legacy emoji Stars Text & build crisp star sprites
        Transform tStarsLegacy = objCard.transform.Find("Stars");
        if (tStarsLegacy != null) tStarsLegacy.gameObject.SetActive(false);
        BuildStarRow(objCard.transform, "StarsRow", new Vector2(0f, 185f), 80f, 110f);

        // Title
        Transform tTitle = objCard.transform.Find("Title");
        GameObject objTitle = tTitle != null ? tTitle.gameObject : new GameObject("Title", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        if (tTitle == null) objTitle.transform.SetParent(objCard.transform, false);
        RectTransform rtTitle = objTitle.GetComponent<RectTransform>();
        rtTitle.anchorMin = rtTitle.anchorMax = new Vector2(0.5f, 0.5f);
        rtTitle.anchoredPosition = new Vector2(0f, 80f);
        rtTitle.sizeDelta = new Vector2(1080f, 64f);
        Text txtTitle = objTitle.GetComponent<Text>();
        txtTitle.text = "HEBAT SEKALI, ANANDA!";
        txtTitle.font = defaultFont;
        txtTitle.fontSize = 38;
        txtTitle.fontStyle = FontStyle.Bold;
        txtTitle.alignment = TextAnchor.MiddleCenter;
        txtTitle.color = colDarkInk;

        // Desc
        Transform tDesc = objCard.transform.Find("Desc");
        GameObject objDesc = tDesc != null ? tDesc.gameObject : new GameObject("Desc", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        if (tDesc == null) objDesc.transform.SetParent(objCard.transform, false);
        RectTransform rtDesc = objDesc.GetComponent<RectTransform>();
        rtDesc.anchorMin = rtDesc.anchorMax = new Vector2(0.5f, 0.5f);
        rtDesc.anchoredPosition = new Vector2(0f, -20f);
        rtDesc.sizeDelta = new Vector2(1040f, 110f);
        Text txtDesc = objDesc.GetComponent<Text>();
        txtDesc.text = "Bagus sekali! Tahap ini berhasil kamu selesaikan dengan tertib dan mandiri!";
        txtDesc.font = defaultFont;
        txtDesc.fontSize = 26;
        txtDesc.fontStyle = FontStyle.Normal;
        txtDesc.alignment = TextAnchor.MiddleCenter;
        txtDesc.color = new Color(0.20f, 0.26f, 0.35f);

        // Next Level Button
        Transform tBtn = objCard.transform.Find("Btn_NextLevel");
        GameObject objBtn = tBtn != null ? tBtn.gameObject : new GameObject("Btn_NextLevel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        if (tBtn == null) objBtn.transform.SetParent(objCard.transform, false);
        StyleActionButton(objBtn.transform, "LANJUT KE LEVEL BERIKUTNYA", sprBtnGreen, spRoundedFill, colGreen, Color.white, 28, defaultFont, new Vector2(0f, -195f), new Vector2(620f, 92f));
        Button btnNext = objBtn.GetComponent<Button>() ?? objBtn.AddComponent<Button>();

        mui.levelCompletePanel = panel;
        mui.levelCompleteTitleText = txtTitle;
        mui.levelCompleteDescText = txtDesc;
        mui.nextLevelButton = btnNext;

        panel.SetActive(false);
        EditorUtility.SetDirty(panel);
        EditorUtility.SetDirty(mui);
    }

    [MenuItem("Tools/Simulasi Gempa/Restyle Final Score Popup")]
    public static void RestyleFinalScorePopup()
    {
        MissionUI mui = Object.FindFirstObjectByType<MissionUI>(FindObjectsInactive.Include);
        if (mui == null) return;

        GameObject panel = mui.finalScorePanel ?? (mui.transform.Find("Popup_FinalScore") ?? mui.transform.parent?.Find("Popup_FinalScore"))?.gameObject;
        if (panel == null) return;

        Undo.RecordObject(panel, "Restyle Final Score Popup");
        panel.SetActive(true);

        Font defaultFont = GetProjectFont();
        Sprite spRoundedFill = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/spr_ui_rounded_fill.png");
        Sprite sprBtnYellow = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/spr_ui_rounded_btn_yellow.png");
        Sprite sprBtnGreen = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/spr_ui_rounded_btn_green.png");
        Sprite sprBtnWhite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/spr_ui_rounded_btn_white.png");

        Color colDarkInk = new Color(0.09f, 0.13f, 0.24f);
        Color colCream = new Color(1.00f, 0.984f, 0.922f);
        Color colOrange = new Color(0.96f, 0.62f, 0.04f);
        Color colGreen = new Color(0.13f, 0.77f, 0.36f);
        Color colSubtle = new Color(0.82f, 0.85f, 0.90f);

        RectTransform rtPanel = panel.GetComponent<RectTransform>();
        if (rtPanel != null) SetFullStretch(rtPanel);
        Image imgBackdrop = panel.GetComponent<Image>() ?? panel.AddComponent<Image>();
        imgBackdrop.color = new Color(0.04f, 0.07f, 0.17f, 0.70f);

        Transform tCard = panel.transform.Find("Card");
        GameObject objCard = tCard != null ? tCard.gameObject : new GameObject("Card", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        if (tCard == null) objCard.transform.SetParent(panel.transform, false);
        Undo.RecordObject(objCard, "Setup Card");

        Transform oldTime = objCard.transform.Find("TimeText");
        if (oldTime != null) Object.DestroyImmediate(oldTime.gameObject);
        Transform oldEval = objCard.transform.Find("EvalText");
        if (oldEval != null) Object.DestroyImmediate(oldEval.gameObject);

        RectTransform rtCard = objCard.GetComponent<RectTransform>();
        rtCard.anchorMin = rtCard.anchorMax = rtCard.pivot = new Vector2(0.5f, 0.5f);
        rtCard.anchoredPosition = new Vector2(0f, -10f);
        rtCard.sizeDelta = new Vector2(1460f, 900f);

        ApplyBorderedCard(objCard, spRoundedFill, colOrange, colCream, 7f);

        // Ribbon Header
        Transform tRibbon = objCard.transform.Find("Ribbon_Header");
        GameObject objRibbon = tRibbon != null ? tRibbon.gameObject : new GameObject("Ribbon_Header", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        if (tRibbon == null) objRibbon.transform.SetParent(objCard.transform, false);
        var olRibbon = objRibbon.GetComponent<Outline>();
        if (olRibbon != null) Object.DestroyImmediate(olRibbon);

        RectTransform rtRibbon = objRibbon.GetComponent<RectTransform>();
        rtRibbon.anchorMin = rtRibbon.anchorMax = new Vector2(0.5f, 1f);
        rtRibbon.pivot = new Vector2(0.5f, 0.5f);
        rtRibbon.anchoredPosition = Vector2.zero;
        rtRibbon.sizeDelta = new Vector2(800f, 88f);

        Image imgRibbon = objRibbon.GetComponent<Image>() ?? objRibbon.AddComponent<Image>();
        if (sprBtnYellow != null) { imgRibbon.sprite = sprBtnYellow; imgRibbon.type = Image.Type.Sliced; imgRibbon.color = Color.white; }
        else imgRibbon.color = colOrange;

        Transform tRibbonTxt = objRibbon.transform.Find("Text");
        GameObject objRibbonTxt = tRibbonTxt != null ? tRibbonTxt.gameObject : new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        if (tRibbonTxt == null) objRibbonTxt.transform.SetParent(objRibbon.transform, false);
        SetFullStretch(objRibbonTxt.GetComponent<RectTransform>());
        Text txtRibbon = objRibbonTxt.GetComponent<Text>();
        txtRibbon.text = "RAPOR PAHLAWAN CILIK";
        txtRibbon.font = defaultFont;
        txtRibbon.fontSize = 34;
        txtRibbon.fontStyle = FontStyle.Bold;
        txtRibbon.alignment = TextAnchor.MiddleCenter;
        txtRibbon.color = Color.white;

        // 3 Stars Sprites (Tabel 3.7 No. 7)
        Transform tStarsLegacy = objCard.transform.Find("Stars");
        if (tStarsLegacy != null) tStarsLegacy.gameObject.SetActive(false);
        mui.scoreStarsText = tStarsLegacy != null ? tStarsLegacy.GetComponent<Text>() : null;
        mui.finalScoreStarImages = BuildStarRow(objCard.transform, "StarsRow", new Vector2(0f, 305f), 84f, 120f);

        // Title
        Transform tTitle = objCard.transform.Find("Title");
        GameObject objTitle = tTitle != null ? tTitle.gameObject : new GameObject("Title", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        if (tTitle == null) objTitle.transform.SetParent(objCard.transform, false);
        RectTransform rtTitle = objTitle.GetComponent<RectTransform>();
        rtTitle.anchorMin = rtTitle.anchorMax = new Vector2(0.5f, 0.5f);
        rtTitle.anchoredPosition = new Vector2(0f, 205f);
        rtTitle.sizeDelta = new Vector2(1320f, 60f);
        Text txtTitle = objTitle.GetComponent<Text>();
        txtTitle.text = "SELAMAT! KAMU JADI PAHLAWAN CILIK SIAGA GEMPA!";
        txtTitle.font = defaultFont;
        txtTitle.fontSize = 34;
        txtTitle.fontStyle = FontStyle.Bold;
        txtTitle.alignment = TextAnchor.MiddleCenter;
        txtTitle.color = colDarkInk;

        Transform tSub = objCard.transform.Find("Subtitle");
        if (tSub != null) tSub.gameObject.SetActive(false);

        // 3 Kotak Hasil Level (Putih + Border Warna-warni + Teks + Ceklis)
        float[] pX = new float[] { -415f, 0f, 415f };
        Color[] pBorderColors = new Color[]
        {
            new Color(0.13f, 0.77f, 0.36f), // Level 1 Hijau
            new Color(0.85f, 0.47f, 0.02f), // Level 2 Oranye-coklat
            new Color(0.01f, 0.52f, 0.78f)  // Level 3 Biru
        };
        string[] pLabels = new string[] { "LEVEL 1: 30/30", "LEVEL 2: 35/35", "LEVEL 3: 35/35" };
        Text[] pScoreTexts = new Text[3];

        for (int i = 0; i < 3; i++)
        {
            Transform tPillar = objCard.transform.Find($"Pillar_{i + 1}");
            GameObject objPillar = tPillar != null ? tPillar.gameObject : new GameObject($"Pillar_{i + 1}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            if (tPillar == null) objPillar.transform.SetParent(objCard.transform, false);

            RectTransform rtPillar = objPillar.GetComponent<RectTransform>();
            rtPillar.anchorMin = rtPillar.anchorMax = new Vector2(0.5f, 0.5f);
            rtPillar.anchoredPosition = new Vector2(pX[i], 70f);
            rtPillar.sizeDelta = new Vector2(390f, 145f);

            ApplyBorderedCard(objPillar, spRoundedFill, pBorderColors[i], Color.white, 5f);

            Transform legacyBdg = objPillar.transform.Find("Badge");
            if (legacyBdg != null) legacyBdg.gameObject.SetActive(false);
            Transform legacyTit = objPillar.transform.Find("Title");
            if (legacyTit != null) legacyTit.gameObject.SetActive(false);
            Transform legacyChk = objPillar.transform.Find("Check");
            if (legacyChk != null) legacyChk.gameObject.SetActive(false);

            Transform tPScore = objPillar.transform.Find("Score");
            GameObject objPScore = tPScore != null ? tPScore.gameObject : new GameObject("Score", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            if (tPScore == null) objPScore.transform.SetParent(objPillar.transform, false);
            RectTransform rtPScore = objPScore.GetComponent<RectTransform>();
            rtPScore.anchorMin = rtPScore.anchorMax = new Vector2(0.5f, 0.5f);
            rtPScore.anchoredPosition = new Vector2(-18f, 0f);
            rtPScore.sizeDelta = new Vector2(310f, 60f);
            Text txtPScore = objPScore.GetComponent<Text>();
            txtPScore.text = pLabels[i];
            txtPScore.font = defaultFont;
            txtPScore.fontSize = 27;
            txtPScore.fontStyle = FontStyle.Bold;
            txtPScore.alignment = TextAnchor.MiddleCenter;
            txtPScore.color = pBorderColors[i];
            pScoreTexts[i] = txtPScore;

            BuildCheckIcon("CheckIcon", objPillar.transform, new Vector2(142f, 0f), pBorderColors[i]);
        }

        // Banner Total Skor Kelulusan
        Transform tBanner = objCard.transform.Find("Banner_TotalScore");
        GameObject objBanner = tBanner != null ? tBanner.gameObject : new GameObject("Banner_TotalScore", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        if (tBanner == null) objBanner.transform.SetParent(objCard.transform, false);
        RectTransform rtBanner = objBanner.GetComponent<RectTransform>();
        rtBanner.anchorMin = rtBanner.anchorMax = new Vector2(0.5f, 0.5f);
        rtBanner.anchoredPosition = new Vector2(0f, -68f);
        rtBanner.sizeDelta = new Vector2(1220f, 88f);
        ApplyBorderedCard(objBanner, spRoundedFill, colOrange, new Color(1.0f, 0.95f, 0.78f), 4f);

        Transform tBannerTxt = objBanner.transform.Find("Text");
        GameObject objBannerTxt = tBannerTxt != null ? tBannerTxt.gameObject : new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        if (tBannerTxt == null) objBannerTxt.transform.SetParent(objBanner.transform, false);
        SetFullStretch(objBannerTxt.GetComponent<RectTransform>());
        Text txtBanner = objBannerTxt.GetComponent<Text>();
        txtBanner.text = "TOTAL SKOR KELULUSAN: 100 / 100 (SEMPURNA!)";
        txtBanner.font = defaultFont;
        txtBanner.fontSize = 30;
        txtBanner.fontStyle = FontStyle.Bold;
        txtBanner.alignment = TextAnchor.MiddleCenter;
        txtBanner.color = new Color(0.50f, 0.22f, 0.05f);

        // Card Waktu Selesai
        Transform tEvalCard = objCard.transform.Find("Card_Evaluation");
        GameObject objEvalCard = tEvalCard != null ? tEvalCard.gameObject : new GameObject("Card_Evaluation", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        if (tEvalCard == null) objEvalCard.transform.SetParent(objCard.transform, false);
        RectTransform rtEvalCard = objEvalCard.GetComponent<RectTransform>();
        rtEvalCard.anchorMin = rtEvalCard.anchorMax = new Vector2(0.5f, 0.5f);
        rtEvalCard.anchoredPosition = new Vector2(0f, -176f);
        rtEvalCard.sizeDelta = new Vector2(1220f, 88f);
        ApplyBorderedCard(objEvalCard, spRoundedFill, colSubtle, new Color(0.97f, 0.98f, 0.99f), 3.5f);

        Transform tTime = objEvalCard.transform.Find("TimeText");
        GameObject objTime = tTime != null ? tTime.gameObject : new GameObject("TimeText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        if (tTime == null) objTime.transform.SetParent(objEvalCard.transform, false);
        SetFullStretch(objTime.GetComponent<RectTransform>(), 16f);
        Text txtTime = objTime.GetComponent<Text>();
        txtTime.text = "Waktu Selesai: 00:54 Detik • Sangat Tanggap & Mandiri";
        txtTime.font = defaultFont;
        txtTime.fontSize = 24;
        txtTime.fontStyle = FontStyle.Bold;
        txtTime.alignment = TextAnchor.MiddleCenter;
        txtTime.color = new Color(0.18f, 0.24f, 0.33f);

        Transform tEval = objEvalCard.transform.Find("EvalText");
        if (tEval != null) tEval.gameObject.SetActive(false);

        // Action Buttons (Main Lagi & Menu Utama)
        Transform tRetry = objCard.transform.Find("Btn_Retry");
        GameObject objRetry = tRetry != null ? tRetry.gameObject : new GameObject("Btn_Retry", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        if (tRetry == null) objRetry.transform.SetParent(objCard.transform, false);
        StyleActionButton(objRetry.transform, "Main Lagi", sprBtnWhite, spRoundedFill, Color.white, colDarkInk, 30, defaultFont, new Vector2(-260f, -310f), new Vector2(420f, 92f));
        Button btnRetry = objRetry.GetComponent<Button>() ?? objRetry.AddComponent<Button>();

        Transform tMenu = objCard.transform.Find("Btn_MainMenu");
        GameObject objMenu = tMenu != null ? tMenu.gameObject : new GameObject("Btn_MainMenu", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        if (tMenu == null) objMenu.transform.SetParent(objCard.transform, false);
        StyleActionButton(objMenu.transform, "Menu Utama", sprBtnGreen, spRoundedFill, colGreen, Color.white, 30, defaultFont, new Vector2(260f, -310f), new Vector2(420f, 92f));
        Button btnMenu = objMenu.GetComponent<Button>() ?? objMenu.AddComponent<Button>();

        mui.finalScorePanel = panel;
        mui.scoreTimeText = txtTime;
        mui.scorePointsText = txtBanner;
        mui.scoreEvaluationText = null;
        mui.scorePillar1Text = pScoreTexts[0];
        mui.scorePillar2Text = pScoreTexts[1];
        mui.scorePillar3Text = pScoreTexts[2];
        mui.retryButton = btnRetry;
        mui.mainMenuButton = btnMenu;

        panel.SetActive(false);
        EditorUtility.SetDirty(panel);
        EditorUtility.SetDirty(mui);
    }

    #region Word UI Visual Helpers

    private static Font GetProjectFont()
    {
        return AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Fredoka-Variable.ttf")
            ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
            ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
    }

    private static void SetFullStretch(RectTransform rt, float inset = 0f)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(inset, inset);
        rt.offsetMax = new Vector2(-inset, -inset);
    }

    private static void ApplyBorderedCard(GameObject outer, Sprite spFill, Color borderColor, Color fillColor, float thickness)
    {
        var ol = outer.GetComponent<Outline>();
        if (ol != null) Object.DestroyImmediate(ol);

        Image outerImg = outer.GetComponent<Image>() ?? outer.AddComponent<Image>();
        if (spFill != null) { outerImg.sprite = spFill; outerImg.type = Image.Type.Sliced; }
        outerImg.color = borderColor;

        Transform tFill = outer.transform.Find("Card_Fill");
        GameObject fillObj = tFill != null ? tFill.gameObject : new GameObject("Card_Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        if (tFill == null) fillObj.transform.SetParent(outer.transform, false);
        fillObj.transform.SetSiblingIndex(0);
        SetFullStretch(fillObj.GetComponent<RectTransform>(), thickness);
        Image fillImg = fillObj.GetComponent<Image>();
        if (spFill != null) { fillImg.sprite = spFill; fillImg.type = Image.Type.Sliced; }
        fillImg.color = fillColor;
        fillImg.raycastTarget = false;
    }

    private static void StylePauseLabel(Transform tLbl, string labelText, Font font, Color col, Vector2 pos, Vector2 size)
    {
        if (tLbl == null) return;
        RectTransform rt = tLbl as RectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        Text txt = tLbl.GetComponent<Text>();
        if (txt != null)
        {
            txt.text = labelText;
            txt.font = font;
            txt.fontSize = 30;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleLeft;
            txt.color = col;
        }
    }

    private static void StyleCapsuleSlider(Slider sld, Sprite spFill, Color fillColor, Vector2 pos, Vector2 size)
    {
        RectTransform rt = sld.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        sld.transition = Selectable.Transition.None;

        Transform tBg = sld.transform.Find("Background");
        if (tBg != null)
        {
            SetFullStretch(tBg.GetComponent<RectTransform>());
            var bgOl = tBg.GetComponent<Outline>();
            if (bgOl != null) Object.DestroyImmediate(bgOl);
            Image bgImg = tBg.GetComponent<Image>();
            if (bgImg != null)
            {
                if (spFill != null) { bgImg.sprite = spFill; bgImg.type = Image.Type.Sliced; }
                bgImg.color = new Color(0.88f, 0.91f, 0.95f);
            }
        }

        Transform tFillArea = sld.transform.Find("Fill Area");
        if (tFillArea != null)
        {
            SetFullStretch(tFillArea.GetComponent<RectTransform>());
            Transform tFill = tFillArea.Find("Fill");
            if (tFill != null)
            {
                SetFullStretch(tFill.GetComponent<RectTransform>());
                Image fImg = tFill.GetComponent<Image>();
                if (fImg != null)
                {
                    if (spFill != null) { fImg.sprite = spFill; fImg.type = Image.Type.Sliced; }
                    fImg.color = fillColor;
                }
            }
        }

        Transform tHandleArea = sld.transform.Find("Handle Slide Area");
        if (tHandleArea != null)
        {
            Transform tHandle = tHandleArea.Find("Handle");
            if (tHandle != null)
            {
                Image hImg = tHandle.GetComponent<Image>();
                if (hImg != null) hImg.color = Color.clear;
            }
        }
    }

    private static void StyleActionButton(Transform tBtn, string label, Sprite preferred, Sprite fallback, Color tint, Color textColor, int fontSize, Font font, Vector2 pos, Vector2 size)
    {
        if (tBtn == null) return;
        RectTransform rt = tBtn as RectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        var oldOl = tBtn.GetComponent<Outline>();
        if (oldOl != null) Object.DestroyImmediate(oldOl);

        Image img = tBtn.GetComponent<Image>() ?? tBtn.gameObject.AddComponent<Image>();
        Sprite s = preferred ?? fallback;
        if (s != null) { img.sprite = s; img.type = Image.Type.Sliced; }
        img.color = (preferred != null && preferred != fallback) ? Color.white : tint;

        Transform tTxt = tBtn.Find("Text");
        GameObject objTxt = tTxt != null ? tTxt.gameObject : new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        if (tTxt == null) objTxt.transform.SetParent(tBtn, false);
        SetFullStretch(objTxt.GetComponent<RectTransform>());
        Text txt = objTxt.GetComponent<Text>();
        txt.text = label;
        txt.font = font;
        txt.fontSize = fontSize;
        txt.fontStyle = FontStyle.Bold;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = textColor;
    }

    private static GameObject BuildCheckIcon(string name, Transform parent, Vector2 pos, Color col)
    {
        Transform existing = parent.Find(name);
        if (existing != null) Object.DestroyImmediate(existing.gameObject);

        GameObject root = new GameObject(name, typeof(RectTransform));
        root.transform.SetParent(parent, false);
        RectTransform r = root.GetComponent<RectTransform>();
        r.anchoredPosition = pos;
        r.sizeDelta = new Vector2(30f, 30f);

        GameObject s1 = new GameObject("ArmShort", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        s1.transform.SetParent(root.transform, false);
        RectTransform r1 = s1.GetComponent<RectTransform>();
        r1.anchoredPosition = new Vector2(-5f, -3f);
        r1.sizeDelta = new Vector2(14f, 5f);
        r1.localEulerAngles = new Vector3(0f, 0f, -45f);
        Image i1 = s1.GetComponent<Image>(); i1.color = col; i1.raycastTarget = false;

        GameObject s2 = new GameObject("ArmLong", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        s2.transform.SetParent(root.transform, false);
        RectTransform r2 = s2.GetComponent<RectTransform>();
        r2.anchoredPosition = new Vector2(5f, 1f);
        r2.sizeDelta = new Vector2(24f, 5f);
        r2.localEulerAngles = new Vector3(0f, 0f, 45f);
        Image i2 = s2.GetComponent<Image>(); i2.color = col; i2.raycastTarget = false;
        return root;
    }

    private static void CreatePauseBar(Transform parent, string name, Sprite spFill, Vector2 pos)
    {
        Transform existing = parent.Find(name);
        GameObject bar = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        if (existing == null) bar.transform.SetParent(parent, false);
        RectTransform rt = bar.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(11f, 38f);
        Image img = bar.GetComponent<Image>();
        if (spFill != null) { img.sprite = spFill; img.type = Image.Type.Sliced; }
        img.color = Color.white;
        img.raycastTarget = false;
    }

    private static void BuildSpeakerShape(Transform parent, Color col)
    {
        for (int i = parent.childCount - 1; i >= 0; i--) Object.DestroyImmediate(parent.GetChild(i).gameObject);

        GameObject body = new GameObject("SpkBody", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        body.transform.SetParent(parent, false);
        RectTransform rb = body.GetComponent<RectTransform>();
        rb.anchoredPosition = new Vector2(-10f, 0f);
        rb.sizeDelta = new Vector2(10f, 14f);
        body.GetComponent<Image>().color = col;

        GameObject cone = new GameObject("SpkCone", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        cone.transform.SetParent(parent, false);
        RectTransform rc = cone.GetComponent<RectTransform>();
        rc.anchoredPosition = new Vector2(-2f, 0f);
        rc.sizeDelta = new Vector2(12f, 24f);
        cone.GetComponent<Image>().color = col;

        GameObject wave1 = new GameObject("Wave1", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        wave1.transform.SetParent(parent, false);
        RectTransform rw1 = wave1.GetComponent<RectTransform>();
        rw1.anchoredPosition = new Vector2(10f, 0f);
        rw1.sizeDelta = new Vector2(4f, 14f);
        wave1.GetComponent<Image>().color = new Color(0.45f, 0.72f, 0.96f);

        GameObject wave2 = new GameObject("Wave2", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        wave2.transform.SetParent(parent, false);
        RectTransform rw2 = wave2.GetComponent<RectTransform>();
        rw2.anchoredPosition = new Vector2(17f, 0f);
        rw2.sizeDelta = new Vector2(4f, 24f);
        wave2.GetComponent<Image>().color = new Color(0.45f, 0.72f, 0.96f);
    }

    private static Image[] BuildStarRow(Transform parent, string name, Vector2 pos, float starSize, float spacing)
    {
        Transform existing = parent.Find(name);
        GameObject row = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform));
        if (existing == null) row.transform.SetParent(parent, false);
        RectTransform rRow = row.GetComponent<RectTransform>();
        rRow.anchorMin = rRow.anchorMax = new Vector2(0.5f, 0.5f);
        rRow.anchoredPosition = pos;
        rRow.sizeDelta = new Vector2(spacing * 3f + starSize, starSize + 10f);

        Sprite spStar = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/tex_star_glowing.png");
        Image[] imgs = new Image[3];
        for (int i = 0; i < 3; i++)
        {
            Transform ts = row.transform.Find($"Star_{i}");
            GameObject go = ts != null ? ts.gameObject : new GameObject($"Star_{i}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            if (ts == null) go.transform.SetParent(row.transform, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2((i - 1) * spacing, 0f);
            rt.sizeDelta = new Vector2(starSize, starSize);
            Image img = go.GetComponent<Image>();
            if (spStar != null) img.sprite = spStar;
            img.color = Color.white;
            img.preserveAspect = true;
            img.raycastTarget = false;
            imgs[i] = img;
        }
        return imgs;
    }

    #endregion
}
#endif

