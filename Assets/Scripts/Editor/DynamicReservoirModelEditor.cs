using System;
using UnityEditor;
using UnityEngine;


[CustomEditor(typeof(DynamicReservoirModel))]
public class DynamicReservoirModelEditor : Editor
{
    private DynamicReservoirModel Instance;

    private void Awake()
    {
        Instance = target as DynamicReservoirModel;
    }


    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        if (GUILayout.Button("Prepare"))
        {
            var time = DateTime.Now;

            Instance.Prepare();
            Debug.Log((DateTime.Now - time).TotalSeconds);
        }
    }
}