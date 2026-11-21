using UnityEngine;
using UnityEditor;

public class InspectWaypoints
{
    public static void Run()
    {
        var guide = Object.FindFirstObjectByType<EvacuationGuide>();
        if (guide == null)
        {
            Debug.LogError("No EvacuationGuide found!");
            return;
        }
        var so = new SerializedObject(guide);
        var wps = so.FindProperty("manualWaypoints");
        Debug.Log("Waypoints count: " + wps.arraySize);
        for (int i = 0; i < wps.arraySize; i++)
        {
            var elem = wps.GetArrayElementAtIndex(i).objectReferenceValue as Transform;
            if (elem != null)
                Debug.Log("WP[" + i + "]: " + elem.name + " at " + elem.position);
            else
                Debug.Log("WP[" + i + "]: null");
        }
        var asm = so.FindProperty("assemblyPoint").objectReferenceValue as Transform;
        Debug.Log("Assembly Point: " + (asm != null ? asm.name + " at " + asm.position.ToString() : "null"));
        var ply = so.FindProperty("player").objectReferenceValue as Transform;
        Debug.Log("Player: " + (ply != null ? ply.name + " at " + ply.position.ToString() : "null"));
    }
}
