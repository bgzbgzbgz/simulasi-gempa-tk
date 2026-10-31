#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Tool Editor untuk membuat layout Blockout Ruang Kelas TK otomatis dengan 1-klik.
/// Dilengkapi dinding, lantai, bukaan pintu evakuasi, papan tulis, serta 4 set meja perlindungan gempa.
/// </summary>
public static class ClassroomBlockoutSetup
{
    private const string RootName = "--- CLASSROOM BLOCKOUT ---";

    [MenuItem("Tools/Buat Blockout Kelas TK (1-Klik)")]
    public static void GenerateClassroom()
    {
        // 1. Bersihkan blockout lama jika ada
        GameObject existing = GameObject.Find(RootName);
        if (existing != null)
        {
            if (EditorUtility.DisplayDialog(
                "Ganti Blockout?",
                "Sudah ada objek '" + RootName + "' di scene. Apakah ingin membuat ulang?",
                "Ya, Buat Ulang",
                "Batal"))
            {
                Undo.DestroyObjectImmediate(existing);
            }
            else
            {
                return;
            }
        }

        // 2. Siapkan Material Berwarna agar visual langsung menarik
        Shader urpShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

        Material matFloor = CreateTemporaryMaterial(urpShader, "Mat_Floor", new Color(0.92f, 0.88f, 0.82f));       // Krem kayu lembut
        Material matWall = CreateTemporaryMaterial(urpShader, "Mat_Wall", new Color(0.85f, 0.92f, 0.88f));         // Pastel mint
        Material matDesk = CreateTemporaryMaterial(urpShader, "Mat_Desk", new Color(0.85f, 0.62f, 0.40f));         // Kayu meja
        Material matBoard = CreateTemporaryMaterial(urpShader, "Mat_Board", new Color(0.18f, 0.28f, 0.24f));       // Hijau papan tulis
        Material matExit = CreateTemporaryMaterial(urpShader, "Mat_Exit", new Color(0.15f, 0.75f, 0.35f));         // Hijau jalur evakuasi
        Material matTeacherDesk = CreateTemporaryMaterial(urpShader, "Mat_TeacherDesk", new Color(0.70f, 0.48f, 0.30f));

        // 3. Buat Root Parent
        GameObject root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Create Classroom Blockout");

        // 4. Buat Lantai (12m x 10m)
        GameObject floor = CreatePrimitiveBox("Lantai Kelas", root.transform, new Vector3(0f, -0.1f, 0f), new Vector3(12f, 0.2f, 10f), matFloor);

        // 5. Buat Dinding Kelas (Tinggi 3 meter)
        // Dinding Belakang (Z = +5)
        CreatePrimitiveBox("Dinding Belakang", root.transform, new Vector3(0f, 1.5f, 5f), new Vector3(12f, 3f, 0.3f), matWall);

        // Dinding Samping Kiri (X = -6)
        CreatePrimitiveBox("Dinding Kiri", root.transform, new Vector3(-6f, 1.5f, 0f), new Vector3(0.3f, 3f, 10f), matWall);

        // Dinding Samping Kanan (X = +6)
        CreatePrimitiveBox("Dinding Kanan", root.transform, new Vector3(6f, 1.5f, 0f), new Vector3(0.3f, 3f, 10f), matWall);

        // Dinding Depan dengan Buka Pintu Evakuasi (Z = -5)
        // Bagian Kiri Dinding Depan (panjang 7m, dari X=-6 s/d X=+1)
        CreatePrimitiveBox("Dinding Depan Kiri", root.transform, new Vector3(-2.5f, 1.5f, -5f), new Vector3(7f, 3f, 0.3f), matWall);

        // Ambang Atas Pintu (Lintel di atas lubang pintu keluar X=+1 s/d X=+3, Y=2.2 s/d 3.0)
        CreatePrimitiveBox("Ambang Pintu Keluar", root.transform, new Vector3(2f, 2.6f, -5f), new Vector3(2f, 0.8f, 0.3f), matWall);

        // Bagian Kanan Dinding Depan (panjang 3m, dari X=+3 s/d X=+6)
        CreatePrimitiveBox("Dinding Depan Kanan", root.transform, new Vector3(4.5f, 1.5f, -5f), new Vector3(3f, 3f, 0.3f), matWall);

        // Jalur / Pintu Evakuasi (Floor Marker Hijau)
        CreatePrimitiveBox("Marker Pintu Evakuasi", root.transform, new Vector3(2f, 0.01f, -5.2f), new Vector3(2f, 0.02f, 1.5f), matExit);

        // 6. Papan Tulis Kelas
        CreatePrimitiveBox("Papan Tulis", root.transform, new Vector3(-2.5f, 1.7f, -4.83f), new Vector3(4f, 1.4f, 0.05f), matBoard);

        // 7. Meja Guru
        CreatePrimitiveBox("Meja Guru", root.transform, new Vector3(4.5f, 0.4f, -3.8f), new Vector3(1.8f, 0.8f, 0.9f), matTeacherDesk);

        // 8. Buat 4 Kelompok Meja Siswa TK dengan Trigger Kolong Meja
        Transform deskContainer = new GameObject("Meja Siswa TK").transform;
        deskContainer.SetParent(root.transform, false);

        Vector3[] tablePositions = new Vector3[]
        {
            new Vector3(-2.8f, 0f, 2.0f),
            new Vector3(2.8f, 0f, 2.0f),
            new Vector3(-2.8f, 0f, -1.0f),
            new Vector3(2.8f, 0f, -1.0f)
        };

        for (int i = 0; i < tablePositions.Length; i++)
        {
            CreateShelterTable($"Meja Siswa {i + 1}", deskContainer, tablePositions[i], matDesk);
        }

        // 9. Reposisi Karakter Boy ke tengah ruangan jika ada
        BoyController boy = Object.FindFirstObjectByType<BoyController>();
        if (boy != null)
        {
            Undo.RecordObject(boy.transform, "Center Boy Position");
            boy.transform.position = new Vector3(0f, 0.05f, 0.5f);
            boy.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        }

        Selection.activeGameObject = root;

        EditorUtility.DisplayDialog(
            "Blockout Kelas TK Selesai!",
            "Layout Ruang Kelas TK berhasil dibuat!\n\n" +
            "Fitur yang disertakan:\n" +
            "• Ruangan Kelas (12 x 10 meter) berdinding pastel.\n" +
            "• Pintu Evakuasi di dinding depan samping (untuk Level 3).\n" +
            "• Papan tulis & Meja guru sebagai penanda depan kelas.\n" +
            "• 4 Meja Siswa dengan TRIGGER KOLONG MEJA (Level 2 Drop-Cover).\n\n" +
            "Coba jalankan Play mode dan bawa karakter masuk ke kolong meja!",
            "Keren!");
    }

