#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Tool otomatis untuk memasang Collider secara presisi pada denah di scene.
/// Memastikan lantai dan dinding memiliki collider agar karakter tidak jatuh ke bawah void,
/// sekaligus menyaring dekorasi kecil, atap, dan kursi agar tidak menghalangi pergerakan atau kamera.
/// </summary>
public static class DenahColliderSetup
{
    // [MenuItem("Tools/Tukar Denah ke V2 di Scene Aktif")]
    public static void SwapToDenahV2()
    {
        // 1. Cari objek denah lama di scene
        GameObject oldDenah = GameObject.Find("denah_v2") ??
                              GameObject.Find("denah_tes") ?? 
                              GameObject.Find("denah_lengkap") ?? 
                              GameObject.Find("Environment_Buildings_Baru");
        
        Vector3 spawnPos = new Vector3(8.05f, 0.1280178f, -12.35f);
        Quaternion spawnRot = Quaternion.identity;
        Vector3 spawnScale = Vector3.one;

        if (oldDenah != null && oldDenah.name != "denah_v2")
        {
            spawnPos = oldDenah.transform.position;
            spawnRot = oldDenah.transform.rotation;
            spawnScale = oldDenah.transform.localScale;
            Undo.RegisterCompleteObjectUndo(oldDenah, "Nonaktifkan Denah Lama");
            oldDenah.SetActive(false);
            oldDenah.name = oldDenah.name + "_BACKUP_DISABLED";
        }

        // 2. Load prefab denah_v2.fbx
        GameObject v2Prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Scenes/denah_v2.fbx");
        if (v2Prefab == null)
        {
            EditorUtility.DisplayDialog("Gagal", "File 'Assets/Scenes/denah_v2.fbx' tidak ditemukan di Project! Pastikan file sudah ter-import.", "OK");
            return;
        }

        // 3. Instantiate denah_v2 di posisi yang tepat jika belum ada
        GameObject newDenah = GameObject.Find("denah_v2");
        if (newDenah == null)
        {
            newDenah = (GameObject)PrefabUtility.InstantiatePrefab(v2Prefab);
            Undo.RegisterCreatedObjectUndo(newDenah, "Instantiate denah_v2");
            newDenah.name = "denah_v2";
            newDenah.transform.position = spawnPos;
            newDenah.transform.rotation = spawnRot;
            newDenah.transform.localScale = spawnScale;
        }

        Selection.activeGameObject = newDenah;

        // 4. Langsung pasang collider presisi
        ApplyPreciseColliders();

        // 5. Pastikan Trigger Mulai Simulasi TV aktif dan GameManager menunggu trigger TV
        GameObject triggerTV = GameObject.Find("Trigger_Mulai_Simulasi");
        if (triggerTV != null)
        {
            triggerTV.SetActive(true);
            var cp = triggerTV.GetComponent<CheckpointTrigger>();
            if (cp != null) cp.enabled = true;
            var col = triggerTV.GetComponent<Collider>();
            if (col != null) col.enabled = true;
        }

        // Bersihkan collider raksasa pada Smart_TV_Level1 jika ada
        GameObject tvObj = GameObject.Find("Smart_TV_Level1") ?? GameObject.Find("denah_v2/Smart_TV_Level1");
        if (tvObj != null)
        {
            var tvCol = tvObj.GetComponent<Collider>();
            if (tvCol != null) Object.DestroyImmediate(tvCol);
            var tvCp = tvObj.GetComponent<CheckpointTrigger>();
            if (tvCp != null) Object.DestroyImmediate(tvCp);
        }

        var gm = Object.FindFirstObjectByType<GameManager>();
        if (gm != null)
        {
            gm.waitForStartTrigger = true;
            gm.enableLevel1Quiz = true;
            EditorUtility.SetDirty(gm);
        }

        // 6. Verifikasi komponen & skrip penting
        string report = "=== Verifikasi Status Game ===\n";
        
        report += gm != null ? "✔ GameManager: OK\n" : "⚠ GameManager: Tidak ditemukan di scene\n";

        var player = GameObject.Find("boy") ?? GameObject.FindWithTag("Player");
        report += player != null ? "✔ Karakter Pemain: OK\n" : "⚠ Karakter Pemain: Belum ada\n";

        var lvl2 = GameObject.Find("Objektif_Level2_KolongMeja");
        report += lvl2 != null ? "✔ Trigger Level 2 (Kolong Meja): OK\n" : "⚠ Trigger Level 2: Tidak ditemukan\n";

        var lvl3 = GameObject.Find("Titik_Kumpul_Level3");
        report += lvl3 != null ? "✔ Trigger Level 3 (Titik Kumpul): OK\n" : "⚠ Trigger Level 3: Tidak ditemukan\n";

        var startTrigger = GameObject.Find("Trigger_Mulai_Simulasi") ?? GameObject.Find("Penanda_Mulai_Simulasi");
        report += startTrigger != null ? "✔ Trigger Mulai Level 1: OK\n" : "⚠ Trigger Mulai Level 1: Cek di scene\n";

        EditorUtility.DisplayDialog("Tukar Denah Berhasil!", 
            "Denah baru (denah_v2) berhasil dipasang dan Mesh Collider presisi telah diaplikasikan!\n\n" + report +
            "\nCatatan: Lakukan Re-Bake NavMesh Surface jika ingin memperbarui jalur hijau evakuasi Level 3.", 
            "Mantap!");
    }

