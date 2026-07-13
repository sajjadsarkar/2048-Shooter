using UnityEngine;
using UnityEngine.EventSystems;

public class TileSwapHandler : MonoBehaviour, IPointerClickHandler
{
    private GridManager2048 gridManager;
    private Tile2048 tile;
    private Vector2Int position;

    private void Start()
    {
        // Find the grid manager
        gridManager = FindObjectOfType<GridManager2048>();

        // Get the tile component
        tile = GetComponent<Tile2048>();

        if (gridManager == null)
        {
            Debug.LogError("GridManager2048 not found in the scene!");
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Check if swap mode is active
        if (PowerUpsManager2048.Instance != null && PowerUpsManager2048.Instance.IsSwapModeActive())
        {
            // Notify PowerUpsManager that this tile was selected
            PowerUpsManager2048.Instance.SetSelectedTile(gameObject);
        }
    }

    public void TrySwapWith(GameObject otherTileObject)
    {
        // Find positions of both tiles
        Vector2Int thisPosition = FindTilePosition();

        TileSwapHandler otherHandler = otherTileObject.GetComponent<TileSwapHandler>();
        if (otherHandler == null)
        {
            Debug.LogError("Other object does not have a TileSwapHandler component");
            return;
        }

        Vector2Int otherPosition = otherHandler.FindTilePosition();

        // Check if both positions are valid
        if (thisPosition.x == -1 || otherPosition.x == -1)
        {
            Debug.LogError("Could not find valid positions for tiles");
            return;
        }

        // Check if the tiles are adjacent
        if (ArePositionsAdjacent(thisPosition, otherPosition))
        {
            Debug.Log($"Swapping tiles at {thisPosition} and {otherPosition}");
            SwapTiles(thisPosition, otherPosition);

            // Notify that tiles were swapped
            PowerUpsManager2048.Instance.TilesSwapped();
        }
        else
        {
            Debug.Log("Tiles are not adjacent, cannot swap");
        }
    }

    public Vector2Int FindTilePosition()
    {
        for (int x = 0; x < gridManager.Width; x++)
        {
            for (int y = 0; y < gridManager.Height; y++)
            {
                if (gridManager.GetTileAt(x, y) == tile)
                {
                    return new Vector2Int(x, y);
                }
            }
        }

        return new Vector2Int(-1, -1); // Not found
    }

    private bool ArePositionsAdjacent(Vector2Int pos1, Vector2Int pos2)
    {
        // Check if the positions are horizontally or vertically adjacent
        int xDiff = Mathf.Abs(pos1.x - pos2.x);
        int yDiff = Mathf.Abs(pos1.y - pos2.y);

        // Adjacent means one coordinate differs by 1 and the other is the same
        return (xDiff == 1 && yDiff == 0) || (xDiff == 0 && yDiff == 1);
    }

    private void SwapTiles(Vector2Int pos1, Vector2Int pos2)
    {
        // Get the tiles at both positions
        Tile2048 tile1 = gridManager.GetTileAt(pos1.x, pos1.y);
        Tile2048 tile2 = gridManager.GetTileAt(pos2.x, pos2.y);

        if (tile1 == null || tile2 == null)
        {
            Debug.LogError("One of the tiles is null, cannot swap");
            return;
        }

        // Store the values
        int value1 = tile1.Value;
        int value2 = tile2.Value;

        // Swap the values
        tile1.Value = value2;
        tile2.Value = value1;

        // Check for merges after swapping
        CheckForMergesAfterSwap(pos1, pos2);
    }

    private void CheckForMergesAfterSwap(Vector2Int pos1, Vector2Int pos2)
    {
        // Start a coroutine in gridManager to check for matches
        // We're calling a method that doesn't exist yet, but we'll add it below
        StartCoroutine(gridManager.CheckForMergesAfterSwap(pos1, pos2));
    }
}
