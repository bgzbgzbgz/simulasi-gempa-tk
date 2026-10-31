#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Generator 1-Klik untuk Denah Skenario Evakuasi TK Nyata (Level 1, 2, 3).
/// Mencakup Ruang Kelas TK, Koridor, Jalan Gang Berkelok, dan Lapangan Titik Kumpul.
/// Dilengkapi juga dengan fitur Export ke format OBJ untuk di-edit lebih detail di Blender.
/// </summary>
public static class RealScenarioGenerator
{
    private const string RootName = "--- REAL SCENARIO EVACUATION ---";

    [MenuItem("Tools/Generate Skenario Evakuasi Lengkap (Real TK)")]
    public static void GenerateScenario()
    {
        // 1. Bersihkan scene dari skenario lama jika ada
        GameObject existing = GameObject.Find(RootName);
        if (existing != null)
        {
            if (EditorUtility.DisplayDialog(
                "Generate Ulang Skenario?",
                "Objek '" + RootName + "' sudah ada di scene. Ingin menimpa ulang?",
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

        // 2. Siapkan Material
        Shader urpShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

        Material matClassFloor = CreateMat(urpShader, "Mat_ClassFloor", new Color(0.92f, 0.90f, 0.84f));   // Keramik terang
        Material matClassWall  = CreateMat(urpShader, "Mat_ClassWall", new Color(0.82f, 0.92f, 0.88f));    // Pastel mint ceria
        Material matCorridor   = CreateMat(urpShader, "Mat_Corridor", new Color(0.78f, 0.80f, 0.82f));     // Abu paving lorong
        Material matGangFloor  = CreateMat(urpShader, "Mat_GangFloor", new Color(0.55f, 0.65f, 0.72f));    // Jalan gang biru/aspal
        Material matGangWall   = CreateMat(urpShader, "Mat_GangWall", new Color(0.88f, 0.85f, 0.80f));     // Tembok rumah pemukiman
        Material matField      = CreateMat(urpShader, "Mat_Field", new Color(0.76f, 0.68f, 0.50f));        // Lapangan tanah berpasir
        Material matDesk       = CreateMat(urpShader, "Mat_DeskReal", new Color(0.82f, 0.45f, 0.25f));     // Meja coklat kayu
        Material matBoard      = CreateMat(urpShader, "Mat_BoardReal", new Color(0.15f, 0.28f, 0.20f));    // Papan tulis hijau
        Material matTV         = CreateMat(urpShader, "Mat_TVReal", new Color(0.12f, 0.12f, 0.15f));       // Smart TV hitam
        Material matAssembly   = CreateMat(urpShader, "Mat_Assembly", new Color(0.20f, 0.80f, 0.35f));     // Titik kumpul hijau

        // 3. Buat Root
        GameObject root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Generate Real Scenario Layout");

        // Offset Koordinat Pusat:
        // Kelas berada di sisi Kanan Bawah: X ~ 4..9, Z ~ -7..5
        // Lorong berada di X ~ 9..11.5
        // Gang berada di Z ~ 6..12 berbelok ke X ~ -3..11 lalu ke Z ~ 0..6
        // Lapangan berada di Kiri: X ~ -15..-3, Z ~ -8..5

        // ==========================================
        // A. RUANG KELAS TK (Level 1 & Level 2)
        // ==========================================
        Transform classParent = new GameObject("1_Ruang_Kelas_TK").transform;
        classParent.SetParent(root.transform, false);

        // Lantai Kelas (5m lebar x 12m panjang)
        CreateBox("Lantai_Kelas", classParent, new Vector3(6.5f, -0.1f, -1f), new Vector3(5f, 0.2f, 12f), matClassFloor);

        // Dinding Luar Kelas
        // Dinding Bawah Kelas (Z = -7)
        CreateBox("Dinding_Bawah_Kelas", classParent, new Vector3(6.5f, 1.4f, -7f), new Vector3(5f, 2.8f, 0.3f), matClassWall);
        // Dinding Kiri Kelas (X = 4)
        CreateBox("Dinding_Kiri_Kelas", classParent, new Vector3(4f, 1.4f, -1f), new Vector3(0.3f, 2.8f, 12f), matClassWall);
        // Dinding Atas Kelas (Z = +5) - Tempat Papan Tulis
        CreateBox("Dinding_Atas_Kelas", classParent, new Vector3(6.5f, 1.4f, 5f), new Vector3(5f, 2.8f, 0.3f), matClassWall);

        // Dinding Kanan Kelas (dengan Lubang Pintu keluar ke Koridor di Z = 3.5 dan Z = -3)
        // Bagian dinding kanan bawah
        CreateBox("Dinding_Kanan_Bawah", classParent, new Vector3(9f, 1.4f, -5f), new Vector3(0.3f, 2.8f, 3.8f), matClassWall);
        // Bagian dinding kanan tengah
        CreateBox("Dinding_Kanan_Tengah", classParent, new Vector3(9f, 1.4f, 0.2f), new Vector3(0.3f, 2.8f, 4.4f), matClassWall);
        // Bagian dinding kanan atas
        CreateBox("Dinding_Kanan_Atas", classParent, new Vector3(9f, 1.4f, 4.5f), new Vector3(0.3f, 2.8f, 1f), matClassWall);
        // Balok atas pintu (Lintel)
        CreateBox("Kusen_Atas_Pintu_Keluar", classParent, new Vector3(9f, 2.5f, 2.8f), new Vector3(0.3f, 0.6f, 1.6f), matClassWall);

        // Sekat Pemisah Level 1 (Bawah) dan Level 2 (Atas) di Z = -2.5
        CreateBox("Sekat_Kiri", classParent, new Vector3(5f, 1.4f, -2.5f), new Vector3(2f, 2.8f, 0.25f), matClassWall);
        CreateBox("Sekat_Kanan", classParent, new Vector3(8.2f, 1.4f, -2.5f), new Vector3(1.6f, 2.8f, 0.25f), matClassWall);
        CreateBox("Sekat_Atas_Pintu", classParent, new Vector3(6.7f, 2.5f, -2.5f), new Vector3(1.4f, 0.6f, 0.25f), matClassWall);

        // Papan Tulis di Dinding Atas
        CreateBox("Papan_Tulis", classParent, new Vector3(6.5f, 1.4f, 4.82f), new Vector3(2.8f, 1.2f, 0.08f), matBoard);

        // Smart TV di Sekat Pemisah
        CreateBox("Smart_TV_Layar", classParent, new Vector3(5.2f, 1.3f, -2.62f), new Vector3(1.4f, 0.8f, 0.05f), matTV);

        // 3 Meja Murid di Level 2 (dengan ShelterZone)
        CreateShelterDesk("Meja_Murid_1", classParent, new Vector3(6.5f, 0f, 3.2f), matDesk);
        CreateShelterDesk("Meja_Murid_2", classParent, new Vector3(6.5f, 0f, 1.2f), matDesk);
        CreateShelterDesk("Meja_Murid_3", classParent, new Vector3(6.5f, 0f, -0.8f), matDesk);

        // ==========================================
        // B. KORIDOR / SELASAR KELAS
        // ==========================================
        Transform corridorParent = new GameObject("2_Koridor_Kelas").transform;
        corridorParent.SetParent(root.transform, false);

        // Lantai Koridor (Lebar 2.5m, sejajar kelas ke arah utara)
        CreateBox("Lantai_Koridor", corridorParent, new Vector3(10.25f, -0.1f, 0f), new Vector3(2.5f, 0.2f, 14f), matCorridor);
        // Dinding Luar Kanan Koridor
        CreateBox("Dinding_Luar_Koridor", corridorParent, new Vector3(11.5f, 1.4f, 0f), new Vector3(0.3f, 2.8f, 14f), matGangWall);

        // ==========================================
        // C. JALAN GANG BERKELOK (Street Alley)
        // ==========================================
        Transform gangParent = new GameObject("3_Jalan_Gang").transform;
        gangParent.SetParent(root.transform, false);

        // Segmen 1: Lanjutan dari Koridor ke Utara (Z = 7 .. 11)
        CreateBox("Lantai_Gang_Utara", gangParent, new Vector3(10.25f, -0.1f, 9.5f), new Vector3(2.5f, 0.2f, 5f), matGangFloor);
        CreateBox("Dinding_Gang_Timur", gangParent, new Vector3(11.5f, 1.4f, 9.5f), new Vector3(0.3f, 2.8f, 5f), matGangWall);

        // Segmen 2: Belok ke Kiri / Barat (X = -1 .. 10.25, Z = 11)
        CreateBox("Lantai_Gang_Barat", gangParent, new Vector3(4.5f, -0.1f, 11f), new Vector3(14f, 0.2f, 2.5f), matGangFloor);
        CreateBox("Dinding_Gang_Utara_Luar", gangParent, new Vector3(4.5f, 1.4f, 12.25f), new Vector3(14.3f, 2.8f, 0.3f), matGangWall);
        CreateBox("Dinding_Gang_Utara_Dalam", gangParent, new Vector3(4f, 1.4f, 9.75f), new Vector3(12f, 2.8f, 0.3f), matGangWall);

        // Segmen 3: Belok ke Selatan / Bawah (X = -1.5, Z = 4 .. 11)
        CreateBox("Lantai_Gang_Selatan", gangParent, new Vector3(-1.5f, -0.1f, 7.5f), new Vector3(2.5f, 0.2f, 7f), matGangFloor);
        CreateBox("Dinding_Gang_Timur_Seg3", gangParent, new Vector3(-0.25f, 1.4f, 6.5f), new Vector3(0.3f, 2.8f, 7f), matGangWall);

        // Segmen 4: Belok ke Kiri / Barat Menuju Lapangan (X = -5 .. -1.5, Z = 4)
        CreateBox("Lantai_Gang_Pintu_Lapangan", gangParent, new Vector3(-4f, -0.1f, 4f), new Vector3(5f, 0.2f, 2.5f), matGangFloor);
        CreateBox("Dinding_Gang_Utara_Seg4", gangParent, new Vector3(-4f, 1.4f, 5.25f), new Vector3(5.3f, 2.8f, 0.3f), matGangWall);

        // ==========================================
        // D. LAPANGAN TERBUKA / TITIK KUMPUL (Level 3)
        // ==========================================
        Transform fieldParent = new GameObject("4_Lapangan_Titik_Kumpul").transform;
        fieldParent.SetParent(root.transform, false);

        // Lantai Lapangan Tanah Luas (14m lebar x 16m panjang)
        CreateBox("Lantai_Lapangan", fieldParent, new Vector3(-13.5f, -0.1f, -3f), new Vector3(14f, 0.2f, 16f), matField);

        // Tembok Keliling Lapangan
        // Tembok Atas Lapangan (Z = 5)
        CreateBox("Pagar_Atas_Lapangan", fieldParent, new Vector3(-13.5f, 1.1f, 5f), new Vector3(14f, 2.2f, 0.3f), matGangWall);
        // Tembok Kiri Lapangan (X = -20.5)
        CreateBox("Pagar_Kiri_Lapangan", fieldParent, new Vector3(-20.5f, 1.1f, -3f), new Vector3(0.3f, 2.2f, 16f), matGangWall);
        // Tembok Bawah Lapangan (Z = -11)
        CreateBox("Pagar_Bawah_Lapangan", fieldParent, new Vector3(-13.5f, 1.1f, -11f), new Vector3(14f, 2.2f, 0.3f), matGangWall);
        // Tembok Kanan Lapangan (dengan bukaan pintu gerbang di Z = 3.5 .. 5)
        CreateBox("Pagar_Kanan_Bawah_Lapangan", fieldParent, new Vector3(-6.5f, 1.1f, -4f), new Vector3(0.3f, 2.2f, 14f), matGangWall);

        // Titik Kumpul (Assembly Point Zone) di tengah lapangan
        CreateAssemblyPoint("Zona_Titik_Kumpul", fieldParent, new Vector3(-13.5f, 0f, -3f), matAssembly);

        Selection.activeGameObject = root;
        Undo.CollapseUndoOperations(Undo.GetCurrentGroup());

        EditorUtility.DisplayDialog(
            "Skenario Berhasil Dibuat!",
            "Denah Skenario Evakuasi TK Lengkap berhasil dibuat!\n\n" +
            "• Ruang Kelas (Level 1 & 2) lengkap dengan 3 meja & ShelterZone\n" +
            "• Koridor & Jalan Gang Berkelok\n" +
            "• Lapangan Terbuka Titik Kumpul (Level 3)\n\n" +
            "Kamu bisa langsung Play untuk mencoba berjalan, atau Export ke OBJ untuk edit di Blender via menu 'Tools/Export Denah ke OBJ'!",
            "Siap!");
    }

    private static GameObject CreateBox(string name, Transform parent, Vector3 localPos, Vector3 size, Material mat)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = size;

        Renderer r = go.GetComponent<Renderer>();
        if (r != null && mat != null)
        {
            r.sharedMaterial = mat;
        }

        return go;
    }

    private static void CreateShelterDesk(string name, Transform parent, Vector3 pos, Material matDesk)
    {
        GameObject deskRoot = new GameObject(name);
        deskRoot.transform.SetParent(parent, false);
        deskRoot.transform.localPosition = pos;

        // Papan Meja (Panjang 1.6m, Lebar 0.8m, Tebal 0.08m, Tinggi 0.65m pas untuk anak TK)
        CreateBox("Papan_Meja", deskRoot.transform, new Vector3(0f, 0.65f, 0f), new Vector3(1.6f, 0.08f, 0.8f), matDesk);

        // Kaki Meja
        float legH = 0.61f;
        float legY = 0.305f;
        float legX = 0.72f;
        float legZ = 0.32f;
        Vector3 legScale = new Vector3(0.08f, legH, 0.08f);

        CreateBox("Kaki_Kiri_Depan", deskRoot.transform, new Vector3(-legX, legY, legZ), legScale, matDesk);
        CreateBox("Kaki_Kanan_Depan", deskRoot.transform, new Vector3(legX, legY, legZ), legScale, matDesk);
        CreateBox("Kaki_Kiri_Belakang", deskRoot.transform, new Vector3(-legX, legY, -legZ), legScale, matDesk);
        CreateBox("Kaki_Kanan_Belakang", deskRoot.transform, new Vector3(legX, legY, -legZ), legScale, matDesk);

        // Trigger Kolong Meja (ShelterZone)
        GameObject zoneObj = new GameObject("ShelterZone_Trigger");
        zoneObj.transform.SetParent(deskRoot.transform, false);
        zoneObj.transform.localPosition = new Vector3(0f, 0.32f, 0f);

        BoxCollider col = zoneObj.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = new Vector3(1.5f, 0.64f, 0.76f);

        ShelterZone sz = zoneObj.AddComponent<ShelterZone>();
        // Gunakan reflection/serialized object untuk mengisi zoneName agar informatif
        SerializedObject so = new SerializedObject(sz);
        SerializedProperty sp = so.FindProperty("zoneName");
        if (sp != null)
        {
            sp.stringValue = "Kolong " + name;
            so.ApplyModifiedProperties();
        }
    }

    private static void CreateAssemblyPoint(string name, Transform parent, Vector3 pos, Material mat)
    {
        GameObject zone = new GameObject(name);
        zone.transform.SetParent(parent, false);
        zone.transform.localPosition = pos;

        // Lingkaran / Kotak Area Hijau Titik Kumpul
        CreateBox("Visual_Lantai_Titik_Kumpul", zone.transform, new Vector3(0f, 0.02f, 0f), new Vector3(5f, 0.05f, 5f), mat);

        // Tiang Papan Titik Kumpul
        GameObject pole = CreateBox("Tiang_Papan", zone.transform, new Vector3(0f, 1.25f, 2.2f), new Vector3(0.12f, 2.5f, 0.12f), mat);
        GameObject sign = CreateBox("Papan_Assembly_Point", zone.transform, new Vector3(0f, 2.3f, 2.2f), new Vector3(1.2f, 0.8f, 0.08f), mat);

        // Trigger Box Area Kumpul
        BoxCollider col = zone.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = new Vector3(5f, 2f, 5f);
        col.center = new Vector3(0f, 1f, 0f);
    }

    private static Material CreateMat(Shader shader, string name, Color color)
    {
        Material mat = new Material(shader)
        {
            name = name,
            color = color
        };
        return mat;
    }

    // =========================================================
    // FITUR EXPORT OBJ UNTUK DIBUKA & DI-DETAILKAN DI BLENDER
    // =========================================================
    [MenuItem("Tools/Export Denah ke OBJ (Untuk Edit di Blender)")]
    public static void ExportScenarioToObj()
    {
        GameObject root = GameObject.Find(RootName);
        if (root == null)
        {
            EditorUtility.DisplayDialog("Tidak Ditemukan", "Objek '" + RootName + "' belum ada di scene. Silakan generate dulu!", "OK");
            return;
        }

        MeshFilter[] filters = root.GetComponentsInChildren<MeshFilter>();
        if (filters.Length == 0)
        {
            EditorUtility.DisplayDialog("Kosong", "Tidak ada mesh yang ditemukan untuk diexport.", "OK");
            return;
        }

        string savePath = EditorUtility.SaveFilePanel("Export Denah ke OBJ", "Assets", "Denah_Skenario_TK", "obj");
        if (string.IsNullOrEmpty(savePath))
        {
            return;
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("# Exported from Unity RealScenarioGenerator for Blender");
        sb.AppendLine("o " + root.name);

        int vertexOffset = 1;
        int normalOffset = 1;
        int uvOffset = 1;

        foreach (MeshFilter mf in filters)
        {
            Mesh mesh = mf.sharedMesh;
            if (mesh == null) continue;

            Transform t = mf.transform;
            sb.AppendLine("g " + mf.gameObject.name);

            // Vertices dalam World Space
            foreach (Vector3 v in mesh.vertices)
            {
                Vector3 wv = t.TransformPoint(v);
                // Konversi sumbu Z Unity (Left-handed) ke OBJ standar
                sb.AppendLine(string.Format(System.Globalization.CultureInfo.InvariantCulture, "v {0:F4} {1:F4} {2:F4}", wv.x, wv.y, wv.z));
            }

            // Normals
            foreach (Vector3 n in mesh.normals)
            {
                Vector3 wn = t.TransformDirection(n);
                sb.AppendLine(string.Format(System.Globalization.CultureInfo.InvariantCulture, "vn {0:F4} {1:F4} {2:F4}", wn.x, wn.y, wn.z));
            }

            // UVs
            foreach (Vector2 uv in mesh.uv)
            {
                sb.AppendLine(string.Format(System.Globalization.CultureInfo.InvariantCulture, "vt {0:F4} {1:F4}", uv.x, uv.y));
            }

            // Triangles
            for (int i = 0; i < mesh.subMeshCount; i++)
            {
                int[] triangles = mesh.GetTriangles(i);
                for (int m = 0; m < triangles.Length; m += 3)
                {
                    int i1 = triangles[m] + vertexOffset;
                    int i2 = triangles[m + 1] + vertexOffset;
                    int i3 = triangles[m + 2] + vertexOffset;
                    sb.AppendLine(string.Format("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}", i1, i2, i3));
                }
            }

            vertexOffset += mesh.vertexCount;
            normalOffset += mesh.normals.Length;
            uvOffset += mesh.uv.Length;
        }

        File.WriteAllText(savePath, sb.ToString());
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Export Sukses!",
            "File OBJ berhasil disimpan di:\n" + savePath + "\n\nKamu bisa langsung import file ini ke Blender untuk menambahkan bevel, dekorasi atap, jendela, atau tekstur kustom!",
            "Mantap!");
    }
}
#endif