    private static GameObject CreatePrimitiveBox(string name, Transform parent, Vector3 localPos, Vector3 size, Material material)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.transform.SetParent(parent, false);
        box.transform.localPosition = localPos;
        box.transform.localScale = size;
        if (material != null)
        {
            box.GetComponent<MeshRenderer>().sharedMaterial = material;
        }
        return box;
    }

    private static void CreateShelterTable(string name, Transform parent, Vector3 position, Material matDesk)
    {
        GameObject tableGroup = new GameObject(name);
        tableGroup.transform.SetParent(parent, false);
        tableGroup.transform.localPosition = position;

        float width = 1.6f;
        float depth = 1.0f;
        float height = 0.9f;
        float topThickness = 0.08f;
        float legThickness = 0.08f;

        // 1. Daun Meja (Top)
        GameObject top = GameObject.CreatePrimitive(PrimitiveType.Cube);
        top.name = "Daun Meja";
        top.transform.SetParent(tableGroup.transform, false);
        top.transform.localPosition = new Vector3(0f, height - topThickness * 0.5f, 0f);
        top.transform.localScale = new Vector3(width, topThickness, depth);
        top.GetComponent<MeshRenderer>().sharedMaterial = matDesk;

        // 2. 4 Kaki Meja
        float halfW = width * 0.5f - legThickness * 0.5f;
        float halfD = depth * 0.5f - legThickness * 0.5f;
        float legH = height - topThickness;

        Vector3[] legOffsets = new Vector3[]
        {
            new Vector3(-halfW, legH * 0.5f, -halfD),
            new Vector3(halfW, legH * 0.5f, -halfD),
            new Vector3(-halfW, legH * 0.5f, halfD),
            new Vector3(halfW, legH * 0.5f, halfD)
        };

        for (int i = 0; i < legOffsets.Length; i++)
        {
            GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leg.name = $"Kaki {i + 1}";
            leg.transform.SetParent(tableGroup.transform, false);
            leg.transform.localPosition = legOffsets[i];
            leg.transform.localScale = new Vector3(legThickness, legH, legThickness);
            leg.GetComponent<MeshRenderer>().sharedMaterial = matDesk;
        }

        // 3. Kolong Meja Trigger (Shelter Zone)
        GameObject triggerObj = new GameObject("ShelterZone_KolongMeja");
        triggerObj.transform.SetParent(tableGroup.transform, false);
        triggerObj.transform.localPosition = new Vector3(0f, legH * 0.5f, 0f);

        BoxCollider triggerCol = triggerObj.AddComponent<BoxCollider>();
        triggerCol.isTrigger = true;
        triggerCol.size = new Vector3(width - 0.2f, legH, depth - 0.2f);

        ShelterZone shelter = triggerObj.AddComponent<ShelterZone>();
        SerializedObject serialized = new SerializedObject(shelter);
        serialized.FindProperty("zoneName").stringValue = name;
        serialized.ApplyModifiedProperties();
    }

    private static Material CreateTemporaryMaterial(Shader shader, string name, Color color)
    {
        Material mat = new Material(shader);
        mat.name = name;
        mat.color = color;
        return mat;
    }
}
#endif
