using UnityEngine;

public class LayerMaskManager : SingletonWithDontDestroyOnLoad<LayerMaskManager>
{
    public LayerMask groundLayer;
    public LayerMask wallLayer;
    public LayerMask playerMask;
    public LayerMask enemyMask;
}