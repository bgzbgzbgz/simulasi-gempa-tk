#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Tool otomatis untuk memasang Collider secara presisi pada denah_tes di scene.
/// Memastikan lantai dan dinding memiliki collider agar karakter tidak jatuh ke bawah void,
/// sekaligus menyaring dekorasi kecil, atap, dan kursi agar tidak menghalangi pergerakan atau kamera.
/// </summary>
public static class DenahColliderSetup
{
    [MenuItem("Tools/Pasang Collider Denah Presisi")]
    public static void ApplyPreciseColliders()
    {
        // 1. Cari objek denah_tes di scene
        GameObject denah = GameObject.Find("denah_tes");
        if (denah == null)
        {
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                if (go.name.ToLower().Contains("denah_tes") || go.transform.Find("tembok kelas") != null)
                {
                    denah = go;
                    break;
                }
            }
        }

        if (denah == null)
        {
            EditorUtility.DisplayDialog("Tidak Ditemukan", "Objek 'denah_tes' tidak ditemukan di scene aktif. Pastikan model sudah ada di dalam hierarki!", "OK");
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
            if (n.Contains("meja_belajar"))
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

            // C. LANTAI, JALAN, DINDING, DAN OBSTACLE UTAMA
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
            $"Collider presisi berhasil dipasang!\n\n" +
            $"• {collidersAdded} Collider dipasang pada Lantai, Dinding, Jalan Paving/Aspal, & Lapangan (karakter tidak akan jatuh).\n" +
            $"• {collidersIgnored} Dekorasi kecil, atap, dan kursi diabaikan agar pergerakan & kamera tetap lancar.", 
            "Mantap!");
    }
}
#endif
