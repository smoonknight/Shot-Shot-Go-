using UnityEngine;

public class LayerMaskManager : SingletonWithDontDestroyOnLoad<LayerMaskManager>
{
    public LayerMask groundableLayer;
    public LayerMask wallLayer;
    public LayerMask playerMask;
    public LayerMask enemyMask;
    public LayerMask collectableMask;
}