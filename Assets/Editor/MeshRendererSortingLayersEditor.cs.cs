using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
using System.Reflection;
using System;
#endif

[CustomEditor(typeof(MeshRenderer))]
[CanEditMultipleObjects]
public class MeshRendererSortingLayersEditor : Editor
{
    public override void OnInspectorGUI()
    {
 
        base.OnInspectorGUI();
 
        serializedObject.Update();
 
        SerializedProperty sortingLayerID = serializedObject.FindProperty("m_SortingLayerID");
        SerializedProperty sortingOrder = serializedObject.FindProperty("m_SortingOrder");
 
        MeshRenderer renderer = target as MeshRenderer;
 
        Rect firstHoriz = EditorGUILayout.BeginHorizontal();
 
        EditorGUI.BeginChangeCheck();
 
        EditorGUI.BeginProperty(firstHoriz,GUIContent.none,sortingLayerID);
 
        string[] layerNames = GetSortingLayerNames();
        int[] layerID = GetSortingLayerUniqueIDs();
 
        int selected = -1;
        //What is selected?
        int sID = sortingLayerID.intValue;
        for(int i = 0 ; i < layerID.Length ; i++)
        {
            //Debug.Log(sID + " " + layerID[i]);
            if(sID == layerID[i])
            {
                selected = i;
            }
        }
 
        if(selected == -1)
        {
            //Select Default.
            for(int i = 0 ; i < layerID.Length ; i++)
            {
                if(layerID[i] == 0)
                {
                    selected = i;
                }
            }
        }
 
        selected = EditorGUILayout.Popup("Sorting Layer" ,selected,layerNames);
 
        //Translate to ID
        sortingLayerID.intValue = layerID[selected];
 
 
        EditorGUI.EndProperty();
 
        EditorGUILayout.EndHorizontal();
 
        EditorGUILayout.BeginHorizontal();
        EditorGUI.BeginChangeCheck();
 
        EditorGUILayout.PropertyField(sortingOrder,new GUIContent("Order in Layer"));
 
 
        EditorGUILayout.EndHorizontal();
        serializedObject.ApplyModifiedProperties();
    }
 
    public string[] GetSortingLayerNames() {
        Type internalEditorUtilityType = typeof(InternalEditorUtility);
        PropertyInfo sortingLayersProperty = internalEditorUtilityType.GetProperty("sortingLayerNames", BindingFlags.Static | BindingFlags.NonPublic);
        return (string[])sortingLayersProperty.GetValue(null, new object[0]);
    }
 
    public int[] GetSortingLayerUniqueIDs() {
        Type internalEditorUtilityType = typeof(InternalEditorUtility);
        PropertyInfo sortingLayerUniqueIDsProperty = internalEditorUtilityType.GetProperty("sortingLayerUniqueIDs", BindingFlags.Static | BindingFlags.NonPublic);
        return (int[])sortingLayerUniqueIDsProperty.GetValue(null, new object[0]);
    }
}