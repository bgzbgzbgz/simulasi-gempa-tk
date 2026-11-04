#if UNITY_EDITOR
using UnityEditor;
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
    // LEVEL 2: OBJEKTIF BERLINDUNG DI KOLONG MEJA
    // ==========================================
    [MenuItem("Tools/Level 2: Pasang Objektif Berlindung di Meja Terpilih")]
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

            GameObject zone = new GameObject($"Objektif_Level2_{desk.name}");
            Undo.RegisterCreatedObjectUndo(zone, "Pasang Objektif Level 2");
            zone.transform.position = b.center;

            // Zona sedikit lebih lebar dari meja supaya mudah dimasuki anak
            BoxCollider box = zone.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(b.size.x + 0.4f, b.size.y, b.size.z + 0.4f);

            // Tanpa ShelterZone: anak harus menekan tombol Runduk sendiri (inti latihan Drop & Cover)
            CheckpointTrigger checkpoint = zone.AddComponent<CheckpointTrigger>();
            checkpoint.targetLevel = 2;
            checkpoint.checkpointName = "Kolong Meja";
            checkpoint.requireCrouch = true;
            checkpoint.requiredHoldTime = 5f;
            created++;
        }

        if (created == 0)
        {
            EditorUtility.DisplayDialog("Pilih Meja Dulu",
                "Pilih satu atau beberapa meja (misal 'Meja Siswa 1') di Hierarchy, lalu jalankan menu ini lagi.", "OK");
            return;
        }

        EditorUtility.DisplayDialog("Objektif Level 2 Terpasang",
            $"{created} zona kolong meja dibuat.\n\n" +
            "Syarat selesai: pemain masuk zona, menekan Runduk, dan bertahan 5 detik.\n" +
            "Durasi bisa diubah di komponen CheckpointTrigger (Required Hold Time).", "Siap");
    }

    [MenuItem("Tools/Level 2: Pasang Efek Gempa")]
    public static void SetupEarthquakeEffect()
    {
        if (Object.FindFirstObjectByType<EarthquakeEffect>() != null)
        {
            EditorUtility.DisplayDialog("Sudah Ada", "Efek gempa sudah ada di scene ini.", "OK");
            return;
        }

        GameObject quakeObj = new GameObject("Efek_Gempa", typeof(EarthquakeEffect), typeof(AudioSource));
        Undo.RegisterCreatedObjectUndo(quakeObj, "Pasang Efek Gempa");

        AudioSource audio = quakeObj.GetComponent<AudioSource>();
        audio.playOnAwake = false;
        audio.loop = true;

        SerializedObject so = new SerializedObject(quakeObj.GetComponent<EarthquakeEffect>());
        so.FindProperty("rumbleAudio").objectReferenceValue = audio;

        // Overlay layar penuh di bawah HUD, supaya instruksi misi tetap terbaca
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas != null)
        {
            GameObject overlayObj = new GameObject("Overlay_Gempa", typeof(RectTransform), typeof(Image));
            Undo.RegisterCreatedObjectUndo(overlayObj, "Pasang Overlay Gempa");
            overlayObj.transform.SetParent(canvas.transform, false);
            overlayObj.transform.SetAsFirstSibling();

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
        Selection.activeGameObject = quakeObj;

        EditorUtility.DisplayDialog("Efek Gempa Terpasang",
            "Gempa otomatis aktif selama Level 2.\n\n" +
            "Opsional di Inspector 'Efek_Gempa':\n" +
            "• AudioSource → isi AudioClip suara gemuruh\n" +
            "• Falling Objects → seret benda yang jatuh (beri Rigidbody, centang Is Kinematic)", "Siap");
    }

    // ==========================================
    // LEVEL 3: TITIK KUMPUL + JALUR EVAKUASI
    // ==========================================
    [MenuItem("Tools/Level 3: Pasang Titik Kumpul di Posisi Terpilih")]
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
}
#endif
