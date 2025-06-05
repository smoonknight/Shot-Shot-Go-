using UnityEngine;

public interface IMagneticable
{
    public void Attraction(Vector2 magnetPosition);
    public Transform MagneticSource();
    public void OnMagneticClose(PlayerController playerController);
}