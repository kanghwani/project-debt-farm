#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DualGridEditorTilemap))]
public class DualGridEditorTilemapInspector : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        DualGridEditorTilemap builder = (DualGridEditorTilemap)target;

        GUILayout.Space(10f);
        if (GUILayout.Button("Rebuild Display Tilemap"))
            builder.RebuildAll();
    }
}
#endif
