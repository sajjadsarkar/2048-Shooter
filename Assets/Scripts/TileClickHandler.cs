using UnityEngine;
using UnityEngine.EventSystems;

public class TileClickHandler : MonoBehaviour, IPointerClickHandler
{
    private GridManager2048 gridManager;
    private Tile2048 tile;
    private TileSwapHandler swapHandler;
    private ShootingManager2048 shootingManager; // Add reference to shooting manager

    private void Start()
    {
        // Find the grid manager
        gridManager = FindObjectOfType<GridManager2048>();

        // Get the tile component
        tile = GetComponent<Tile2048>();

        // Get or add the swap handler component
        swapHandler = GetComponent<TileSwapHandler>();
        if (swapHandler == null)
        {
            swapHandler = gameObject.AddComponent<TileSwapHandler>();
        }

        // Find the shooting manager
        shootingManager = FindObjectOfType<ShootingManager2048>();

        if (gridManager == null)
        {
            Debug.LogError("GridManager2048 not found in the scene!");
        }

        if (shootingManager == null)
        {
            Debug.LogWarning("ShootingManager2048 not found in the scene!");
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        bool powerUpActive = false;

        // Check if PowerUpsManager exists and if any power-up mode is active
        if (PowerUpsManager2048.Instance != null)
        {
            // Check destroy mode
            if (PowerUpsManager2048.Instance.IsDestroyModeActive())
            {
                powerUpActive = true;
                // Find the tile's position in the grid
                Vector2Int tilePosition = FindTilePosition();

                if (tilePosition.x != -1 && tilePosition.y != -1)
                {
                    Debug.Log($"Destroying tile at position: {tilePosition.x}, {tilePosition.y}");

                    // Remove tile from grid
                    DestroyTile(tilePosition.x, tilePosition.y);

                    // Notify the destruction manager that a tile has been destroyed
                    PowerUpsManager2048.Instance.TileDestroyed();
                }
            }
            // Check swap mode
            else if (PowerUpsManager2048.Instance.IsSwapModeActive())
            {
                powerUpActive = true;
                // The swap mode is handled by TileSwapHandler
            }
        }

        // If no power-up is active, forward to shooting manager to shoot at this column
        if (!powerUpActive && shootingManager != null)
        {
            Vector2Int tilePosition = FindTilePosition();
            if (tilePosition.x != -1)
            {
                shootingManager.ShootAtColumn(tilePosition.x);
            }
        }
    }

    private Vector2Int FindTilePosition()
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

    private void DestroyTile(int column, int row)
    {
        // Set the grid cell to null
        gridManager.ClearTileAt(column, row);

        // Apply gravity to the column
        gridManager.ApplyGravity(column);

        // Also check adjacent columns for potential cascade effects
        if (column > 0) gridManager.ApplyGravity(column - 1);
        if (column < gridManager.Width - 1) gridManager.ApplyGravity(column + 1);

        // Destroy the game object
        Destroy(gameObject);
    }
}
