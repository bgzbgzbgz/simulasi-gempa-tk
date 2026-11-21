#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(EvacuationGuide))]
public class EvacuationGuideEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Gambar default inspector
        DrawDefaultInspector();

        EvacuationGuide guide = (EvacuationGuide)target;

        EditorGUILayout.Space(15);
        EditorGUILayout.LabelField("Peralatan Rute Manual (Waypoints)", EditorStyles.boldLabel);

        EditorGUILayout.HelpBox(
            "Sistem Rute Manual (Waypoints) menjamin garis evakuasi 100% tidak akan menembus tembok.\n\n" +
            "Setiap titik belokan (misal di lorong meja, pintu kelas, dan selasar luar) dapat digeser langsung menggunakan Move Tool (W) di jendela Scene View Unity.",
            MessageType.Info
        );

        if (GUILayout.Button("📍 Buat / Reset Waypoints Bawaan di Scene", GUILayout.Height(35)))
        {
            Undo.RecordObject(guide.gameObject, "Generate Waypoints");
            guide.AutoPopulateWaypointsIfEmpty();
            EditorUtility.SetDirty(guide);
        }

        if (guide.transform.childCount > 0)
        {
            if (GUILayout.Button("🔍 Pilih Objek Waypoint Pertama di Scene", GUILayout.Height(28)))
            {
                Selection.activeGameObject = guide.transform.GetChild(0).gameObject;
                SceneView.FrameLastActiveSceneView();
            }
        }
    }
}
#endif
