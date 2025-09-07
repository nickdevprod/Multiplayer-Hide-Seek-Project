#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class HierarchyHeaderEditor
{
    static HierarchyHeaderEditor()
    {
        EditorApplication.hierarchyWindowItemOnGUI += HandleHierarchyWindowItemOnGUI;
    }

    private static void HandleHierarchyWindowItemOnGUI(int instanceID, Rect selectionRect)
    {
        GameObject obj = EditorUtility.InstanceIDToObject(instanceID) as GameObject;

        if (obj == null) return;
        
        if (obj.GetComponent<HierarchyHeader>())
        {
            EditorGUI.DrawRect(selectionRect, new Color(0.15f, 0.15f, 0.15f));
            
            GUIStyle style = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = new GUIStyleState() { textColor = Color.white }
            };

            EditorGUI.LabelField(selectionRect, obj.name, style);
        }
    }
}
#endif