    [MenuItem("Tools/Environment/Pasang Collider Denah Presisi")]
    public static void ApplyPreciseColliders()
    {
        // 1. Cari objek denah di scene
        GameObject denah = Selection.activeGameObject;
        if (denah == null)
        {
            denah = GameObject.Find("denah_v2") ?? 
                    GameObject.Find("denah_tes") ?? 
                    GameObject.Find("denah_lengkap") ?? 
                    GameObject.Find("Environment_Buildings_Baru");
        }

        if (denah == null)
        {
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                if (go.name.ToLower().Contains("denah") || go.transform.Find("tembok kelas") != null)
                {
                    denah = go;
                    break;
                }
            }
        }

        if (denah == null)
        {
            EditorUtility.DisplayDialog("Tidak Ditemukan", "Objek denah tidak ditemukan di scene aktif. Pastikan model sudah ada di dalam hierarki!", "OK");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(denah, "Pasang Collider Denah Presisi");

        int collidersAdded = 0;
        int collidersIgnored = 0;

        MeshFilter[] meshFilters = denah.GetComponentsInChildren<MeshFilter>(true);

        foreach (var mf in meshFilters)
        {
            GameObject obj = mf.gameObject;
            string n = obj.name.ToLower();

            // A. ABAIKAN (JANGAN DIBERI COLLIDER / BERSIHKAN JIKA ADA)
            // - Atap & plafon (agar kamera TPS tidak terhalang)
            // - Dekorasi dinding: poster, bendera, mading, jam, AC, bintang
            // - Printilan kecil: kapur, buku, apel, balok, tas, penghapus
            // - Kursi: agar tidak memblokir akses anak merunduk masuk ke kolong meja
            if (n.Contains("atap") || 
                n.Contains("lampu") || 
                n.Contains("kipas") || 
                n.Contains("bendera") || 
                n.Contains("ac_") || 
                n.Contains("jam_") || 
                n.Contains("poster") || 
                n.Contains("papan_tulis_surface") || 
                n.Contains("papan_tulis_ledge") || 
                n.Contains("mading") || 
                n.Contains("balok") || 
                n.Contains("kapur") || 
                n.Contains("penghapus") || 
                n.Contains("buku") || 
                n.Contains("apel") || 
                n.Contains("tas_") || 
                n.Contains("kursi") || 
                n.Contains("jerapah") || 
                n.Contains("pointlight") || 
                n.Contains("empty") || 
                n.Contains("bintang"))
            {
                Collider existingCol = obj.GetComponent<Collider>();
                if (existingCol != null)
                {
                    Object.DestroyImmediate(existingCol);
                }
                collidersIgnored++;
                continue;
            }

            // B. MEJA BELAJAR (Hanya beri collider di papan atas, kolong tetap bolong)
            if (n.Contains("meja_belajar") || n.Contains("mid_meja"))
            {
                BoxCollider bc = obj.GetComponent<BoxCollider>();
                if (bc == null)
                {
                    bc = obj.AddComponent<BoxCollider>();
                }
                bc.size = new Vector3(2.2f, 0.1f, 0.9f);
                bc.center = new Vector3(0f, 0.5f, 0f);
                collidersAdded++;
                continue;
            }

            // C. LANTAI, JALAN, DINDING, BANGUNAN, DAN OBSTACLE UTAMA (Agar tidak jatuh & tidak tembus)
            if (n.Contains("aspal") || 
                n.Contains("paving") || 
                n.Contains("lapangan") || 
                n.Contains("plane") || 
                n.Contains("tembok") || 
                n.Contains("pintu") || 
                n.Contains("fence") || 
                n.Contains("pagar") || 
                n.Contains("merc_lp") || 
                n.Contains("rak_") || 
                n.Contains("ayunan") || 
                n.Contains("pohon") || 
                n.Contains("gedung") ||
                n.Contains("balai") ||
                n.Contains("paud") ||
                n.Contains("rumah") ||
                n.Contains("kanstin") ||
                n.Contains("ground") ||
                n.Contains("rumput") ||
                n.Contains("walkway") ||
                n.Contains("connector") ||
                n.Contains("papan_tulis_level2"))
            {
                MeshCollider mc = obj.GetComponent<MeshCollider>();
                if (mc == null)
                {
                    mc = obj.AddComponent<MeshCollider>();
                }
                mc.convex = false; // Non-convex agar lantai dan jalan rata dilewati
                collidersAdded++;
                continue;
            }
        }

        EditorUtility.SetDirty(denah);

        EditorUtility.DisplayDialog("Sukses!", 
            $"Collider presisi berhasil dipasang pada '{denah.name}'!\n\n" +
            $"• {collidersAdded} Mesh/Box Collider dipasang pada Lantai, Dinding, Jalan, Lapangan, & Bangunan (karakter dijamin tidak akan jatuh).\n" +
            $"• {collidersIgnored} Dekorasi kecil, atap, dan kursi diabaikan agar pergerakan & kamera tetap lancar.", 
            "Mantap!");
    }
}
#endif
