using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Cartesian))]
public class CartesianEditor : Editor
{
    private Cartesian Instance;
    private void Awake()
    {
        Instance = target as Cartesian;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        if (GUILayout.Button("Prepare"))
        {
            Instance.Init();
        }
    }
}
