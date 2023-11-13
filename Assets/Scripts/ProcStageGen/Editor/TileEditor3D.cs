using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Tile3D))]
public class TileEditor3D : Editor
{
    public override void OnInspectorGUI()
    {
        Tile3D tile = (Tile3D)target;

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Generate Rotated Tiles"))
        {
            tile.GenerateTiles();
        }
        EditorGUILayout.EndHorizontal();

        // EditorGUILayout.LabelField("Forward: " + tile.GetSideValue(Cell3D.Direction.Forward).ToString());
        // EditorGUILayout.LabelField("Right: " + tile.GetSideValue(Cell3D.Direction.Right).ToString());
        // EditorGUILayout.LabelField("Back: " + tile.GetSideValue(Cell3D.Direction.Back).ToString());
        // EditorGUILayout.LabelField("Left: " + tile.GetSideValue(Cell3D.Direction.Left).ToString());
        // EditorGUILayout.LabelField("Up: " + tile.GetSideValue(Cell3D.Direction.Up).ToString());
        // EditorGUILayout.LabelField("Down: " + tile.GetSideValue(Cell3D.Direction.Down).ToString());

        base.OnInspectorGUI();
    }

}
