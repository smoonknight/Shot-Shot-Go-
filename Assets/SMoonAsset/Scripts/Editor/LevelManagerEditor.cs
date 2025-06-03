using UnityEngine;
using UnityEditor;
using System;

[CustomEditor(typeof(LevelManager))]
public class LevelManagerEditor : Editor
{
    LevelManager levelManager;

    private void OnEnable()
    {
        levelManager = target as LevelManager;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        levelManager.magicSwordPointDataProperties.Clear();

        Transform[] childrenTransforms = levelManager.transform.GetComponentsInChildren<Transform>(true);
        foreach (var childrenTransform in childrenTransforms)
        {
            if (Enum.TryParse(childrenTransform.name, true, out MagicSwordItemType type))
            {
                PointDataProperty<MagicSwordItemType> pointDataProperty = new();
                pointDataProperty.type = type;
                pointDataProperty.pointData.position = childrenTransform.position;
                pointDataProperty.pointData.rotation = childrenTransform.rotation;
                levelManager.magicSwordPointDataProperties.Add(pointDataProperty);
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

}