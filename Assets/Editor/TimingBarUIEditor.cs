using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TimingBarUI))]
public class TimingBarUIEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        TimingBarUI bar = (TimingBarUI)target;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Zone Preview", EditorStyles.boldLabel);

        // 미리보기 바 영역
        Rect barRect = GUILayoutUtility.GetRect(0, 28, GUILayout.ExpandWidth(true));
        barRect.x     += 4;
        barRect.width -= 8;

        // 직렬화된 프로퍼티에서 값 읽기
        float perfectMin = serializedObject.FindProperty("perfectMin").floatValue;
        float perfectMax = serializedObject.FindProperty("perfectMax").floatValue;
        float goodMin    = serializedObject.FindProperty("goodMin").floatValue;
        float goodMax    = serializedObject.FindProperty("goodMax").floatValue;

        // BAD 구간 (전체 배경 = 회색)
        EditorGUI.DrawRect(barRect, new Color(0.3f, 0.3f, 0.3f));

        // GOOD 구간 (파랑)
        DrawZone(barRect, goodMin, goodMax, new Color(0.2f, 0.4f, 0.9f));

        // PERFECT 구간 (노랑)
        DrawZone(barRect, perfectMin, perfectMax, new Color(1f, 0.8f, 0.1f));

        // 구간 경계선
        DrawLine(barRect, goodMin,    Color.white);
        DrawLine(barRect, goodMax,    Color.white);
        DrawLine(barRect, perfectMin, Color.yellow);
        DrawLine(barRect, perfectMax, Color.yellow);

        // 텍스트 레이블
        GUILayout.BeginHorizontal();
        GUILayout.Label("BAD",     GUILayout.Width(barRect.width * goodMin));
        GUILayout.Label("GOOD",    GUILayout.Width(barRect.width * (perfectMin - goodMin)));
        GUILayout.Label("PERFECT", GUILayout.Width(barRect.width * (perfectMax - perfectMin)));
        GUILayout.Label("GOOD",    GUILayout.Width(barRect.width * (goodMax - perfectMax)));
        GUILayout.Label("BAD",     GUILayout.ExpandWidth(true));
        GUILayout.EndHorizontal();
    }

    void DrawZone(Rect bar, float from, float to, Color color)
    {
        Rect zone = new Rect(
            bar.x + bar.width * from,
            bar.y,
            bar.width * (to - from),
            bar.height
        );
        EditorGUI.DrawRect(zone, color);
    }

    void DrawLine(Rect bar, float t, Color color)
    {
        Rect line = new Rect(
            bar.x + bar.width * t - 1,
            bar.y,
            2,
            bar.height
        );
        EditorGUI.DrawRect(line, color);
    }
}
