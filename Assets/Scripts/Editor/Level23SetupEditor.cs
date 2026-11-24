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
            RestyleLevelCompletePopup();
            RestyleFinalScorePopup();
            SetupChevronArrowsAndStars(false);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[Level23SetupEditor] Auto-setup Kuis TV, Player Name, Kamera, Efek Gempa, Pause HUD, & Figcraft Popups berhasil!");
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
        // Sprites are already configured as 2D Sprites in project

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

        Undo.RecordObject(panelQuiz, "Restyle Quiz UI to Figcraft");
        panelQuiz.SetActive(true);

        Font defaultFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Fredoka-Variable.ttf")
            ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") 
            ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

        RectTransform rtPanel = panelQuiz.GetComponent<RectTransform>();
        if (rtPanel != null)
        {
            rtPanel.anchorMin = Vector2.zero;
            rtPanel.anchorMax = Vector2.one;
            rtPanel.offsetMin = Vector2.zero;
            rtPanel.offsetMax = Vector2.zero;
        }

        Image panelImg = panelQuiz.GetComponent<Image>();
        if (panelImg == null) panelImg = panelQuiz.AddComponent<Image>();
        panelImg.color = new Color(0.08f, 0.11f, 0.18f, 0.72f); // Dark translucent backdrop

        // -------------------------------------------------------------
        // 1. BU GURU FLOATING CARD (SLIDE IN DARI KIRI)
        // -------------------------------------------------------------
        Transform tGuru = panelQuiz.transform.Find("Card_BuGuru");
        GameObject cardGuru = tGuru != null ? tGuru.gameObject : new GameObject("Card_BuGuru", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
        if (tGuru == null)
        {
            cardGuru.transform.SetParent(panelQuiz.transform, false);
            cardGuru.transform.SetSiblingIndex(0);
        }
        Undo.RecordObject(cardGuru, "Setup Card Bu Guru");

        RectTransform rtGuru = cardGuru.GetComponent<RectTransform>();
        rtGuru.anchorMin = new Vector2(0f, 0.5f);
        rtGuru.anchorMax = new Vector2(0f, 0.5f);
        rtGuru.pivot = new Vector2(0f, 0.5f);
        rtGuru.anchoredPosition = new Vector2(35f, 50f);
        rtGuru.sizeDelta = new Vector2(145f, 220f);

        Sprite sprCardSubtle = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/spr_ui_rounded_card_subtle.png");
        Image bgGuru = cardGuru.GetComponent<Image>() ?? cardGuru.AddComponent<Image>();
        if (sprCardSubtle != null)
        {
            bgGuru.sprite = sprCardSubtle;
            bgGuru.type = Image.Type.Sliced;
            bgGuru.color = Color.white;
            var ol = cardGuru.GetComponent<Outline>();
            if (ol != null) Object.DestroyImmediate(ol);
        }
        else
        {
            bgGuru.color = Color.white;
            Outline outGuru = cardGuru.GetComponent<Outline>() ?? cardGuru.AddComponent<Outline>();
            outGuru.effectColor = new Color(0.82f, 0.85f, 0.90f);
            outGuru.effectDistance = new Vector2(2.5f, -2.5f);
        }

        // Avatar Circle
        Transform tCircle = cardGuru.transform.Find("Avatar_Circle");
        GameObject objCircle = tCircle != null ? tCircle.gameObject : new GameObject("Avatar_Circle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        if (tCircle == null) objCircle.transform.SetParent(cardGuru.transform, false);
        RectTransform rtCircle = objCircle.GetComponent<RectTransform>();
        rtCircle.anchorMin = new Vector2(0.5f, 0.5f);
        rtCircle.anchorMax = new Vector2(0.5f, 0.5f);
        rtCircle.anchoredPosition = new Vector2(0f, 42f);
        rtCircle.sizeDelta = new Vector2(96f, 96f);
        Image imgCircle = objCircle.GetComponent<Image>();
        imgCircle.color = new Color(0.06f, 0.73f, 0.51f); // Emerald Green #10B981

        // Avatar Image
        Transform tAva = objCircle.transform.Find("Image_Avatar");
        GameObject objAva = tAva != null ? tAva.gameObject : new GameObject("Image_Avatar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        if (tAva == null) objAva.transform.SetParent(objCircle.transform, false);
        RectTransform rtAva = objAva.GetComponent<RectTransform>();
        rtAva.anchorMin = Vector2.zero;
        rtAva.anchorMax = Vector2.one;
        rtAva.offsetMin = Vector2.zero;
        rtAva.offsetMax = Vector2.zero;
        Image imgAva = objAva.GetComponent<Image>();
        imgAva.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/avatar_guru.png");
        imgAva.color = Color.white;
        imgAva.preserveAspect = true;

        // Text Bu Guru
        Transform tName = cardGuru.transform.Find("Text_Name");
        GameObject objName = tName != null ? tName.gameObject : new GameObject("Text_Name", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        if (tName == null) objName.transform.SetParent(cardGuru.transform, false);
        RectTransform rtName = objName.GetComponent<RectTransform>();
        rtName.anchorMin = new Vector2(0.5f, 0.5f);
        rtName.anchorMax = new Vector2(0.5f, 0.5f);
        rtName.anchoredPosition = new Vector2(0f, -22f);
        rtName.sizeDelta = new Vector2(130f, 26f);
        Text txtName = objName.GetComponent<Text>();
        txtName.text = "Bu Guru";
        txtName.font = defaultFont;
        txtName.fontSize = 17;
        txtName.fontStyle = FontStyle.Bold;
        txtName.alignment = TextAnchor.MiddleCenter;
        txtName.color = new Color(0.06f, 0.09f, 0.16f);

        // Icon Speaker
        Transform tSpeaker = cardGuru.transform.Find("Icon_Speaker");
        GameObject objSpeaker = tSpeaker != null ? tSpeaker.gameObject : new GameObject("Icon_Speaker", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        if (tSpeaker == null) objSpeaker.transform.SetParent(cardGuru.transform, false);
        RectTransform rtSpeaker = objSpeaker.GetComponent<RectTransform>();
        rtSpeaker.anchorMin = new Vector2(0.5f, 0.5f);
        rtSpeaker.anchorMax = new Vector2(0.5f, 0.5f);
        rtSpeaker.anchoredPosition = new Vector2(0f, -50f);
        rtSpeaker.sizeDelta = new Vector2(50f, 26f);
        Text txtSpeaker = objSpeaker.GetComponent<Text>();
        txtSpeaker.text = "🔊";
        txtSpeaker.font = defaultFont;
        txtSpeaker.fontSize = 18;
        txtSpeaker.alignment = TextAnchor.MiddleCenter;
        txtSpeaker.color = new Color(0.2f, 0.25f, 0.35f);

        // -------------------------------------------------------------
        // 2. CARD QUIZ (NOTEBOOK LOOSE LEAF)
        // -------------------------------------------------------------
        Transform tQuiz = panelQuiz.transform.Find("Card_Quiz");
        GameObject cardQuiz = tQuiz != null ? tQuiz.gameObject : new GameObject("Card_Quiz", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
        if (tQuiz == null) cardQuiz.transform.SetParent(panelQuiz.transform, false);
        Undo.RecordObject(cardQuiz, "Setup Card Quiz Notebook");

        RectTransform rtQuiz = cardQuiz.GetComponent<RectTransform>();
        rtQuiz.anchorMin = new Vector2(0.5f, 0.5f);
        rtQuiz.anchorMax = new Vector2(0.5f, 0.5f);
        rtQuiz.pivot = new Vector2(0.5f, 0.5f);
        rtQuiz.anchoredPosition = new Vector2(70f, 0f);
        rtQuiz.sizeDelta = new Vector2(1420f, 820f); // 3/4 Layar (1080p canvas)

        Sprite sprCardQuiz = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/spr_ui_rounded_card_parchment.png");
        Image bgQuiz = cardQuiz.GetComponent<Image>() ?? cardQuiz.AddComponent<Image>();
        if (sprCardQuiz != null)
        {
            bgQuiz.sprite = sprCardQuiz;
            bgQuiz.type = Image.Type.Sliced;
            bgQuiz.color = Color.white;
            var ol = cardQuiz.GetComponent<Outline>();
            if (ol != null) Object.DestroyImmediate(ol);
        }
        else
        {
            bgQuiz.color = Color.white;
            Outline outQuiz = cardQuiz.GetComponent<Outline>() ?? cardQuiz.AddComponent<Outline>();
            outQuiz.effectColor = new Color(0.82f, 0.85f, 0.90f);
            outQuiz.effectDistance = new Vector2(3.5f, -3.5f);
        }

        // 6 Binder Holes
        Transform tHoles = cardQuiz.transform.Find("Binder_Holes");
        GameObject objHoles = tHoles != null ? tHoles.gameObject : new GameObject("Binder_Holes", typeof(RectTransform));
        if (tHoles == null) objHoles.transform.SetParent(cardQuiz.transform, false);
        RectTransform rtHoles = objHoles.GetComponent<RectTransform>();
        rtHoles.anchorMin = Vector2.zero;
        rtHoles.anchorMax = Vector2.one;
        rtHoles.offsetMin = Vector2.zero;
        rtHoles.offsetMax = Vector2.zero;

        for (int i = 0; i < 6; i++)
        {
            Transform tHole = objHoles.transform.Find($"Hole_{i}");
            GameObject objHole = tHole != null ? tHole.gameObject : new GameObject($"Hole_{i}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            if (tHole == null) objHole.transform.SetParent(objHoles.transform, false);
            RectTransform rtHole = objHole.GetComponent<RectTransform>();
            rtHole.anchorMin = new Vector2(0.5f, 0.5f);
            rtHole.anchorMax = new Vector2(0.5f, 0.5f);
            rtHole.anchoredPosition = new Vector2(-675f, 280f - i * 110f);
            rtHole.sizeDelta = new Vector2(24f, 24f);
            Image imgHole = objHole.GetComponent<Image>();
            imgHole.color = new Color(0.78f, 0.82f, 0.88f);
        }

        // Red Margin Line
        Transform tLine = cardQuiz.transform.Find("Margin_Line_Red");
        GameObject objLine = tLine != null ? tLine.gameObject : new GameObject("Margin_Line_Red", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        if (tLine == null) objLine.transform.SetParent(cardQuiz.transform, false);
        RectTransform rtLine = objLine.GetComponent<RectTransform>();
        rtLine.anchorMin = new Vector2(0.5f, 0.5f);
        rtLine.anchorMax = new Vector2(0.5f, 0.5f);
        rtLine.anchoredPosition = new Vector2(-640f, 0f);
        rtLine.sizeDelta = new Vector2(3f, 780f);
        Image imgLine = objLine.GetComponent<Image>();
        imgLine.color = new Color(0.94f, 0.27f, 0.27f, 0.75f); // Red margin #EF4444

        // Question Prompt
        Transform tPrompt = cardQuiz.transform.Find("Question_Prompt");
        if (tPrompt == null) tPrompt = cardQuiz.transform.Find("Text_Prompt");
        GameObject objPrompt = tPrompt != null ? tPrompt.gameObject : new GameObject("Question_Prompt", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        objPrompt.name = "Question_Prompt";
        if (tPrompt == null) objPrompt.transform.SetParent(cardQuiz.transform, false);
        RectTransform rtPrompt = objPrompt.GetComponent<RectTransform>();
        rtPrompt.anchorMin = new Vector2(0.5f, 0.5f);
        rtPrompt.anchorMax = new Vector2(0.5f, 0.5f);
        rtPrompt.anchoredPosition = new Vector2(25f, 310f);
        rtPrompt.sizeDelta = new Vector2(1150f, 75f);
        Text txtPrompt = objPrompt.GetComponent<Text>();
        txtPrompt.text = "Perhatikan ketiga gambar rumah! Rumah manakah yang sedang terkena bahaya?";
        txtPrompt.font = defaultFont;
        txtPrompt.fontSize = 28;
        txtPrompt.fontStyle = FontStyle.Bold;
        txtPrompt.alignment = TextAnchor.MiddleCenter;
        txtPrompt.color = new Color(0.09f, 0.13f, 0.24f);

        // Feedback Text
        Transform tFeedback = cardQuiz.transform.Find("FeedbackText");
        if (tFeedback == null) tFeedback = cardQuiz.transform.Find("Text_Feedback");
        GameObject objFeedback = tFeedback != null ? tFeedback.gameObject : new GameObject("FeedbackText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        objFeedback.name = "FeedbackText";
        if (tFeedback == null) objFeedback.transform.SetParent(cardQuiz.transform, false);
        RectTransform rtFeedback = objFeedback.GetComponent<RectTransform>();
        rtFeedback.anchorMin = new Vector2(0.5f, 0.5f);
        rtFeedback.anchorMax = new Vector2(0.5f, 0.5f);
        rtFeedback.anchoredPosition = new Vector2(25f, -225f);
        rtFeedback.sizeDelta = new Vector2(1150f, 40f);
        Text txtFeedback = objFeedback.GetComponent<Text>();
        txtFeedback.text = "";
        txtFeedback.font = defaultFont;
        txtFeedback.fontSize = 22;
        txtFeedback.fontStyle = FontStyle.Bold;
        txtFeedback.alignment = TextAnchor.MiddleCenter;

        // -------------------------------------------------------------
        // 3. 3 POLAROID OPTION CARDS WITH YELLOW TAPE
        // -------------------------------------------------------------
        float[] posX = new float[] { -400f, 25f, 450f };
        float[] tapeRot = new float[] { -3.5f, 1.5f, 3.5f };
        Button[] btns = new Button[3];
        Image[] imgs = new Image[3];
        Text[] txts = new Text[3];
        Outline[] outlines = new Outline[3];

        for (int i = 0; i < 3; i++)
        {
            Transform tOpt = cardQuiz.transform.Find($"Btn_Option_{i}");
            GameObject objOpt = tOpt != null ? tOpt.gameObject : new GameObject($"Btn_Option_{i}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(Outline));
            if (tOpt == null) objOpt.transform.SetParent(cardQuiz.transform, false);
            Undo.RecordObject(objOpt, $"Setup Option {i}");

            RectTransform rtOpt = objOpt.GetComponent<RectTransform>();
            rtOpt.anchorMin = new Vector2(0.5f, 0.5f);
            rtOpt.anchorMax = new Vector2(0.5f, 0.5f);
            rtOpt.anchoredPosition = new Vector2(posX[i], 35f);
            rtOpt.sizeDelta = new Vector2(370f, 390f);

            Sprite sprBtnWhite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/spr_ui_rounded_btn_white.png");
            Image cardImg = objOpt.GetComponent<Image>() ?? objOpt.AddComponent<Image>();
            if (sprBtnWhite != null)
            {
                cardImg.sprite = sprBtnWhite;
                cardImg.type = Image.Type.Sliced;
                cardImg.color = Color.white;
            }
            else
            {
                cardImg.color = Color.white;
            }
            Outline cardOut = objOpt.GetComponent<Outline>() ?? objOpt.AddComponent<Outline>();
            cardOut.effectColor = new Color(0.85f, 0.88f, 0.92f);
            cardOut.effectDistance = new Vector2(3.5f, -3.5f);
            outlines[i] = cardOut;
            btns[i] = objOpt.GetComponent<Button>() ?? objOpt.AddComponent<Button>();

            // Yellow Masking Tape
            Transform tTape = objOpt.transform.Find("Tape_Sticker");
            GameObject objTape = tTape != null ? tTape.gameObject : new GameObject("Tape_Sticker", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            if (tTape == null) objTape.transform.SetParent(objOpt.transform, false);
            RectTransform rtTape = objTape.GetComponent<RectTransform>();
            rtTape.anchorMin = new Vector2(0.5f, 0.5f);
            rtTape.anchorMax = new Vector2(0.5f, 0.5f);
            rtTape.anchoredPosition = new Vector2(0f, 195f);
            rtTape.sizeDelta = new Vector2(110f, 30f);
            rtTape.localEulerAngles = new Vector3(0f, 0f, tapeRot[i]);
            Image imgTape = objTape.GetComponent<Image>();
            imgTape.color = new Color(0.99f, 0.94f, 0.54f, 0.88f); // Pastel yellow tape

            // Image Display (Photo Frame)
            Transform tDisp = objOpt.transform.Find("Image_Display");
            GameObject objDisp = tDisp != null ? tDisp.gameObject : new GameObject("Image_Display", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(AnimatedImageUI));
            if (tDisp == null) objDisp.transform.SetParent(objOpt.transform, false);
            RectTransform rtDisp = objDisp.GetComponent<RectTransform>();
            rtDisp.anchorMin = new Vector2(0.5f, 0.5f);
            rtDisp.anchorMax = new Vector2(0.5f, 0.5f);
            rtDisp.anchoredPosition = new Vector2(0f, 25f);
            rtDisp.sizeDelta = new Vector2(330f, 280f);
            Image imgDisp = objDisp.GetComponent<Image>() ?? objDisp.AddComponent<Image>();
            imgDisp.preserveAspect = true;
            imgDisp.enabled = (imgDisp.sprite != null);
            imgs[i] = imgDisp;

            // Text Label
            Transform tLbl = objOpt.transform.Find("Text_Label");
            GameObject objLbl = tLbl != null ? tLbl.gameObject : new GameObject("Text_Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            if (tLbl == null) objLbl.transform.SetParent(objOpt.transform, false);
            RectTransform rtLbl = objLbl.GetComponent<RectTransform>();
            rtLbl.anchorMin = new Vector2(0.5f, 0.5f);
            rtLbl.anchorMax = new Vector2(0.5f, 0.5f);
            rtLbl.anchoredPosition = new Vector2(0f, -155f);
            rtLbl.sizeDelta = new Vector2(330f, 36f);
            Text txtLbl = objLbl.GetComponent<Text>();
            txtLbl.font = defaultFont;
            txtLbl.fontSize = 20;
            txtLbl.fontStyle = FontStyle.Bold;
            txtLbl.alignment = TextAnchor.MiddleCenter;
            txtLbl.color = new Color(0.28f, 0.35f, 0.45f);
            txts[i] = txtLbl;
        }

        // -------------------------------------------------------------
        // 4. PILIH BUTTON (CONFIRM ANSWER)
        // -------------------------------------------------------------
        Transform oldBtnConfirm = panelQuiz.transform.Find("Btn_ConfirmAnswer");
        if (oldBtnConfirm != null) oldBtnConfirm.SetParent(cardQuiz.transform, false);

        Transform tConfirm = cardQuiz.transform.Find("Btn_ConfirmAnswer");
        GameObject objConfirm = tConfirm != null ? tConfirm.gameObject : new GameObject("Btn_ConfirmAnswer", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(Outline));
        if (tConfirm == null) objConfirm.transform.SetParent(cardQuiz.transform, false);
        Undo.RecordObject(objConfirm, "Setup Confirm Button");

        RectTransform rtConfirm = objConfirm.GetComponent<RectTransform>();
        rtConfirm.anchorMin = new Vector2(0.5f, 0.5f);
        rtConfirm.anchorMax = new Vector2(0.5f, 0.5f);
        rtConfirm.anchoredPosition = new Vector2(25f, -315f);
        rtConfirm.sizeDelta = new Vector2(240f, 68f);

        Sprite sprBtnGreen = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/spr_ui_rounded_btn_green.png");
        Image imgConfirm = objConfirm.GetComponent<Image>() ?? objConfirm.AddComponent<Image>();
        if (sprBtnGreen != null)
        {
            imgConfirm.sprite = sprBtnGreen;
            imgConfirm.type = Image.Type.Sliced;
            imgConfirm.color = Color.white;
            var ol = objConfirm.GetComponent<Outline>();
            if (ol != null) Object.DestroyImmediate(ol);
        }
        else
        {
            imgConfirm.color = new Color(0.13f, 0.77f, 0.36f);
            Outline outConfirm = objConfirm.GetComponent<Outline>() ?? objConfirm.AddComponent<Outline>();
            outConfirm.effectColor = new Color(0.08f, 0.5f, 0.24f);
            outConfirm.effectDistance = new Vector2(2f, -2f);
        }

        Transform tConfirmTxt = objConfirm.transform.Find("Text");
        GameObject objConfirmTxt = tConfirmTxt != null ? tConfirmTxt.gameObject : new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        if (tConfirmTxt == null) objConfirmTxt.transform.SetParent(objConfirm.transform, false);
        RectTransform rtConfirmTxt = objConfirmTxt.GetComponent<RectTransform>();
        rtConfirmTxt.anchorMin = Vector2.zero;
        rtConfirmTxt.anchorMax = Vector2.one;
        rtConfirmTxt.offsetMin = Vector2.zero;
        rtConfirmTxt.offsetMax = Vector2.zero;
        Text txtConfirm = objConfirmTxt.GetComponent<Text>();
        txtConfirm.text = "Pilih";
        txtConfirm.font = defaultFont;
        txtConfirm.fontSize = 24;
        txtConfirm.fontStyle = FontStyle.Bold;
        txtConfirm.alignment = TextAnchor.MiddleCenter;
        txtConfirm.color = Color.white;

        // -------------------------------------------------------------
        // 5. HUBUNGKAN LEVEL1QUIZMANAGER
        // -------------------------------------------------------------
        if (quizManager == null) quizManager = panelQuiz.GetComponent<Level1QuizManager>();
        if (quizManager == null) quizManager = panelQuiz.AddComponent<Level1QuizManager>();
        Undo.RecordObject(quizManager, "Connect Figcraft Quiz Elements");

        quizManager.quizPanel = panelQuiz;
        quizManager.promptText = txtPrompt;
        quizManager.feedbackText = txtFeedback;
        quizManager.confirmAnswerButton = objConfirm.GetComponent<Button>();
        quizManager.buGuruCard = rtGuru;
        quizManager.optionButtons = btns;
        quizManager.optionImages = imgs;
        quizManager.optionTexts = txts;
        quizManager.optionCardOutlines = outlines;

        quizManager.SetupDefaultQuestions();
        quizManager.AutoAssignAssetsInEditor();

        // Sembunyikan panel kuis saat idle di editor
        panelQuiz.SetActive(false);

        EditorUtility.SetDirty(panelQuiz);
        EditorUtility.SetDirty(quizManager);
        
        if (!Application.isPlaying)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
        }

        Debug.Log("[Level23SetupEditor] Restyle Quiz UI to Figcraft notebook style berhasil!");
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

        RectTransform rtCard = card.GetComponent<RectTransform>();
        if (rtCard != null)
        {
            rtCard.sizeDelta = new Vector2(680f, 490f);
        }

        // Cari atau buat Slider_Camera
        Transform tSlider = card.Find("Slider_CameraDistance");
        Transform tLabel = card.Find("LabelCamera");
        Transform tSliderQuake = card.Find("Slider_EarthquakeIntensity");
        Transform tLabelQuake = card.Find("LabelEarthquake");

        // Hapus Slider & Label Intensitas Gempa (setting dipindah sepenuhnya ke Unity Inspector)
        if (tSliderQuake != null)
        {
            Undo.DestroyObjectImmediate(tSliderQuake.gameObject);
            tSliderQuake = null;
        }
        if (tLabelQuake != null)
        {
            Undo.DestroyObjectImmediate(tLabelQuake.gameObject);
            tLabelQuake = null;
        }

        // Reposisi tombol & elemen agar rapi dan selaras dengan Figcraft
        RectTransform rtRibbon = card.Find("Ribbon_Header") as RectTransform;
        RectTransform rtLblBGM = card.Find("LabelBGM") as RectTransform;
        RectTransform rtSldBGM = card.Find("Slider_BGM") as RectTransform;
        RectTransform rtLblSFX = card.Find("LabelSFX") as RectTransform;
        RectTransform rtSldSFX = card.Find("Slider_SFX") as RectTransform;
        RectTransform rtBtnResume = card.Find("Btn_Resume") as RectTransform;
        RectTransform rtBtnUlangi = card.Find("Btn_Ulangi") as RectTransform;
        RectTransform rtBtnMenu = card.Find("Btn_MenuUtama") as RectTransform;

        if (rtRibbon != null) rtRibbon.anchoredPosition = new Vector2(0f, 25f);
        if (rtLblBGM != null) rtLblBGM.anchoredPosition = new Vector2(-120f, 150f);
        if (rtSldBGM != null) rtSldBGM.anchoredPosition = new Vector2(30f, 125f);
        if (rtLblSFX != null) rtLblSFX.anchoredPosition = new Vector2(-120f, 85f);
        if (rtSldSFX != null) rtSldSFX.anchoredPosition = new Vector2(30f, 60f);

        // 1. Label & Slider Camera (Zoom)
        if (tLabel == null && rtLblSFX != null)
        {
            GameObject labelObj = Object.Instantiate(rtLblSFX.gameObject, card, false);
            labelObj.name = "LabelCamera";
            tLabel = labelObj.transform;
            Text txt = labelObj.GetComponent<Text>();
            if (txt != null) txt.text = "JARAK KAMERA (ZOOM)";
        }
        if (tLabel != null)
        {
            RectTransform rt = tLabel as RectTransform;
            rt.anchoredPosition = new Vector2(-120f, 20f);
            Text txt = tLabel.GetComponent<Text>();
            if (txt != null) txt.text = "JARAK KAMERA (ZOOM)";
        }

        if (tSlider == null && rtSldSFX != null)
        {
            GameObject sliderObj = Object.Instantiate(rtSldSFX.gameObject, card, false);
            sliderObj.name = "Slider_CameraDistance";
            tSlider = sliderObj.transform;
        }
        if (tSlider != null)
        {
            RectTransform rt = tSlider as RectTransform;
            rt.anchoredPosition = new Vector2(30f, -5f);
            Slider sld = tSlider.GetComponent<Slider>();
            if (sld != null)
            {
                sld.minValue = 1.2f;
                sld.maxValue = 5.0f;
                sld.value = 2.6f;
                mui.pauseCameraDistanceSlider = sld;
            }
        }

        Transform tVal = tSlider != null ? tSlider.Find("Text_Value") : null;
        if (tVal == null && tSlider != null)
        {
            GameObject valObj = new GameObject("Text_Value", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            valObj.transform.SetParent(tSlider, false);
            tVal = valObj.transform;
            RectTransform rtVal = valObj.GetComponent<RectTransform>();
            rtVal.anchorMin = new Vector2(1f, 0.5f);
            rtVal.anchorMax = new Vector2(1f, 0.5f);
            rtVal.pivot = new Vector2(0f, 0.5f);
            rtVal.anchoredPosition = new Vector2(15f, 0f);
            rtVal.sizeDelta = new Vector2(60f, 30f);

            Text txtVal = valObj.GetComponent<Text>();
            txtVal.font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Fredoka-Variable.ttf") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            txtVal.fontSize = 16;
            txtVal.fontStyle = FontStyle.Bold;
            txtVal.color = new Color(0.15f, 0.2f, 0.3f);
            txtVal.alignment = TextAnchor.MiddleLeft;
            txtVal.text = "2.6m";
            mui.pauseCameraDistanceValueText = txtVal;
        }

        if (rtBtnResume != null) rtBtnResume.anchoredPosition = new Vector2(0f, -75f);
        if (rtBtnUlangi != null) rtBtnUlangi.anchoredPosition = new Vector2(-120f, -145f);
        if (rtBtnMenu != null) rtBtnMenu.anchoredPosition = new Vector2(120f, -145f);

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

        // 1. MissionBanner di Kiri Atas (di atas Avatar Bu Guru)
        Transform banner = hudTop.Find("MissionBanner");
        if (banner != null)
        {
            RectTransform rtBanner = banner.GetComponent<RectTransform>();
            if (rtBanner != null)
            {
                rtBanner.anchorMin = new Vector2(0f, 1f);
                rtBanner.anchorMax = new Vector2(0f, 1f);
                rtBanner.pivot = new Vector2(0f, 1f);
                rtBanner.anchoredPosition = new Vector2(25f, -25f);
                rtBanner.sizeDelta = new Vector2(520f, 75f);
            }

            Transform tInstr = banner.Find("InstructionText");
            if (tInstr != null)
            {
                RectTransform rtInstr = tInstr.GetComponent<RectTransform>();
                if (rtInstr != null)
                {
                    rtInstr.anchorMin = Vector2.zero;
                    rtInstr.anchorMax = Vector2.one;
                    rtInstr.offsetMin = new Vector2(20f, 6f);
                    rtInstr.offsetMax = new Vector2(-20f, -6f);
                }
                Text txtInstr = tInstr.GetComponent<Text>();
                if (txtInstr != null)
                {
                    txtInstr.alignment = TextAnchor.MiddleLeft;
                    txtInstr.fontSize = 17;
                    txtInstr.fontStyle = FontStyle.Bold;
                    txtInstr.color = Color.white;
                }
            }
        }

        // 2. TimerBadge di Kanan Atas
        Transform timerBadge = hudTop.Find("TimerBadge");
        if (timerBadge != null)
        {
            RectTransform rtTimer = timerBadge.GetComponent<RectTransform>();
            if (rtTimer != null)
            {
                rtTimer.anchorMin = new Vector2(1f, 1f);
                rtTimer.anchorMax = new Vector2(1f, 1f);
                rtTimer.pivot = new Vector2(1f, 1f);
                rtTimer.anchoredPosition = new Vector2(-115f, -25f);
                rtTimer.sizeDelta = new Vector2(130f, 65f);
            }
        }

        // 3. Tombol Pause di Pojok Kanan Atas
        Transform tPause = hudTop.Find("Btn_Pause");
        GameObject objPause = tPause != null ? tPause.gameObject : new GameObject("Btn_Pause", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(Outline));
        if (tPause == null) objPause.transform.SetParent(hudTop, false);

        RectTransform rtBtn = objPause.GetComponent<RectTransform>();
        rtBtn.anchorMin = new Vector2(1f, 1f);
        rtBtn.anchorMax = new Vector2(1f, 1f);
        rtBtn.pivot = new Vector2(1f, 1f);
        rtBtn.anchoredPosition = new Vector2(-25f, -25f);
        rtBtn.sizeDelta = new Vector2(65f, 65f);

        Sprite sprPause = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/spr_ui_rounded_btn_orange.png");
        Image imgBtn = objPause.GetComponent<Image>() ?? objPause.AddComponent<Image>();
        if (sprPause != null)
        {
            imgBtn.sprite = sprPause;
            imgBtn.type = Image.Type.Sliced;
            imgBtn.color = Color.white;
            var ol = objPause.GetComponent<Outline>();
            if (ol != null) Object.DestroyImmediate(ol);
        }
        else
        {
            imgBtn.color = new Color(0.98f, 0.65f, 0.11f); // Figcraft Amber / Orange
            Outline outBtn = objPause.GetComponent<Outline>() ?? objPause.AddComponent<Outline>();
            outBtn.effectColor = new Color(0.75f, 0.45f, 0.05f);
            outBtn.effectDistance = new Vector2(2f, -2f);
        }

        // Pastikan Banner dan Timer juga rounded
        Sprite sprHudDark = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/spr_ui_rounded_hud_dark.png");
        if (sprHudDark != null)
        {
            if (banner != null)
            {
                var bImg = banner.GetComponent<Image>();
                if (bImg != null) { bImg.sprite = sprHudDark; bImg.type = Image.Type.Sliced; bImg.color = Color.white; }
            }
            if (timerBadge != null)
            {
                var tImg = timerBadge.GetComponent<Image>();
                if (tImg != null) { tImg.sprite = sprHudDark; tImg.type = Image.Type.Sliced; tImg.color = Color.white; }
            }
        }

        // Matikan Image background HUD_Top agar tidak memunculkan bar hitam panjang memotong layar
        Image imgHudTop = hudTop.GetComponent<Image>();
        if (imgHudTop != null)
        {
            imgHudTop.color = Color.clear;
            imgHudTop.enabled = false;
        }

        Transform tIcon = objPause.transform.Find("Text");
        GameObject objIcon = tIcon != null ? tIcon.gameObject : new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        if (tIcon == null) objIcon.transform.SetParent(objPause.transform, false);

        RectTransform rtIcon = objIcon.GetComponent<RectTransform>();
        rtIcon.anchorMin = Vector2.zero;
        rtIcon.anchorMax = Vector2.one;
        rtIcon.offsetMin = Vector2.zero;
        rtIcon.offsetMax = Vector2.zero;

        Text txtIcon = objIcon.GetComponent<Text>();
        txtIcon.text = "⏸";
        txtIcon.font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Fredoka-Variable.ttf") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        txtIcon.fontSize = 22;
        txtIcon.fontStyle = FontStyle.Bold;
        txtIcon.alignment = TextAnchor.MiddleCenter;
        txtIcon.color = Color.white;

        Button btn = objPause.GetComponent<Button>() ?? objPause.AddComponent<Button>();
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(mui.OpenPauseMenu);

        mui.pauseButton = btn;

        EditorUtility.SetDirty(hudTop.gameObject);
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
        soGuide.FindProperty("arrowMaterial").objectReferenceValue = matChevron;
        soGuide.FindProperty("lineWidth").floatValue = 0.75f;
        soGuide.FindProperty("heightAboveGround").floatValue = 0.22f;
        soGuide.FindProperty("arrowDensity").floatValue = 0.35f;
        var pSpeed = soGuide.FindProperty("arrowScrollSpeed");
        if (pSpeed != null) pSpeed.floatValue = 2.2f;
        soGuide.FindProperty("waypointReachRadius").floatValue = 2.2f;
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

        GameObject panel = mui.levelCompletePanel;
        if (panel == null)
        {
            Transform t = mui.transform.Find("Popup_LevelComplete") ?? mui.transform.parent?.Find("Popup_LevelComplete");
            if (t != null) panel = t.gameObject;
        }
        if (panel == null) return;

        Undo.RecordObject(panel, "Restyle Level Complete Popup");
        panel.SetActive(true);

        Font defaultFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Fredoka-Variable.ttf")
            ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") 
            ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

        Sprite sprCardParchment = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/spr_ui_rounded_card_parchment.png");
        Sprite sprBtnYellow = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/spr_ui_rounded_btn_yellow.png");
        Sprite sprBtnGreen = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/spr_ui_rounded_btn_green.png");
        Sprite sprBtnWhite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/spr_ui_rounded_btn_white.png");
        Sprite sprCardSubtle = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/spr_ui_rounded_card_subtle.png");

        // 1. Backdrop
        RectTransform rtPanel = panel.GetComponent<RectTransform>();
        if (rtPanel != null)
        {
            rtPanel.anchorMin = Vector2.zero;
            rtPanel.anchorMax = Vector2.one;
            rtPanel.offsetMin = Vector2.zero;
            rtPanel.offsetMax = Vector2.zero;
        }
        Image imgBackdrop = panel.GetComponent<Image>() ?? panel.AddComponent<Image>();
        imgBackdrop.color = new Color(0.04f, 0.07f, 0.17f, 0.65f);

        // 2. Card (Clean White Wide Card)
        Transform tCard = panel.transform.Find("Card");
        GameObject objCard = tCard != null ? tCard.gameObject : new GameObject("Card", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
        if (tCard == null) objCard.transform.SetParent(panel.transform, false);
        Undo.RecordObject(objCard, "Setup Card");

        RectTransform rtCard = objCard.GetComponent<RectTransform>();
        rtCard.anchorMin = new Vector2(0.5f, 0.5f);
        rtCard.anchorMax = new Vector2(0.5f, 0.5f);
        rtCard.pivot = new Vector2(0.5f, 0.5f);
        rtCard.anchoredPosition = Vector2.zero;
        rtCard.sizeDelta = new Vector2(780f, 450f);

        Image imgCard = objCard.GetComponent<Image>() ?? objCard.AddComponent<Image>();
        if (sprCardParchment != null)
        {
            imgCard.sprite = sprCardParchment;
            imgCard.type = Image.Type.Sliced;
            imgCard.color = Color.white;
            var ol = objCard.GetComponent<Outline>();
            if (ol != null) Object.DestroyImmediate(ol);
        }
        else
        {
            imgCard.color = new Color(1f, 0.988f, 0.945f); // Warm cream white
            Outline outCard = objCard.GetComponent<Outline>() ?? objCard.AddComponent<Outline>();
            outCard.effectColor = new Color(0.85f, 0.88f, 0.92f);
            outCard.effectDistance = new Vector2(3.5f, -3.5f);
        }

        // 3. Header Ribbon (Gold / Orange Pill)
        Transform tRibbon = objCard.transform.Find("Ribbon_Header");
        GameObject objRibbon = tRibbon != null ? tRibbon.gameObject : new GameObject("Ribbon_Header", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
        if (tRibbon == null) objRibbon.transform.SetParent(objCard.transform, false);

        RectTransform rtRibbon = objRibbon.GetComponent<RectTransform>();
        rtRibbon.anchorMin = new Vector2(0.5f, 1f);
        rtRibbon.anchorMax = new Vector2(0.5f, 1f);
        rtRibbon.pivot = new Vector2(0.5f, 0.5f);
        rtRibbon.anchoredPosition = new Vector2(0f, 0f);
        rtRibbon.sizeDelta = new Vector2(480f, 56f);

        Image imgRibbon = objRibbon.GetComponent<Image>() ?? objRibbon.AddComponent<Image>();
        if (sprBtnYellow != null)
        {
            imgRibbon.sprite = sprBtnYellow;
            imgRibbon.type = Image.Type.Sliced;
            imgRibbon.color = Color.white;
            var ol = objRibbon.GetComponent<Outline>();
            if (ol != null) Object.DestroyImmediate(ol);
        }
        else
        {
            imgRibbon.color = new Color(0.96f, 0.62f, 0.04f); // Warm Gold #F59E0B
            Outline outRibbon = objRibbon.GetComponent<Outline>() ?? objRibbon.AddComponent<Outline>();
            outRibbon.effectColor = new Color(0.79f, 0.51f, 0.03f);
            outRibbon.effectDistance = new Vector2(2f, -2f);
        }

        Transform tRibbonTxt = objRibbon.transform.Find("Text");
        GameObject objRibbonTxt = tRibbonTxt != null ? tRibbonTxt.gameObject : new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        if (tRibbonTxt == null) objRibbonTxt.transform.SetParent(objRibbon.transform, false);
        RectTransform rtRibbonTxt = objRibbonTxt.GetComponent<RectTransform>();
        rtRibbonTxt.anchorMin = Vector2.zero;
        rtRibbonTxt.anchorMax = Vector2.one;
        rtRibbonTxt.offsetMin = Vector2.zero;
        rtRibbonTxt.offsetMax = Vector2.zero;
        Text txtRibbon = objRibbonTxt.GetComponent<Text>();
        txtRibbon.text = "🎉 TAHAP SELESAI! 🎉";
        txtRibbon.font = defaultFont;
        txtRibbon.fontSize = 22;
        txtRibbon.fontStyle = FontStyle.Bold;
        txtRibbon.alignment = TextAnchor.MiddleCenter;
        txtRibbon.color = Color.white;

        // 3b. Celebratory Stars
        Transform tStars = objCard.transform.Find("Stars");
        GameObject objStars = tStars != null ? tStars.gameObject : new GameObject("Stars", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        if (tStars == null) objStars.transform.SetParent(objCard.transform, false);
        RectTransform rtStars = objStars.GetComponent<RectTransform>();
        rtStars.anchorMin = new Vector2(0.5f, 0.5f);
        rtStars.anchorMax = new Vector2(0.5f, 0.5f);
        rtStars.anchoredPosition = new Vector2(0f, 135f);
        rtStars.sizeDelta = new Vector2(350f, 50f);
        Text txtStars = objStars.GetComponent<Text>();
        txtStars.text = "⭐  ⭐  ⭐";
        txtStars.font = defaultFont;
        txtStars.fontSize = 40;
        txtStars.fontStyle = FontStyle.Bold;
        txtStars.alignment = TextAnchor.MiddleCenter;
        txtStars.color = new Color(0.96f, 0.62f, 0.04f);

        // 4. Title (Nama & Ucapan Hebat)
        Transform tTitle = objCard.transform.Find("Title");
        GameObject objTitle = tTitle != null ? tTitle.gameObject : new GameObject("Title", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        if (tTitle == null) objTitle.transform.SetParent(objCard.transform, false);
        RectTransform rtTitle = objTitle.GetComponent<RectTransform>();
        rtTitle.anchorMin = new Vector2(0.5f, 0.5f);
        rtTitle.anchorMax = new Vector2(0.5f, 0.5f);
        rtTitle.anchoredPosition = new Vector2(0f, 75f);
        rtTitle.sizeDelta = new Vector2(680f, 50f);
        Text txtTitle = objTitle.GetComponent<Text>();
        txtTitle.text = "HEBAT SEKALI, ANANDA!";
        txtTitle.font = defaultFont;
        txtTitle.fontSize = 28;
        txtTitle.fontStyle = FontStyle.Bold;
        txtTitle.alignment = TextAnchor.MiddleCenter;
        txtTitle.color = new Color(0.06f, 0.09f, 0.16f); // Dark Charcoal #0F172A

        // 5. Desc
        Transform tDesc = objCard.transform.Find("Desc");
        GameObject objDesc = tDesc != null ? tDesc.gameObject : new GameObject("Desc", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        if (tDesc == null) objDesc.transform.SetParent(objCard.transform, false);
        RectTransform rtDesc = objDesc.GetComponent<RectTransform>();
        rtDesc.anchorMin = new Vector2(0.5f, 0.5f);
        rtDesc.anchorMax = new Vector2(0.5f, 0.5f);
        rtDesc.anchoredPosition = new Vector2(0f, -5f);
        rtDesc.sizeDelta = new Vector2(660f, 65f);
        Text txtDesc = objDesc.GetComponent<Text>();
        txtDesc.text = "Bagus sekali! Tahap ini berhasil kamu selesaikan dengan tertib dan mandiri!";
        txtDesc.font = defaultFont;
        txtDesc.fontSize = 18;
        txtDesc.fontStyle = FontStyle.Normal;
        txtDesc.alignment = TextAnchor.MiddleCenter;
        txtDesc.color = new Color(0.20f, 0.26f, 0.35f);

        // 6. Next Level Button
        Transform tBtn = objCard.transform.Find("Btn_NextLevel");
        GameObject objBtn = tBtn != null ? tBtn.gameObject : new GameObject("Btn_NextLevel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(Outline));
        if (tBtn == null) objBtn.transform.SetParent(objCard.transform, false);
        RectTransform rtBtn = objBtn.GetComponent<RectTransform>();
        rtBtn.anchorMin = new Vector2(0.5f, 0.5f);
        rtBtn.anchorMax = new Vector2(0.5f, 0.5f);
        rtBtn.anchoredPosition = new Vector2(0f, -115f);
        rtBtn.sizeDelta = new Vector2(460f, 64f);

        Image imgBtn = objBtn.GetComponent<Image>() ?? objBtn.AddComponent<Image>();
        if (sprBtnGreen != null)
        {
            imgBtn.sprite = sprBtnGreen;
            imgBtn.type = Image.Type.Sliced;
            imgBtn.color = Color.white;
            var ol = objBtn.GetComponent<Outline>();
            if (ol != null) Object.DestroyImmediate(ol);
        }
        else
        {
            imgBtn.color = new Color(0.13f, 0.77f, 0.36f); // Vibrant Emerald Green #22C55E
            Outline outBtn = objBtn.GetComponent<Outline>() ?? objBtn.AddComponent<Outline>();
            outBtn.effectColor = new Color(0.08f, 0.64f, 0.29f);
            outBtn.effectDistance = new Vector2(2.5f, -2.5f);
        }

        Button btnNext = objBtn.GetComponent<Button>() ?? objBtn.AddComponent<Button>();

        Transform tBtnTxt = objBtn.transform.Find("Text");
        GameObject objBtnTxt = tBtnTxt != null ? tBtnTxt.gameObject : new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        if (tBtnTxt == null) objBtnTxt.transform.SetParent(objBtn.transform, false);
        RectTransform rtBtnTxt = objBtnTxt.GetComponent<RectTransform>();
        rtBtnTxt.anchorMin = Vector2.zero;
        rtBtnTxt.anchorMax = Vector2.one;
        rtBtnTxt.offsetMin = Vector2.zero;
        rtBtnTxt.offsetMax = Vector2.zero;
        Text txtBtn = objBtnTxt.GetComponent<Text>();
        txtBtn.text = "LANJUT KE LEVEL BERIKUTNYA ▶";
        txtBtn.font = defaultFont;
        txtBtn.fontSize = 20;
        txtBtn.fontStyle = FontStyle.Bold;
        txtBtn.alignment = TextAnchor.MiddleCenter;
        txtBtn.color = Color.white;

        // Wire to MissionUI
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

        GameObject panel = mui.finalScorePanel;
        if (panel == null)
        {
            Transform t = mui.transform.Find("Popup_FinalScore") ?? mui.transform.parent?.Find("Popup_FinalScore");
            if (t != null) panel = t.gameObject;
        }
        if (panel == null) return;

        Undo.RecordObject(panel, "Restyle Final Score Popup");
        panel.SetActive(true);

        Font defaultFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Fredoka-Variable.ttf")
            ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") 
            ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

        Sprite sprCardParchment = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/spr_ui_rounded_card_parchment.png");
        Sprite sprBtnYellow = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/spr_ui_rounded_btn_yellow.png");
        Sprite sprBtnGreen = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/spr_ui_rounded_btn_green.png");
        Sprite sprBtnWhite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/spr_ui_rounded_btn_white.png");
        Sprite sprCardSubtle = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/spr_ui_rounded_card_subtle.png");

        // 1. Backdrop
        RectTransform rtPanel = panel.GetComponent<RectTransform>();
        if (rtPanel != null)
        {
            rtPanel.anchorMin = Vector2.zero;
            rtPanel.anchorMax = Vector2.one;
            rtPanel.offsetMin = Vector2.zero;
            rtPanel.offsetMax = Vector2.zero;
        }
        Image imgBackdrop = panel.GetComponent<Image>() ?? panel.AddComponent<Image>();
        imgBackdrop.color = new Color(0.04f, 0.07f, 0.17f, 0.65f);

        // 2. Card (Clean White Wide Card)
        Transform tCard = panel.transform.Find("Card");
        GameObject objCard = tCard != null ? tCard.gameObject : new GameObject("Card", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
        if (tCard == null) objCard.transform.SetParent(panel.transform, false);
        Undo.RecordObject(objCard, "Setup Card");

        // Bersihkan child lama yang tidak terstruktur
        Transform oldTime = objCard.transform.Find("TimeText");
        if (oldTime != null) Object.DestroyImmediate(oldTime.gameObject);
        Transform oldEval = objCard.transform.Find("EvalText");
        if (oldEval != null) Object.DestroyImmediate(oldEval.gameObject);

        RectTransform rtCard = objCard.GetComponent<RectTransform>();
        rtCard.anchorMin = new Vector2(0.5f, 0.5f);
        rtCard.anchorMax = new Vector2(0.5f, 0.5f);
        rtCard.pivot = new Vector2(0.5f, 0.5f);
        rtCard.anchoredPosition = Vector2.zero;
        rtCard.sizeDelta = new Vector2(980f, 630f);

        Image imgCard = objCard.GetComponent<Image>() ?? objCard.AddComponent<Image>();
        if (sprCardParchment != null)
        {
            imgCard.sprite = sprCardParchment;
            imgCard.type = Image.Type.Sliced;
            imgCard.color = Color.white;
            var ol = objCard.GetComponent<Outline>();
            if (ol != null) Object.DestroyImmediate(ol);
        }
        else
        {
            imgCard.color = new Color(1f, 0.988f, 0.945f); // Warm cream white
            Outline outCard = objCard.GetComponent<Outline>() ?? objCard.AddComponent<Outline>();
            outCard.effectColor = new Color(0.85f, 0.88f, 0.92f);
            outCard.effectDistance = new Vector2(3.5f, -3.5f);
        }

        // 3. Header Ribbon (Blue Pill)
        Transform tRibbon = objCard.transform.Find("Ribbon_Header");
        GameObject objRibbon = tRibbon != null ? tRibbon.gameObject : new GameObject("Ribbon_Header", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
        if (tRibbon == null) objRibbon.transform.SetParent(objCard.transform, false);

        RectTransform rtRibbon = objRibbon.GetComponent<RectTransform>();
        rtRibbon.anchorMin = new Vector2(0.5f, 1f);
        rtRibbon.anchorMax = new Vector2(0.5f, 1f);
        rtRibbon.pivot = new Vector2(0.5f, 0.5f);
        rtRibbon.anchoredPosition = new Vector2(0f, 0f);
        rtRibbon.sizeDelta = new Vector2(580f, 54f);

        Image imgRibbon = objRibbon.GetComponent<Image>() ?? objRibbon.AddComponent<Image>();
        if (sprBtnYellow != null)
        {
            imgRibbon.sprite = sprBtnYellow;
            imgRibbon.type = Image.Type.Sliced;
            imgRibbon.color = Color.white;
            var ol = objRibbon.GetComponent<Outline>();
            if (ol != null) Object.DestroyImmediate(ol);
        }
        else
        {
            imgRibbon.color = new Color(0.15f, 0.39f, 0.92f); // Royal Blue #2563EB
            Outline outRibbon = objRibbon.GetComponent<Outline>() ?? objRibbon.AddComponent<Outline>();
            outRibbon.effectColor = new Color(0.11f, 0.27f, 0.68f);
            outRibbon.effectDistance = new Vector2(2f, -2f);
        }

        Transform tRibbonTxt = objRibbon.transform.Find("Text");
        GameObject objRibbonTxt = tRibbonTxt != null ? tRibbonTxt.gameObject : new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        if (tRibbonTxt == null) objRibbonTxt.transform.SetParent(objRibbon.transform, false);
        RectTransform rtRibbonTxt = objRibbonTxt.GetComponent<RectTransform>();
        rtRibbonTxt.anchorMin = Vector2.zero;
        rtRibbonTxt.anchorMax = Vector2.one;
        rtRibbonTxt.offsetMin = Vector2.zero;
        rtRibbonTxt.offsetMax = Vector2.zero;
        Text txtRibbon = objRibbonTxt.GetComponent<Text>();
        txtRibbon.text = "🏆 RAPOR PAHLAWAN CILIK 🏆";
        txtRibbon.font = defaultFont;
        txtRibbon.fontSize = 20;
        txtRibbon.fontStyle = FontStyle.Bold;
        txtRibbon.alignment = TextAnchor.MiddleCenter;
        txtRibbon.color = Color.white;

        // 4. Stars
        Transform tStars = objCard.transform.Find("Stars");
        GameObject objStars = tStars != null ? tStars.gameObject : new GameObject("Stars", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        if (tStars == null) objStars.transform.SetParent(objCard.transform, false);
        RectTransform rtStars = objStars.GetComponent<RectTransform>();
        rtStars.anchorMin = new Vector2(0.5f, 0.5f);
        rtStars.anchorMax = new Vector2(0.5f, 0.5f);
        rtStars.anchoredPosition = new Vector2(0f, 222f);
        rtStars.sizeDelta = new Vector2(320f, 44f);
        Text txtStars = objStars.GetComponent<Text>();
        txtStars.text = "⭐  ⭐  ⭐";
        txtStars.font = defaultFont;
        txtStars.fontSize = 44;
        txtStars.fontStyle = FontStyle.Bold;
        txtStars.alignment = TextAnchor.MiddleCenter;
        txtStars.color = new Color(0.96f, 0.62f, 0.04f); // Golden Gold

        // 5. Title
        Transform tTitle = objCard.transform.Find("Title");
        GameObject objTitle = tTitle != null ? tTitle.gameObject : new GameObject("Title", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        if (tTitle == null) objTitle.transform.SetParent(objCard.transform, false);
        RectTransform rtTitle = objTitle.GetComponent<RectTransform>();
        rtTitle.anchorMin = new Vector2(0.5f, 0.5f);
        rtTitle.anchorMax = new Vector2(0.5f, 0.5f);
        rtTitle.anchoredPosition = new Vector2(0f, 175f);
        rtTitle.sizeDelta = new Vector2(900f, 40f);
        Text txtTitle = objTitle.GetComponent<Text>();
        txtTitle.text = "SELAMAT! KAMU JADI PAHLAWAN CILIK SIAGA GEMPA!";
        txtTitle.font = defaultFont;
        txtTitle.fontSize = 25;
        txtTitle.fontStyle = FontStyle.Bold;
        txtTitle.alignment = TextAnchor.MiddleCenter;
        txtTitle.color = new Color(0.06f, 0.09f, 0.16f);

        // 6. Subtitle
        Transform tSub = objCard.transform.Find("Subtitle");
        GameObject objSub = tSub != null ? tSub.gameObject : new GameObject("Subtitle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        if (tSub == null) objSub.transform.SetParent(objCard.transform, false);
        RectTransform rtSub = objSub.GetComponent<RectTransform>();
        rtSub.anchorMin = new Vector2(0.5f, 0.5f);
        rtSub.anchorMax = new Vector2(0.5f, 0.5f);
        rtSub.anchoredPosition = new Vector2(0f, 142f);
        rtSub.sizeDelta = new Vector2(900f, 26f);
        Text txtSub = objSub.GetComponent<Text>();
        txtSub.text = "TK AISYIYAH 28 SURABAYA • KELULUSAN MANDIRI & TERTIB";
        txtSub.font = defaultFont;
        txtSub.fontSize = 15;
        txtSub.fontStyle = FontStyle.Bold;
        txtSub.alignment = TextAnchor.MiddleCenter;
        txtSub.color = new Color(0.71f, 0.33f, 0.04f); // Amber / Ochre

        // 7. 3 Level Pillar Cards (side-by-side)
        float[] pX = new float[] { -295f, 0f, 295f };
        string[] pLvl = new string[] { "LEVEL 1", "LEVEL 2", "LEVEL 3" };
        Color[] pColors = new Color[] {
            new Color(0.23f, 0.51f, 0.96f), // Blue
            new Color(0.96f, 0.62f, 0.04f), // Amber
            new Color(0.13f, 0.77f, 0.36f)  // Emerald
        };
        Color[] pBgs = new Color[] {
            new Color(0.94f, 0.96f, 1.0f),
            new Color(1.0f, 0.98f, 0.92f),
            new Color(0.94f, 0.99f, 0.96f)
        };
        string[] pTitles = new string[] { "Kuis Bahaya Gempa", "Tanggap Lindung", "Evakuasi Lapangan" };
        string[] pChecks = new string[] { "✓ Kenali Benda Goyang", "✓ Cepat Masuk Kolong", "✓ Tertib Lindungi Kepala" };
        Text[] pScoreTexts = new Text[3];

        for (int i = 0; i < 3; i++)
        {
            Transform tPillar = objCard.transform.Find($"Pillar_{i + 1}");
            GameObject objPillar = tPillar != null ? tPillar.gameObject : new GameObject($"Pillar_{i + 1}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
            if (tPillar == null) objPillar.transform.SetParent(objCard.transform, false);

            RectTransform rtPillar = objPillar.GetComponent<RectTransform>();
            rtPillar.anchorMin = new Vector2(0.5f, 0.5f);
            rtPillar.anchorMax = new Vector2(0.5f, 0.5f);
            rtPillar.anchoredPosition = new Vector2(pX[i], 42f);
            rtPillar.sizeDelta = new Vector2(275f, 135f);

            Image imgPillar = objPillar.GetComponent<Image>() ?? objPillar.AddComponent<Image>();
            imgPillar.color = pBgs[i];
            Outline outPillar = objPillar.GetComponent<Outline>() ?? objPillar.AddComponent<Outline>();
            outPillar.effectColor = new Color(0.85f, 0.88f, 0.92f);
            outPillar.effectDistance = new Vector2(2f, -2f);

            // Badge
            Transform tBdg = objPillar.transform.Find("Badge");
            GameObject objBdg = tBdg != null ? tBdg.gameObject : new GameObject("Badge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            if (tBdg == null) objBdg.transform.SetParent(objPillar.transform, false);
            RectTransform rtBdg = objBdg.GetComponent<RectTransform>();
            rtBdg.anchorMin = new Vector2(0.5f, 0.5f);
            rtBdg.anchorMax = new Vector2(0.5f, 0.5f);
            rtBdg.anchoredPosition = new Vector2(0f, 48f);
            rtBdg.sizeDelta = new Vector2(115f, 26f);
            Image imgBdg = objBdg.GetComponent<Image>() ?? objBdg.AddComponent<Image>();
            imgBdg.color = pColors[i];

            Transform tBdgTxt = objBdg.transform.Find("Text");
            GameObject objBdgTxt = tBdgTxt != null ? tBdgTxt.gameObject : new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            if (tBdgTxt == null) objBdgTxt.transform.SetParent(objBdg.transform, false);
            RectTransform rtBdgTxt = objBdgTxt.GetComponent<RectTransform>();
            rtBdgTxt.anchorMin = Vector2.zero;
            rtBdgTxt.anchorMax = Vector2.one;
            rtBdgTxt.offsetMin = Vector2.zero;
            rtBdgTxt.offsetMax = Vector2.zero;
            Text txtBdg = objBdgTxt.GetComponent<Text>();
            txtBdg.text = pLvl[i];
            txtBdg.font = defaultFont;
            txtBdg.fontSize = 12;
            txtBdg.fontStyle = FontStyle.Bold;
            txtBdg.alignment = TextAnchor.MiddleCenter;
            txtBdg.color = Color.white;

            // Pillar Title
            Transform tPTit = objPillar.transform.Find("Title");
            GameObject objPTit = tPTit != null ? tPTit.gameObject : new GameObject("Title", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            if (tPTit == null) objPTit.transform.SetParent(objPillar.transform, false);
            RectTransform rtPTit = objPTit.GetComponent<RectTransform>();
            rtPTit.anchorMin = new Vector2(0.5f, 0.5f);
            rtPTit.anchorMax = new Vector2(0.5f, 0.5f);
            rtPTit.anchoredPosition = new Vector2(0f, 18f);
            rtPTit.sizeDelta = new Vector2(250f, 24f);
            Text txtPTit = objPTit.GetComponent<Text>();
            txtPTit.text = pTitles[i];
            txtPTit.font = defaultFont;
            txtPTit.fontSize = 15;
            txtPTit.fontStyle = FontStyle.Bold;
            txtPTit.alignment = TextAnchor.MiddleCenter;
            txtPTit.color = new Color(0.12f, 0.16f, 0.25f);

            // Pillar Score Text
            Transform tPScore = objPillar.transform.Find("Score");
            GameObject objPScore = tPScore != null ? tPScore.gameObject : new GameObject("Score", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            if (tPScore == null) objPScore.transform.SetParent(objPillar.transform, false);
            RectTransform rtPScore = objPScore.GetComponent<RectTransform>();
            rtPScore.anchorMin = new Vector2(0.5f, 0.5f);
            rtPScore.anchorMax = new Vector2(0.5f, 0.5f);
            rtPScore.anchoredPosition = new Vector2(0f, -10f);
            rtPScore.sizeDelta = new Vector2(250f, 24f);
            Text txtPScore = objPScore.GetComponent<Text>();
            txtPScore.text = i == 0 ? "30 / 30 Poin" : "35 / 35 Poin";
            txtPScore.font = defaultFont;
            txtPScore.fontSize = 16;
            txtPScore.fontStyle = FontStyle.Bold;
            txtPScore.alignment = TextAnchor.MiddleCenter;
            txtPScore.color = new Color(0.09f, 0.64f, 0.29f); // Green
            pScoreTexts[i] = txtPScore;

            // Pillar Check Text
            Transform tPChk = objPillar.transform.Find("Check");
            GameObject objPChk = tPChk != null ? tPChk.gameObject : new GameObject("Check", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            if (tPChk == null) objPChk.transform.SetParent(objPillar.transform, false);
            RectTransform rtPChk = objPChk.GetComponent<RectTransform>();
            rtPChk.anchorMin = new Vector2(0.5f, 0.5f);
            rtPChk.anchorMax = new Vector2(0.5f, 0.5f);
            rtPChk.anchoredPosition = new Vector2(0f, -38f);
            rtPChk.sizeDelta = new Vector2(250f, 22f);
            Text txtPChk = objPChk.GetComponent<Text>();
            txtPChk.text = pChecks[i];
            txtPChk.font = defaultFont;
            txtPChk.fontSize = 13;
            txtPChk.alignment = TextAnchor.MiddleCenter;
            txtPChk.color = new Color(0.35f, 0.42f, 0.52f);
        }

        // 8. Total Score Banner
        Transform tBanner = objCard.transform.Find("Banner_TotalScore");
        GameObject objBanner = tBanner != null ? tBanner.gameObject : new GameObject("Banner_TotalScore", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
        if (tBanner == null) objBanner.transform.SetParent(objCard.transform, false);
        RectTransform rtBanner = objBanner.GetComponent<RectTransform>();
        rtBanner.anchorMin = new Vector2(0.5f, 0.5f);
        rtBanner.anchorMax = new Vector2(0.5f, 0.5f);
        rtBanner.anchoredPosition = new Vector2(0f, -54f);
        rtBanner.sizeDelta = new Vector2(865f, 50f);
        Image imgBanner = objBanner.GetComponent<Image>() ?? objBanner.AddComponent<Image>();
        imgBanner.color = new Color(1.0f, 0.95f, 0.78f); // Soft light amber
        Outline outBanner = objBanner.GetComponent<Outline>() ?? objBanner.AddComponent<Outline>();
        outBanner.effectColor = new Color(0.96f, 0.62f, 0.04f);
        outBanner.effectDistance = new Vector2(1.5f, -1.5f);

        Transform tBannerTxt = objBanner.transform.Find("Text");
        GameObject objBannerTxt = tBannerTxt != null ? tBannerTxt.gameObject : new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        if (tBannerTxt == null) objBannerTxt.transform.SetParent(objBanner.transform, false);
        RectTransform rtBannerTxt = objBannerTxt.GetComponent<RectTransform>();
        rtBannerTxt.anchorMin = Vector2.zero;
        rtBannerTxt.anchorMax = Vector2.one;
        rtBannerTxt.offsetMin = Vector2.zero;
        rtBannerTxt.offsetMax = Vector2.zero;
        Text txtBanner = objBannerTxt.GetComponent<Text>();
        txtBanner.text = "⭐ TOTAL SKOR KELULUSAN: 100 / 100 (SEMPURNA!) ⭐";
        txtBanner.font = defaultFont;
        txtBanner.fontSize = 20;
        txtBanner.fontStyle = FontStyle.Bold;
        txtBanner.alignment = TextAnchor.MiddleCenter;
        txtBanner.color = new Color(0.47f, 0.21f, 0.06f);

        // 9. Evaluation Card (Time & Notes)
        Transform tEvalCard = objCard.transform.Find("Card_Evaluation");
        GameObject objEvalCard = tEvalCard != null ? tEvalCard.gameObject : new GameObject("Card_Evaluation", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
        if (tEvalCard == null) objEvalCard.transform.SetParent(objCard.transform, false);
        RectTransform rtEvalCard = objEvalCard.GetComponent<RectTransform>();
        rtEvalCard.anchorMin = new Vector2(0.5f, 0.5f);
        rtEvalCard.anchorMax = new Vector2(0.5f, 0.5f);
        rtEvalCard.anchoredPosition = new Vector2(0f, -128f);
        rtEvalCard.sizeDelta = new Vector2(865f, 70f);
        Image imgEvalCard = objEvalCard.GetComponent<Image>() ?? objEvalCard.AddComponent<Image>();
        imgEvalCard.color = new Color(0.97f, 0.98f, 0.99f);
        Outline outEvalCard = objEvalCard.GetComponent<Outline>() ?? objEvalCard.AddComponent<Outline>();
        outEvalCard.effectColor = new Color(0.88f, 0.91f, 0.94f);
        outEvalCard.effectDistance = new Vector2(1.5f, -1.5f);

        Transform tTime = objEvalCard.transform.Find("TimeText");
        GameObject objTime = tTime != null ? tTime.gameObject : new GameObject("TimeText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        if (tTime == null) objTime.transform.SetParent(objEvalCard.transform, false);
        RectTransform rtTime = objTime.GetComponent<RectTransform>();
        rtTime.anchorMin = new Vector2(0.5f, 0.5f);
        rtTime.anchorMax = new Vector2(0.5f, 0.5f);
        rtTime.anchoredPosition = new Vector2(0f, 15f);
        rtTime.sizeDelta = new Vector2(830f, 26f);
        Text txtTime = objTime.GetComponent<Text>();
        txtTime.text = "⏱️ Waktu Selesai: 00:54 Detik • Sangat Tanggap & Mandiri";
        txtTime.font = defaultFont;
        txtTime.fontSize = 15;
        txtTime.fontStyle = FontStyle.Bold;
        txtTime.alignment = TextAnchor.MiddleCenter;
        txtTime.color = new Color(0.2f, 0.26f, 0.33f);

        Transform tEval = objEvalCard.transform.Find("EvalText");
        GameObject objEval = tEval != null ? tEval.gameObject : new GameObject("EvalText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        if (tEval == null) objEval.transform.SetParent(objEvalCard.transform, false);
        RectTransform rtEval = objEval.GetComponent<RectTransform>();
        rtEval.anchorMin = new Vector2(0.5f, 0.5f);
        rtEval.anchorMax = new Vector2(0.5f, 0.5f);
        rtEval.anchoredPosition = new Vector2(0f, -15f);
        rtEval.sizeDelta = new Vector2(830f, 26f);
        Text txtEval = objEval.GetComponent<Text>();
        txtEval.text = "Catatan Bu Guru: \"Ananda sangat pintar dan mandiri dalam mempraktikkan mitigasi bencana gempa bumi!\"";
        txtEval.font = defaultFont;
        txtEval.fontSize = 15;
        txtEval.fontStyle = FontStyle.Italic;
        txtEval.alignment = TextAnchor.MiddleCenter;
        txtEval.color = new Color(0.28f, 0.33f, 0.41f);

        // 10. Action Buttons (Main Lagi & Menu Utama)
        Transform tRetry = objCard.transform.Find("Btn_Retry");
        GameObject objRetry = tRetry != null ? tRetry.gameObject : new GameObject("Btn_Retry", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(Outline));
        if (tRetry == null) objRetry.transform.SetParent(objCard.transform, false);
        RectTransform rtRetry = objRetry.GetComponent<RectTransform>();
        rtRetry.anchorMin = new Vector2(0.5f, 0.5f);
        rtRetry.anchorMax = new Vector2(0.5f, 0.5f);
        rtRetry.anchoredPosition = new Vector2(-140f, -200f);
        rtRetry.sizeDelta = new Vector2(240f, 54f);
        Image imgRetry = objRetry.GetComponent<Image>() ?? objRetry.AddComponent<Image>();
        if (sprBtnWhite != null)
        {
            imgRetry.sprite = sprBtnWhite;
            imgRetry.type = Image.Type.Sliced;
            imgRetry.color = Color.white;
            var ol = objRetry.GetComponent<Outline>();
            if (ol != null) Object.DestroyImmediate(ol);
        }
        else
        {
            imgRetry.color = new Color(0.31f, 0.27f, 0.90f); // Indigo #4F46E5
            Outline outRetry = objRetry.GetComponent<Outline>() ?? objRetry.AddComponent<Outline>();
            outRetry.effectColor = new Color(0.22f, 0.19f, 0.68f);
            outRetry.effectDistance = new Vector2(2f, -2f);
        }
        Button btnRetry = objRetry.GetComponent<Button>() ?? objRetry.AddComponent<Button>();

        Transform tRetryTxt = objRetry.transform.Find("Text");
        GameObject objRetryTxt = tRetryTxt != null ? tRetryTxt.gameObject : new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        if (tRetryTxt == null) objRetryTxt.transform.SetParent(objRetry.transform, false);
        RectTransform rtRetryTxt = objRetryTxt.GetComponent<RectTransform>();
        rtRetryTxt.anchorMin = Vector2.zero;
        rtRetryTxt.anchorMax = Vector2.one;
        rtRetryTxt.offsetMin = Vector2.zero;
        rtRetryTxt.offsetMax = Vector2.zero;
        Text txtRetry = objRetryTxt.GetComponent<Text>();
        txtRetry.text = "🔄 MAIN LAGI";
        txtRetry.font = defaultFont;
        txtRetry.fontSize = 17;
        txtRetry.fontStyle = FontStyle.Bold;
        txtRetry.alignment = TextAnchor.MiddleCenter;
        txtRetry.color = new Color(0.12f, 0.16f, 0.22f);

        Transform tMenu = objCard.transform.Find("Btn_MainMenu");
        GameObject objMenu = tMenu != null ? tMenu.gameObject : new GameObject("Btn_MainMenu", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(Outline));
        if (tMenu == null) objMenu.transform.SetParent(objCard.transform, false);
        RectTransform rtMenu = objMenu.GetComponent<RectTransform>();
        rtMenu.anchorMin = new Vector2(0.5f, 0.5f);
        rtMenu.anchorMax = new Vector2(0.5f, 0.5f);
        rtMenu.anchoredPosition = new Vector2(140f, -200f);
        rtMenu.sizeDelta = new Vector2(250f, 54f);
        Image imgMenu = objMenu.GetComponent<Image>() ?? objMenu.AddComponent<Image>();
        if (sprBtnGreen != null)
        {
            imgMenu.sprite = sprBtnGreen;
            imgMenu.type = Image.Type.Sliced;
            imgMenu.color = Color.white;
            var ol = objMenu.GetComponent<Outline>();
            if (ol != null) Object.DestroyImmediate(ol);
        }
        else
        {
            imgMenu.color = new Color(0.13f, 0.77f, 0.36f); // Green #22C55E
            Outline outMenu = objMenu.GetComponent<Outline>() ?? objMenu.AddComponent<Outline>();
            outMenu.effectColor = new Color(0.08f, 0.64f, 0.29f);
            outMenu.effectDistance = new Vector2(2f, -2f);
        }
        Button btnMenu = objMenu.GetComponent<Button>() ?? objMenu.AddComponent<Button>();

        Transform tMenuTxt = objMenu.transform.Find("Text");
        GameObject objMenuTxt = tMenuTxt != null ? tMenuTxt.gameObject : new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        if (tMenuTxt == null) objMenuTxt.transform.SetParent(objMenu.transform, false);
        RectTransform rtMenuTxt = objMenuTxt.GetComponent<RectTransform>();
        rtMenuTxt.anchorMin = Vector2.zero;
        rtMenuTxt.anchorMax = Vector2.one;
        rtMenuTxt.offsetMin = Vector2.zero;
        rtMenuTxt.offsetMax = Vector2.zero;
        Text txtMenu = objMenuTxt.GetComponent<Text>();
        txtMenu.text = "🏠 MENU UTAMA";
        txtMenu.font = defaultFont;
        txtMenu.fontSize = 18;
        txtMenu.fontStyle = FontStyle.Bold;
        txtMenu.alignment = TextAnchor.MiddleCenter;
        txtMenu.color = Color.white;

        // Wire to MissionUI
        mui.finalScorePanel = panel;
        mui.scoreTimeText = txtTime;
        mui.scoreStarsText = txtStars;
        mui.scorePointsText = txtBanner;
        mui.scoreEvaluationText = txtEval;
        mui.scorePillar1Text = pScoreTexts[0];
        mui.scorePillar2Text = pScoreTexts[1];
        mui.scorePillar3Text = pScoreTexts[2];
        mui.retryButton = btnRetry;
        mui.mainMenuButton = btnMenu;

        panel.SetActive(false);
        EditorUtility.SetDirty(panel);
        EditorUtility.SetDirty(mui);
    }
}
#endif

