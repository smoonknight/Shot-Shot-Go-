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
    [ReadOnly]
    public Tilemap tilemap;
    [ReadOnly]
    public TilemapRenderer tilemapRenderer;
    [ReadOnly]
    public TilemapCollider2D tilemapCollider2D;

    private void Awake()
    {
        tilemap = GetComponent<Tilemap>();
        tilemapRenderer = GetComponent<TilemapRenderer>();
        tilemapCollider2D = GetComponent<TilemapCollider2D>();
    }

    public bool IsSpawnAreasExist => spawnAreas.Count != 0;

    public Vector2 GetRandomPointInSpawnAreas()
    {
        BoxCollider2D spawnArea = spawnAreas.GetRandom();
        Bounds bounds = spawnArea.bounds;
        float x = Random.Range(bounds.min.x, bounds.max.x);
        float y = Random.Range(bounds.min.y, bounds.max.y);
        return new Vector2(x, y);
    }
}