using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Tilemap))]
[RequireComponent(typeof(TilemapRenderer))]
[RequireComponent(typeof(TilemapCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CompositeCollider2D))]
public class TilemapController : MonoBehaviour
{
    public List<BoxCollider2D> spawnAreas;
    public Tilemap tilemap;
    public TilemapCollider2D tilemapCollider2D;

    public bool IsSpawnAreasExist => spawnAreas.Count != 0;

    public Vector2 GetRandomPointInSpawnAreas()
    {
        BoxCollider2D spawnArea = spawnAreas.GetRandom();
        Bounds bounds = spawnArea.bounds;
        float x = Random.Range(bounds.min.x, bounds.max.x);
        float y = Random.Range(bounds.min.y, bounds.max.y);
        return new Vector2(x, y);
    }

    public void Reset()
    {
        tilemap.color = Color.white;
        tilemapCollider2D.enabled = true;
    }
}