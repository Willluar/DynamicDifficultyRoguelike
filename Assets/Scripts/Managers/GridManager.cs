using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance;

    [Header("Grid Settings")]
    public int width = 10;
    public int height = 10;
    public float cellSize = 1f;

    [Header("Walls")]
    public LayerMask wallLayer;

    private Dictionary<Vector2Int, GameObject> occupiedTiles =
        new Dictionary<Vector2Int, GameObject>();

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        BakeWallsIntoGrid();
    }

    /* ---------------- Grid Utilities ---------------- */

    public Vector2Int WorldToGrid(Vector3 worldPos)
    {
        return new Vector2Int(
            Mathf.RoundToInt(worldPos.x / cellSize),
            Mathf.RoundToInt(worldPos.y / cellSize)
        );
    }

    public Vector3 GridToWorld(Vector2Int gridPos)
    {
        return new Vector3(
            gridPos.x * cellSize,
            gridPos.y * cellSize,
            0f
        );
    }

    public bool IsInsideGrid(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < width &&
               pos.y >= 0 && pos.y < height;
    }

    /* ---------------- Occupancy Control ---------------- */

    public void ClearOccupancy()
    {
        occupiedTiles.Clear();
        BakeWallsIntoGrid();
    }

    public bool IsTileOccupied(Vector2Int gridPos)
    {
        return occupiedTiles.ContainsKey(gridPos);
    }

    public GameObject GetOccupant(Vector2Int gridPos)
    {
        return occupiedTiles.TryGetValue(gridPos, out var obj)
            ? obj
            : null;
    }

    public void Register(GameObject obj)
    {
        Vector2Int pos = WorldToGrid(obj.transform.position);
        occupiedTiles[pos] = obj; // overwrite-safe
    }

    public void Unregister(GameObject obj)
    {
        Vector2Int pos = WorldToGrid(obj.transform.position);

        if (occupiedTiles.ContainsKey(pos) && occupiedTiles[pos] == obj)
            occupiedTiles.Remove(pos);
    }

    public void Move(GameObject obj, Vector2Int newGridPos)
    {
        Unregister(obj);

        obj.transform.position = GridToWorld(newGridPos);

        Register(obj);
    }

    /* ---------------- Wall Baking ---------------- */

    public void BakeWallsIntoGrid()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2Int gridPos = new Vector2Int(x, y);
                Vector3 worldPos = GridToWorld(gridPos);

                Collider2D hit = Physics2D.OverlapBox(
                    worldPos,
                    Vector2.one * 0.9f,
                    0,
                    wallLayer
                );

                if (hit != null)
                {
                    occupiedTiles[gridPos] = hit.gameObject;
                }
            }
        }
    }
}