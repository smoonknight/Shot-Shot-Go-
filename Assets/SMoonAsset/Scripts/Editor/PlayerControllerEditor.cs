
using UnityEngine;
using UnityEditor;
using System;

[CustomEditor(typeof(PlayerController))]
public class PlayerControllerEditor : Editor
{
    PlayerController playerController;
    int expValue;
    PlayerStateType playerStateType;

    private void OnEnable()
    {
        playerController = (PlayerController)target;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (!Application.isPlaying)
        {
            return;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Debug Experience", EditorStyles.boldLabel);

        // Pilihan exp yang bisa ditambahkan
        string[] options = new string[] { "10", "50", "100", "200" };
        int[] values = new int[] { 10, 50, 100, 200 };

        expValue = EditorGUILayout.IntPopup("Experience to Add", expValue, options, values);

        if (GUILayout.Button("Add Experience"))
        {
            playerController.AddExperience(expValue);
        }

        playerStateType = (PlayerStateType)EditorGUILayout.EnumPopup("Player State Type", playerStateType, EditorStyles.boldLabel);

        if (GUILayout.Button("Change State"))
        {
            playerController.playerStateMachine.SetState(playerStateType);
        }
    }
}
