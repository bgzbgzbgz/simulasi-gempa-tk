#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Tool Editor untuk memasang gambar Denah sebagai alas referensi (Blueprint Floor) di Scene.
/// Memudahkan pemodelan menggunakan ProBuilder dan Polybrush dengan skala 1:1.
/// </summary>
public static class DenahReferenceSetup
{
    private const string BlueprintObjectName = "--- DENAH REFERENSI (BLUEPRINT) ---";
    private const string ImagePath = "Assets/References/Denah_Keterangan.jpg";

    [MenuItem("Tools/Pasang Denah Referensi di Scene")]
    public static void SetupBlueprintPlane()
    {
        // 1. Cek apakah sudah ada objek blueprint
        GameObject existing = GameObject.Find(BlueprintObjectName);
        if (existing != null)
        {
            if (EditorUtility.DisplayDialog(
                "Blueprint Sudah Ada",
                "Objek '" + BlueprintObjectName + "' sudah ada di scene. Ingin menaruh ulang?",
                "Ya, Taruh Ulang",
                "Batal"))
            {
                Undo.DestroyObjectImmediate(existing);
            }
            else
            {
                Selection.activeGameObject = existing;
                return;
            }
        }

        // 2. Load texture denah
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(ImagePath);
        if (texture == null)
        {
            EditorUtility.DisplayDialog("Gambar Tidak Ditemukan", "File tidak ditemukan di: " + ImagePath, "OK");
            return;
        }

        // 3. Buat Material Unlit agar denah terlihat jelas tanpa terpengaruh bayangan
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Texture") ?? Shader.Find("Standard");
        Material mat = new Material(shader)
        {
            name = "Mat_DenahBlueprint",
            mainTexture = texture
        };

        // Simpan asset material ke folder References
        string matPath = "Assets/References/Mat_DenahBlueprint.mat";
        AssetDatabase.CreateAsset(mat, matPath);

        // 4. Buat Plane 3D
        GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        plane.name = BlueprintObjectName;
        Undo.RegisterCreatedObjectUndo(plane, "Create Blueprint Plane");

        // Posisikan sedikit di bawah lantai (Y = -0.02) agar tidak bentrok (Z-fighting) dengan lantai ProBuilder
        plane.transform.position = new Vector3(0f, -0.02f, 0f);

        // Hitung skala berdasarkan aspect ratio gambar denah (ukuran default ~35m x 25m)
        float aspect = (float)texture.width / texture.height;
        float heightScale = 3.5f; // Plane bawaan Unity berukuran 10x10 meter per 1.0 scale
        float widthScale = heightScale * aspect;

        plane.transform.localScale = new Vector3(widthScale, 1f, heightScale);

        // Pasang material
        Renderer rend = plane.GetComponent<Renderer>();
        if (rend != null)
        {
            rend.sharedMaterial = mat;
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows = false;
        }

        // Matikan collider agar tidak mengganggu klik ProBuilder / raycast karakter
        Collider col = plane.GetComponent<Collider>();
        if (col != null)
        {
            Object.DestroyImmediate(col);
        }

        Selection.activeGameObject = plane;
        EditorGUIUtility.PingObject(plane);

        EditorUtility.DisplayDialog(
            "Berhasil!",
            "Denah referensi berhasil dipasang di scene!\nKamu bisa menggunakan ProBuilder untuk mulai menarik dinding/lantai sesuai garis denah.",
            "OK");
    }
}
#endif
