using UnityEngine;

public class LayerMaskManager : SingletonWithDontDestroyOnLoad<LayerMaskManager>
{
    public LayerMask groundLayer;
    public LayerMask wallLayer;
}