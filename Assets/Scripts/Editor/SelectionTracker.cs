#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Text;

/// <summary>
/// Mencatat detail lengkap objek yang dipilih di Unity untuk diagnosa presisi.
/// </summary>
[InitializeOnLoad]
public static class SelectionTracker
{
    private static readonly string FilePath = "Temp/active_selection.json";

    static SelectionTracker()
    {
        Selection.selectionChanged += OnSelectionChanged;
    }

    // [MenuItem("Tools/Diagnosa Collider Meja dan Karakter")]
    public static void DiagnoseSelection()
    {
        OnSelectionChanged();
        EditorUtility.DisplayDialog("Diagnosa Selesai", "Data collider telah diperbarui dan siap dianalisis!", "OK");
    }

    private static void OnSelectionChanged()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null)
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    File.WriteAllText(FilePath, "{\n  \"selected\": false\n}", Encoding.UTF8);
                }
            }
            catch { }
            return;
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("{");
        sb.AppendLine("  \"selected\": true,");
        sb.AppendLine($"  \"name\": \"{Escape(selected.name)}\",");
        sb.AppendLine($"  \"tag\": \"{Escape(selected.tag)}\",");
        sb.AppendLine($"  \"layer\": \"{LayerMask.LayerToName(selected.layer)} ({selected.layer})\",");
        sb.AppendLine($"  \"path\": \"{Escape(GetHierarchyPath(selected.transform))}\",");
        sb.AppendLine($"  \"position\": \"{selected.transform.position}\",");
        sb.AppendLine($"  \"rotation\": \"{selected.transform.eulerAngles}\",");
        sb.AppendLine($"  \"forward\": \"{selected.transform.forward}\",");
        Renderer rend = selected.GetComponent<Renderer>();
        if (rend != null)
        {
            sb.AppendLine($"  \"renderer_bounds\": \"center={rend.bounds.center}, size={rend.bounds.size}, min={rend.bounds.min}, max={rend.bounds.max}\",");
        }
        GameObject trig = GameObject.Find("Trigger_Mulai_Simulasi");
        if (trig != null)
        {
            sb.AppendLine("  \"trigger_mulai\": {");
            sb.AppendLine($"    \"position\": \"{trig.transform.position}\",");
            sb.AppendLine($"    \"active\": {trig.activeInHierarchy.ToString().ToLower()}");
            sb.AppendLine("  },");
        }
        GameManager gm = Object.FindFirstObjectByType<GameManager>(FindObjectsInactive.Include);
        if (gm != null)
        {
            sb.AppendLine("  \"gameManager\": {");
            sb.AppendLine($"    \"waitForStartTrigger\": {gm.waitForStartTrigger.ToString().ToLower()},");
            sb.AppendLine($"    \"enableLevel1Quiz\": {gm.enableLevel1Quiz.ToString().ToLower()},");
            sb.AppendLine($"    \"currentLevel\": {gm.currentLevel},");
            sb.AppendLine($"    \"isGameActive\": {gm.isGameActive.ToString().ToLower()}");
            sb.AppendLine("  },");
        }
        sb.AppendLine($"  \"scale\": \"{selected.transform.lossyScale}\",");

        // Daftar Komponen
        Component[] components = selected.GetComponents<Component>();
        sb.Append("  \"components\": [");
        for (int i = 0; i < components.Length; i++)
        {
            if (components[i] == null) continue;
            if (i > 0) sb.Append(", ");
            sb.Append($"\"{components[i].GetType().Name}\"");
        }
        sb.AppendLine("],");

        // Detail Collider pada objek terpilih
        Collider col = selected.GetComponent<Collider>();
        if (col != null)
        {
            sb.AppendLine("  \"collider\": {");
            sb.AppendLine($"    \"type\": \"{col.GetType().Name}\",");
            sb.AppendLine($"    \"isTrigger\": {col.isTrigger.ToString().ToLower()},");
            sb.AppendLine($"    \"bounds\": \"center={col.bounds.center}, size={col.bounds.size}, min={col.bounds.min}, max={col.bounds.max}\",");
            if (col is BoxCollider bc)
            {
                sb.AppendLine($"    \"center\": \"{bc.center}\",");
                sb.AppendLine($"    \"size\": \"{bc.size}\"");
            }
            sb.AppendLine("  },");
        }

        // Info Meja_Belajar_2 spesifik jika ada di scene
        GameObject table = GameObject.Find("Meja_Belajar_2");
        if (table != null)
        {
            sb.AppendLine("  \"table_Meja_Belajar_2\": {");
            sb.AppendLine($"    \"position\": \"{table.transform.position}\",");
            sb.AppendLine($"    \"scale\": \"{table.transform.lossyScale}\",");
            Collider tCol = table.GetComponent<Collider>();
            if (tCol != null)
            {
                sb.AppendLine($"    \"hasCollider\": true,");
                sb.AppendLine($"    \"type\": \"{tCol.GetType().Name}\",");
                sb.AppendLine($"    \"isTrigger\": {tCol.isTrigger.ToString().ToLower()},");
                sb.AppendLine($"    \"bounds\": \"min={tCol.bounds.min}, max={tCol.bounds.max}\",");
                if (tCol is BoxCollider tbc)
                {
                    sb.AppendLine($"    \"center\": \"{tbc.center}\",");
                    sb.AppendLine($"    \"size\": \"{tbc.size}\"");
                }
            }
            else
            {
                sb.AppendLine($"    \"hasCollider\": false");
            }
            sb.AppendLine("  },");
        }

        // Info Karakter BoyController jika ada di scene
        BoyController boy = Object.FindFirstObjectByType<BoyController>();
        if (boy != null)
        {
            CharacterController cc = boy.GetComponent<CharacterController>();
            sb.AppendLine("  \"boy\": {");
            sb.AppendLine($"    \"position\": \"{boy.transform.position}\",");
            sb.AppendLine($"    \"isCrouching\": {boy.IsCrouching.ToString().ToLower()},");
            if (cc != null)
            {
                sb.AppendLine($"    \"ccHeight\": {cc.height},");
                sb.AppendLine($"    \"ccCenter\": \"{cc.center}\",");
                sb.AppendLine($"    \"ccRadius\": {cc.radius},");
                sb.AppendLine($"    \"stepOffset\": {cc.stepOffset},");
                sb.AppendLine($"    \"skinWidth\": {cc.skinWidth}");
            }
            sb.AppendLine("  }");
        }
        else
        {
            sb.AppendLine("  \"boy\": null");
        }

        sb.AppendLine("}");

        try
        {
            string dir = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            File.WriteAllText(FilePath, sb.ToString(), Encoding.UTF8);
        }
        catch { }
    }

    private static string GetHierarchyPath(Transform t)
    {
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }

    private static string Escape(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", " ").Replace("\r", "");
    }
}
#endif
