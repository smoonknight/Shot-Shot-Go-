using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public static class TransformHelper
{
    public static void DestroyChilds(this Transform transform)
    {
        if (transform == null) return;
        foreach (Transform child in transform)
        {
            Object.Destroy(child.gameObject);
        }
    }

    public static void DestroyChildsWithExceptTag(this Transform transform, string exceptTag)
    {
        if (transform == null) return;
        foreach (Transform child in transform)
        {
            if (child.CompareTag(exceptTag)) continue;
            Object.Destroy(child.gameObject);
        }
    }

    public static async UniTask UpdateLayout(RectTransform rectTransform)
    {
        int updateCount = 0;

        while (updateCount < 5)
        {
            await UniTask.NextFrame();
            rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x + 0.1f, rectTransform.sizeDelta.y);
            await UniTask.NextFrame();
            rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x - 0.1f, rectTransform.sizeDelta.y);
            rectTransform.ForceUpdateRectTransforms();
            updateCount += 1;
        }
    }

    public static List<T> GetComponentsRecursively<T>(List<Collider> colliders) where T : Component
    {
        List<T> componentsList = new List<T>();

        foreach (var collider in colliders)
        {
            if (collider.TryGetComponent<T>(out var currentComponent))
            {
                componentsList.Add(currentComponent);
            }
        }

        return componentsList;
    }

    public static List<T> GetComponentsRecursively<T>(Transform currentTransform) where T : Component
    {
        List<T> componentsList = new();

        void GetComponents(Transform transform)
        {
            if (transform.TryGetComponent<T>(out var currentComponent))
            {
                componentsList.Add(currentComponent);
            }

            foreach (Transform child in transform)
            {
                GetComponents(child);
            }
        }

        GetComponents(currentTransform);

        return componentsList;
    }

    public static bool CompareWithLayerIndex(this LayerMask layerMask, int index) => layerMask == (layerMask | (1 << index));
    public static bool CompareLayermaskWithLayerIndex(LayerMask layerMask, int index) => layerMask == (layerMask | (1 << index));
}