using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Generator3D))]
public class GeneratorDataEditor3D : Editor
{
    int TotalCells;
    int TotalCellsCollapsed;
    Generator3D generator;
    public override void OnInspectorGUI()
    {
        generator = (Generator3D)target;
        EditorGUILayout.LabelField("PATHS: " + generator.splines.Count().ToString(), EditorStyles.boldLabel);

        int TotalCells = generator.xDimension * generator.xDimension;
        int TotalCellsCollapsed = generator.cellGrid.Count(x => x.collapsed);

        if (TotalCells > 0)
            ProgressBar(TotalCellsCollapsed / TotalCells, "Collapsed Cells");

        base.OnInspectorGUI();

        if (generator.xDimension <= 0 || generator.zDimension <= 0)
        {
            EditorGUILayout.HelpBox("Dimensions must be greater than 0", MessageType.Error);
        }
        if (generator.minSplines == 0 || generator.maxSplines == 0)
        {
            EditorGUILayout.HelpBox("Min and Max Splines must be greater than 0", MessageType.Error);
        }
        if (generator.startTile == null || generator.endTile == null)
        {
            EditorGUILayout.HelpBox("Start and End Tiles must be set", MessageType.Error);
        }
        if (generator.endLocation.y < generator.startLocation.y || generator.endLocation == generator.startLocation)
        {
            EditorGUILayout.HelpBox("End Location must be greater than Start Location on the z-Axis", MessageType.Error);
        }
        if (generator.cellPrefab == null)
        {
            EditorGUILayout.HelpBox("Cell Prefab must be set", MessageType.Error);
        }
    }
    private void Update()
    {
        int TotalCells = generator.xDimension * generator.xDimension;
        int TotalCellsCollapsed = generator.cellGrid.Count(x => x.collapsed);
        ProgressBar(TotalCellsCollapsed / TotalCells, "Collapsed Cells");
    }
    void ProgressBar(float value, string label)
    {
        Rect rect = GUILayoutUtility.GetRect(18, 18, "TextField");
        EditorGUI.ProgressBar(rect, value, label);
        EditorGUILayout.Space();
    }
}
