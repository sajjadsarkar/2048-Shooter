using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening; // Add this for List<>

public class GridManager2048 : MonoBehaviour
{
    [SerializeField] private int width = 4;
    [SerializeField] private int height = 4;
    [SerializeField] private GameObject tilePrefab;
    [SerializeField] private Transform gridParent;
    public float tileSizeX = 100f;
    public float tileSizeY = 100f;
    [SerializeField] private float spacing = 10f;
    [SerializeField] private ScoreManager2048 scoreManager; // Add reference to score manager
    [SerializeField] private GameObject floatingTextPrefab; // Add this for floating text
    [SerializeField] private GameObject comboTextPrefab; // Add this for combo text
    [SerializeField] private float comboDelay = 0.3f; // Delay before showing combo text
    [SerializeField] private float comboResetTime = 1.5f; // Time before combo resets

    private int currentCombo = 0;
    private float lastMergeTime;
    private Coroutine comboResetCoroutine;
    private bool comboTextShown = false; // Add this flag to track if combo text is shown

    // Modified to store tile references instead of just GameObjects
    private Tile2048[,] gridCells;

    // Track if gravity operations are in progress
    private bool isApplyingGravity = false;
    public bool IsApplyingGravity => isApplyingGravity;

    public int Width => width;
    public int Height => height;
    public float TileSizeX => tileSizeX;
    public float TileSizeY => tileSizeY;
    public float Spacing => spacing;

    private Vector2Int lastActiveTilePosition = new Vector2Int(-1, -1);

    private void Start()
    {
        // Find ScoreManager if not assigned
        if (scoreManager == null)
        {
            scoreManager = FindObjectOfType<ScoreManager2048>();
        }

        GenerateGrid();
        InitializeGridCells();
    }

    private void GenerateGrid()
    {
        // First create the actual visible grid cells (keep this part the same)
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                GameObject tile = Instantiate(tilePrefab, gridParent);
                RectTransform rectTransform = tile.GetComponent<RectTransform>();
                rectTransform.sizeDelta = new Vector2(tileSizeX, tileSizeY);
                rectTransform.anchoredPosition = GetCellPosition(x, y);
                tile.name = $"Tile_{x}_{y}";
            }
        }

        // Create invisible column touch areas that extend beyond the visible grid
        CreateColumnTouchAreas();
    }

    // New method to create larger touch areas for columns
    private void CreateColumnTouchAreas()
    {
        // Calculate the grid dimensions
        float gridWidth = width * (tileSizeX + spacing) - spacing;
        float gridHeight = height * (tileSizeY + spacing) - spacing;

        // Create a transparent touch area for each column
        for (int x = 0; x < width; x++)
        {
            GameObject touchArea = new GameObject($"Column_TouchArea_{x}", typeof(RectTransform), typeof(Image));
            touchArea.transform.SetParent(gridParent, false);

            // Get the RectTransform component
            RectTransform rectTransform = touchArea.GetComponent<RectTransform>();

            // Make the touch area slightly narrower than the cell
            float touchWidth = tileSizeX * 1f; // Changed from 1.5f to 0.8f to make it narrower

            // Make the touch area extend the full height of the grid plus extra padding
            float touchHeight = gridHeight + tileSizeY * 2;

            // Set the size of the touch area
            rectTransform.sizeDelta = new Vector2(touchWidth, touchHeight);

            // Position the touch area at the column's X position
            float posX = -gridWidth / 2 + x * (tileSizeX + spacing) + tileSizeX / 2;
            rectTransform.anchoredPosition = new Vector2(posX, 0);

            // Get the Image component
            Image image = touchArea.GetComponent<Image>();

            // Make the image fully transparent but keep it as a raycast target
            Color transparentColor = new Color(0, 0, 0, 0);
            image.color = transparentColor;

            // Make sure it can receive clicks
            image.raycastTarget = true;

            // Add a TileClickHandler component that knows its column
            ColumnTouchHandler handler = touchArea.AddComponent<ColumnTouchHandler>();
            handler.Initialize(x);
        }
    }

    private void InitializeGridCells()
    {
        gridCells = new Tile2048[width, height];
    }

    // Method to place a projectile in the grid
    public bool PlaceProjectileInCell(int column, int row, GameObject projectile)
    {
        if (column < 0 || column >= width || row < 0 || row >= height)
        {
            Debug.LogError($"Invalid cell coordinates: {column}, {row}");
            return false;
        }

        if (gridCells[column, row] != null)
        {
            return false;
        }

        Tile2048 tile = projectile.GetComponent<Tile2048>();
        if (tile == null)
        {
            Debug.LogError("Projectile does not have a Tile2048 component!");
            return false;
        }

        // Add the tile to the grid
        gridCells[column, row] = tile;
        Debug.Log($"Placed new tile at ({column}, {row}) with value {tile.Value}");

        // IMPORTANT: Set the last active tile position as soon as we place the tile
        // This ensures this position is used as the merge center for all subsequent merges
        lastActiveTilePosition = new Vector2Int(column, row);
        Debug.Log($"Setting last active position to newly placed tile: {lastActiveTilePosition}");

        // Process any immediate merges with adjacent tiles
        int originalValue = tile.Value;
        List<Vector2Int> tilesToMerge = GetAdjacentTilesWithValue(column, row, originalValue);

        if (tilesToMerge.Count > 0)
        {
            // We have adjacent tiles to merge
            Debug.Log($"Found {tilesToMerge.Count} adjacent tiles to merge into newly placed tile");

            // Process the merge immediately
            ProcessMergeIntoTile(column, row, tilesToMerge);
        }
        else
        {
            // No immediate adjacent merges, check for connected groups later
            StartCoroutine(CheckForMatchesWithDelay(0.05f));
        }

        return true;
    }

    // New method to find adjacent tiles with the same value
    private List<Vector2Int> GetAdjacentTilesWithValue(int column, int row, int targetValue)
    {
        List<Vector2Int> matchingTiles = new List<Vector2Int>();

        // Define the four adjacent directions
        Vector2Int[] directions = new Vector2Int[]
        {
            new Vector2Int(0, -1),  // down
            new Vector2Int(0, 1),   // up
            new Vector2Int(-1, 0),  // left
            new Vector2Int(1, 0)    // right
        };

        foreach (Vector2Int dir in directions)
        {
            Vector2Int adjPos = new Vector2Int(column, row) + dir;

            // Check if in bounds
            if (adjPos.x >= 0 && adjPos.x < width && adjPos.y >= 0 && adjPos.y < height)
            {
                Tile2048 adjTile = gridCells[adjPos.x, adjPos.y];
                if (adjTile != null && adjTile.Value == targetValue)
                {
                    matchingTiles.Add(adjPos);
                    Debug.Log($"Found matching tile at ({adjPos.x}, {adjPos.y})");
                }
            }
        }

        return matchingTiles;
    }

    // Process a merge directly into a specific tile
    private void ProcessMergeIntoTile(int centerCol, int centerRow, List<Vector2Int> tilesToMerge)
    {
        Tile2048 centerTile = gridCells[centerCol, centerRow];
        if (centerTile == null) return;

        Debug.Log($"Processing merge into tile at ({centerCol}, {centerRow})");

        int totalValue = centerTile.Value;
        List<int> affectedColumns = new List<int>();

        // Add animation to the center tile
        RectTransform centerRect = centerTile.GetComponent<RectTransform>();
        if (centerRect != null)
        {
            DOTween.Kill(centerRect);
            centerRect.localScale = Vector3.one;

            Sequence seq = DOTween.Sequence();
            seq.Append(centerRect.DOScale(1.3f, 0.1f).SetEase(Ease.OutQuad));
            seq.Append(centerRect.DOScale(1.0f, 0.1f).SetEase(Ease.InQuad));
        }

        // Process each tile to merge
        foreach (Vector2Int pos in tilesToMerge)
        {
            Tile2048 mergeTile = gridCells[pos.x, pos.y];
            if (mergeTile == null) continue;

            Debug.Log($"Merging tile at ({pos.x}, {pos.y}) with value {mergeTile.Value}");

            // Add this tile's value to the total
            totalValue += mergeTile.Value;

            // Track affected columns
            if (!affectedColumns.Contains(pos.x))
                affectedColumns.Add(pos.x);

            // Remove the merged tile from the grid and destroy it
            gridCells[pos.x, pos.y] = null;
            Destroy(mergeTile.gameObject);
        }

        // Update the center tile with the new value
        centerTile.Value = totalValue;

        // Play sound effect
        if (AudioManager2048.Instance != null)
        {
            AudioManager2048.Instance.PlayMergeSound(totalValue, currentCombo);
        }

        // Update score
        if (scoreManager != null)
        {
            scoreManager.AddScore(totalValue);
            ShowFloatingText(centerCol, centerRow, totalValue, centerTile.GetComponent<Image>().color);

            if (CoinManager2048.Instance != null)
            {
                CoinManager2048.Instance.AddCoinsFromMerge(totalValue, currentCombo);
            }

            TrackCombo(centerCol, centerRow);
        }

        // CRITICAL: Update last active position to be the merged tile's position
        // This ensures subsequent merges prioritize this location
        lastActiveTilePosition = new Vector2Int(centerCol, centerRow);
        Debug.Log($"Updated last active position to merged tile: {lastActiveTilePosition}");

        // Apply gravity to affected columns
        foreach (int col in affectedColumns)
        {
            ApplyGravity(col);
        }
        if (!affectedColumns.Contains(centerCol))
        {
            ApplyGravity(centerCol);
        }

        // Check for additional matches after a delay
        StartCoroutine(CheckForMatchesWithDelay(0.05f));
    }

    // Check all cells for matches after a delay
    private IEnumerator CheckForMatchesWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // Create a list to store all potential matches
        List<Vector2Int> matchPositions = new List<Vector2Int>();

        // First scan the entire grid to find all potential matches
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (gridCells[x, y] != null)
                {
                    // Get connected tiles without processing them yet
                    List<Vector2Int> connectedTiles = GetAllConnectedTiles(x, y, gridCells[x, y].Value);
                    if (connectedTiles.Count > 1)
                    {
                        matchPositions.Add(new Vector2Int(x, y));
                    }
                }
            }
        }

        // Now process each match one by one with delays
        foreach (Vector2Int pos in matchPositions)
        {
            // Skip positions where the tile might have been removed in previous merges
            if (gridCells[pos.x, pos.y] == null) continue;

            // Process this match
            bool merged = ProcessConnectedTilesAt(pos.x, pos.y);

            if (merged)
            {
                // Wait for animation to complete before processing the next merge
                yield return new WaitForSeconds(0.3f);

                // Apply gravity to affected columns after each merge
                for (int col = 0; col < width; col++)
                {
                    ApplyGravity(col);
                }

                // Wait for gravity animations
                yield return new WaitForSeconds(0.2f);
            }
        }

        // Apply final gravity to all columns
        for (int col = 0; col < width; col++)
        {
            ApplyGravity(col);
        }
    }

    // Process any connected tiles at the given position
    private bool ProcessConnectedTilesAt(int column, int row)
    {
        Tile2048 tile = gridCells[column, row];
        if (tile == null) return false;

        int tileValue = tile.Value;

        // Get all connected tiles of the same value
        List<Vector2Int> connectedTiles = GetAllConnectedTiles(column, row, tileValue);

        // Only process if we have 2 or more tiles (including this one)
        if (connectedTiles.Count <= 1) return false;

        Debug.Log($"Found connected group of {connectedTiles.Count} tiles at ({column}, {row})");

        // Find the newest tile in this group to be the merge center
        Vector2Int mergeCenter = FindNewestTileInGroup(connectedTiles);

        Debug.Log($"Selected merge center: {mergeCenter}. Last active position: {lastActiveTilePosition}");

        // Remove the merge center from the list of tiles to merge
        connectedTiles.Remove(mergeCenter);

        // Process the merge
        ProcessMergeIntoTile(mergeCenter.x, mergeCenter.y, connectedTiles);
        return true;
    }

    // Get all connected tiles with the same value
    private List<Vector2Int> GetAllConnectedTiles(int startCol, int startRow, int targetValue)
    {
        List<Vector2Int> result = new List<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        Queue<Vector2Int> toCheck = new Queue<Vector2Int>();

        // Add starting position
        Vector2Int start = new Vector2Int(startCol, startRow);
        toCheck.Enqueue(start);
        visited.Add(start);

        // Directions to check
        Vector2Int[] directions = new Vector2Int[]
        {
            new Vector2Int(0, -1),  // down
            new Vector2Int(0, 1),   // up
            new Vector2Int(-1, 0),  // left
            new Vector2Int(1, 0)    // right
        };

        // BFS to find all connected tiles
        while (toCheck.Count > 0)
        {
            Vector2Int current = toCheck.Dequeue();
            result.Add(current);

            // Check adjacent tiles
            foreach (Vector2Int dir in directions)
            {
                Vector2Int next = current + dir;

                // Skip if out of bounds or already visited
                if (next.x < 0 || next.x >= width || next.y < 0 || next.y >= height || visited.Contains(next))
                    continue;

                // Skip if no tile or wrong value
                Tile2048 nextTile = gridCells[next.x, next.y];
                if (nextTile == null || nextTile.Value != targetValue)
                    continue;

                // Add to queues
                toCheck.Enqueue(next);
                visited.Add(next);
            }
        }

        return result;
    }

    // Find the newest (most recently placed/merged) tile in a group
    private Vector2Int FindNewestTileInGroup(List<Vector2Int> tiles)
    {
        // Always prioritize the last active tile if it's in this group
        if (lastActiveTilePosition.x >= 0 && tiles.Contains(lastActiveTilePosition))
        {
            Debug.Log($"Using last active tile at {lastActiveTilePosition} as merge center");
            return lastActiveTilePosition;
        }

        // If no last active tile, use the highest tile in the group (lowest row number)
        // This tends to make tiles move upwards rather than downwards
        Vector2Int highest = tiles[0];
        foreach (Vector2Int pos in tiles)
        {
            if (pos.y < highest.y)
            {
                highest = pos;
            }
        }

        return highest;
    }

    // Method to directly merge a projectile with an existing tile
    public void DirectlyMergeTiles(int column, int row, GameObject projectile)
    {
        Tile2048 existingTile = gridCells[column, row];
        Tile2048 projectileTile = projectile.GetComponent<Tile2048>();

        if (existingTile == null || projectileTile == null)
        {
            Debug.LogError("Cannot merge: one of the tiles is null");
            return;
        }

        // Calculate new value
        int newValue = existingTile.Value + projectileTile.Value;
        Debug.Log($"Directly merging projectile (value: {projectileTile.Value}) with existing tile (value: {existingTile.Value})");

        // Get the position of the existing tile
        Vector2 existingPosition = existingTile.GetComponent<RectTransform>().anchoredPosition;

        // Update the projectile (this will be our new tile)
        projectileTile.Value = newValue;
        projectile.GetComponent<RectTransform>().anchoredPosition = existingPosition;

        // Add animation
        RectTransform projectileRect = projectile.GetComponent<RectTransform>();
        if (projectileRect != null)
        {
            DOTween.Kill(projectileRect);
            Sequence seq = DOTween.Sequence();
            seq.Append(projectileRect.DOScale(1.3f, 0.1f).SetEase(Ease.OutQuad));
            seq.Append(projectileRect.DOScale(1.0f, 0.1f).SetEase(Ease.InQuad));
        }

        // Play sound effect
        if (AudioManager2048.Instance != null)
        {
            AudioManager2048.Instance.PlayMergeSound(newValue, currentCombo);
        }

        // Update score
        if (scoreManager != null)
        {
            scoreManager.AddScore(newValue);
            ShowFloatingText(column, row, newValue, projectileTile.GetComponent<Image>().color);
            TrackCombo(column, row);
        }

        // Destroy the existing tile and replace with the projectile in the grid
        Destroy(existingTile.gameObject);
        gridCells[column, row] = projectileTile;

        // CRITICAL: Track this as the most recent active tile
        lastActiveTilePosition = new Vector2Int(column, row);
        Debug.Log($"Setting last active position to directly merged tile: {lastActiveTilePosition}");

        // IMPORTANT CHANGE: Add a small delay before checking for additional matches
        // This prevents immediate merging with adjacent tiles of the same value
        StartCoroutine(DelayedMergeCheck(column, row, newValue));
    }

    // New method to delay checking for matches after a direct merge
    private IEnumerator DelayedMergeCheck(int column, int row, int value)
    {
        // Wait to let any animations finish and to create a noticeable pause
        yield return new WaitForSeconds(0.1f);

        // Check if the tile still exists and has the same value
        // (It might have been merged elsewhere in the meantime)
        if (gridCells[column, row] != null && gridCells[column, row].Value == value)
        {
            // Now check for adjacent tiles with the same value as our newly merged tile
            List<Vector2Int> adjacentMatches = GetAdjacentTilesWithValue(column, row, value);

            if (adjacentMatches.Count > 0)
            {
                // Log what we found for debugging
                Debug.Log($"Found {adjacentMatches.Count} adjacent matches to newly merged tile at ({column}, {row})");

                // Process these matches, maintaining the current tile as the center
                ProcessMergeIntoTile(column, row, adjacentMatches);
            }
            else
            {
                // No adjacent matches, check for connected tiles elsewhere
                StartCoroutine(CheckForMatchesWithDelay(0.05f));
            }
        }
        else
        {
            // The tile has changed or been removed, do a general check
            StartCoroutine(CheckForMatchesWithDelay(0.05f));
        }
    }

    // Method to directly merge a projectile with an existing tile


    private IEnumerator CheckForMergesAfterPlacement(int column, int row)
    {
        yield return new WaitForSeconds(0.1f); // Short delay to ensure physics/animations complete

        Debug.Log($"CheckForMergesAfterPlacement at ({column}, {row})");

        // Always set the last active position to the newly placed tile
        Vector2Int newTilePos = new Vector2Int(column, row);
        lastActiveTilePosition = newTilePos;

        // First attempt to use the new tile as merge center
        if (gridCells[column, row] != null)
        {
            int tileValue = gridCells[column, row].Value;

            // Directly check adjacent tiles (up, down, left, right)
            List<Vector2Int> adjacentMergePositions = new List<Vector2Int>();

            // Define the four adjacent directions
            Vector2Int[] directions = new Vector2Int[]
            {
                new Vector2Int(0, -1),  // down
                new Vector2Int(0, 1),   // up
                new Vector2Int(-1, 0),  // left
                new Vector2Int(1, 0)    // right
            };

            // Check all four directions for tiles with same value
            foreach (Vector2Int dir in directions)
            {
                Vector2Int adjPos = new Vector2Int(column, row) + dir;

                // Check if in bounds
                if (adjPos.x >= 0 && adjPos.x < width && adjPos.y >= 0 && adjPos.y < height)
                {
                    Tile2048 adjTile = gridCells[adjPos.x, adjPos.y];
                    if (adjTile != null && adjTile.Value == tileValue)
                    {
                        adjacentMergePositions.Add(adjPos);
                    }
                }
            }

            // If we found adjacent tiles with the same value
            if (adjacentMergePositions.Count > 0)
            {
                Debug.Log($"Found {adjacentMergePositions.Count} adjacent tiles to merge with newly placed tile");

                // Merge all these tiles into the center tile
                MergeMultipleTilesForced(column, row, adjacentMergePositions);

                // Wait a moment for animations
                yield return new WaitForSeconds(0.3f);

                // Apply gravity after the first merge
                for (int col = 0; col < width; col++)
                {
                    ApplyGravity(col);
                }

                // Wait for gravity animations
                yield return new WaitForSeconds(0.2f);
            }
        }

        // Instead of a do-while loop, use a more controlled approach to check the entire grid for matches
        yield return StartCoroutine(CheckForMatchesWithDelay(0.1f));
    }

    // New method specifically for merging adjacent tiles into a newly placed center tile
    private void MergeMultipleTilesForced(int centerCol, int centerRow, List<Vector2Int> tilesToMerge)
    {
        Tile2048 centerTile = gridCells[centerCol, centerRow];
        if (centerTile == null) return;

        Debug.Log($"Forced merge into tile at ({centerCol}, {centerRow}) with value {centerTile.Value}");

        int originalValue = centerTile.Value;
        int totalValue = originalValue;
        List<int> affectedColumns = new List<int>();

        // Add a pulsing scale animation to the center tile
        RectTransform centerRect = centerTile.GetComponent<RectTransform>();
        if (centerRect != null)
        {
            // Kill any existing animations
            DOTween.Kill(centerRect);
            centerRect.localScale = Vector3.one;

            // Create new animation sequence
            Sequence seq = DOTween.Sequence();
            seq.Append(centerRect.DOScale(1.3f, 0.1f).SetEase(Ease.OutQuad));
            seq.Append(centerRect.DOScale(1.0f, 0.1f).SetEase(Ease.InQuad));
            seq.OnComplete(() =>
            {
                if (centerRect != null)
                    centerRect.localScale = Vector3.one;
            });
        }

        // Merge all tiles into the center
        foreach (Vector2Int pos in tilesToMerge)
        {
            Tile2048 tile = gridCells[pos.x, pos.y];
            if (tile == null) continue;

            Debug.Log($"Merging tile at ({pos.x}, {pos.y}) with value {tile.Value} into center");

            // Add to total value
            totalValue += tile.Value;

            // Track affected columns for gravity
            if (!affectedColumns.Contains(pos.x))
                affectedColumns.Add(pos.x);

            // Clear the grid reference and destroy the object
            gridCells[pos.x, pos.y] = null;
            Destroy(tile.gameObject);
        }

        // Update the center tile with the new value
        centerTile.Value = totalValue;

        // Play merge sound
        if (AudioManager2048.Instance != null)
        {
            AudioManager2048.Instance.PlayMergeSound(totalValue, currentCombo);
        }

        // Add score and show UI effects
        if (scoreManager != null && tilesToMerge.Count > 0)
        {
            scoreManager.AddScore(totalValue);
            ShowFloatingText(centerCol, centerRow, totalValue, centerTile.GetComponent<Image>().color);

            if (CoinManager2048.Instance != null)
            {
                CoinManager2048.Instance.AddCoinsFromMerge(totalValue, currentCombo);
            }

            TrackCombo(centerCol, centerRow);
        }

        // Update last active position to the merged tile
        lastActiveTilePosition = new Vector2Int(centerCol, centerRow);
        Debug.Log($"Updated last active position to: {lastActiveTilePosition}");

        // Apply gravity to all affected columns
        if (!affectedColumns.Contains(centerCol))
            affectedColumns.Add(centerCol);

        foreach (int col in affectedColumns)
        {
            ApplyGravity(col);
        }
    }

    private bool CheckConnectedTiles(int column, int row)
    {
        if (gridCells[column, row] == null) return false;

        int tileValue = gridCells[column, row].Value;

        // Find all connected tiles with the same value using BFS
        List<Vector2Int> connectedTiles = FindConnectedTiles(column, row, tileValue);

        // If we found tiles to merge (more than just the starting tile)
        if (connectedTiles.Count > 1)
        {
            // Find the best candidate to be the "center" of the merge
            // Always prioritize the last active (newest) tile position
            Vector2Int mergeCenterPos = FindBestMergeCenter(connectedTiles);

            Debug.Log($"Found {connectedTiles.Count} connected tiles with value {tileValue}. Merge center: {mergeCenterPos}");

            // Remove the center tile from the list since we'll merge into it
            connectedTiles.Remove(mergeCenterPos);

            // Call merge with the selected center position
            MergeMultipleTilesForced(mergeCenterPos.x, mergeCenterPos.y, connectedTiles);
            return true;
        }

        return false;
    }

    // Updated helper method to find the best merge center
    private Vector2Int FindBestMergeCenter(List<Vector2Int> connectedTiles)
    {
        // Highest priority: use the last active tile (newest placed or merged tile)
        if (lastActiveTilePosition.x >= 0 &&
            connectedTiles.Contains(lastActiveTilePosition))
        {
            Debug.Log($"Using last active tile at {lastActiveTilePosition} as merge center");
            return lastActiveTilePosition;
        }

        // Second priority: if the tiles include a projectile that was just shot, use that
        // We'll need to identify this somehow - for now we'll just use the first tile
        return connectedTiles[0];
    }

    private List<Vector2Int> FindConnectedTiles(int startCol, int startRow, int targetValue)
    {
        List<Vector2Int> connectedTiles = new List<Vector2Int>();
        Queue<Vector2Int> tilesToCheck = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        // Start with the initial tile
        Vector2Int startPos = new Vector2Int(startCol, startRow);
        tilesToCheck.Enqueue(startPos);
        visited.Add(startPos);

        // Directions: up, down, left, right
        Vector2Int[] directions = new Vector2Int[]
        {
            new Vector2Int(0, -1),  // down
            new Vector2Int(0, 1),   // up
            new Vector2Int(-1, 0),  // left
            new Vector2Int(1, 0)    // right
        };

        while (tilesToCheck.Count > 0)
        {
            Vector2Int current = tilesToCheck.Dequeue();
            connectedTiles.Add(current);

            // Check all four directions
            foreach (var dir in directions)
            {
                Vector2Int nextPos = current + dir;

                // Check if in bounds and not visited
                if (nextPos.x >= 0 && nextPos.x < width &&
                    nextPos.y >= 0 && nextPos.y < height &&
                    !visited.Contains(nextPos))
                {
                    // If tile exists and has the same value
                    Tile2048 nextTile = gridCells[nextPos.x, nextPos.y];
                    if (nextTile != null && nextTile.Value == targetValue)
                    {
                        tilesToCheck.Enqueue(nextPos);
                        visited.Add(nextPos);
                    }
                }
            }
        }

        return connectedTiles;
    }

    private void MergeMultipleTiles(int centerCol, int centerRow, List<Vector2Int> tilesToMerge)
    {
        Tile2048 centerTile = gridCells[centerCol, centerRow];
        if (centerTile == null) return;

        // Store position of the merge center for clarity
        Vector2Int centerPos = new Vector2Int(centerCol, centerRow);

        // Get the initial value of the center tile
        int originalValue = centerTile.Value;
        int totalValue = originalValue;
        List<int> affectedColumns = new List<int>();

        // Adding debug log to track merge center
        Debug.Log($"Merging tiles into center: ({centerCol}, {centerRow}) with value {originalValue}");

        // Add a pulsing scale animation to the center tile
        RectTransform centerRectTransform = centerTile.GetComponent<RectTransform>();
        if (centerRectTransform == null) return;

        // Animation code
        // ...existing animation code...

        // Add up all the values and destroy adjacent tiles
        foreach (var pos in tilesToMerge)
        {
            Tile2048 adjacentTile = gridCells[pos.x, pos.y];
            if (adjacentTile == null) continue;

            Debug.Log($"Merging tile at ({pos.x}, {pos.y}) with value {adjacentTile.Value} into center");

            totalValue += adjacentTile.Value;
            gridCells[pos.x, pos.y] = null;
            Destroy(adjacentTile.gameObject);

            if (!affectedColumns.Contains(pos.x))
                affectedColumns.Add(pos.x);
        }

        // Update the center tile with the new value
        centerTile.Value = totalValue;

        // Play merge sound
        if (AudioManager2048.Instance != null)
        {
            AudioManager2048.Instance.PlayMergeSound(totalValue, currentCombo);
        }

        // Add the new merged tile value to the score
        if (scoreManager != null && tilesToMerge.Count > 0)
        {
            int scoreToAdd = totalValue;
            scoreManager.AddScore(scoreToAdd);

            // Create floating score text
            ShowFloatingText(centerCol, centerRow, scoreToAdd, centerTile.GetComponent<Image>().color);

            // Award coins for merge
            if (CoinManager2048.Instance != null)
            {
                CoinManager2048.Instance.AddCoinsFromMerge(totalValue, currentCombo);
            }

            // Handle combo tracking
            TrackCombo(centerCol, centerRow);
        }

        // If the new value is 1, destroy the tile (special case for certain game mechanics)
        if (totalValue == 1)
        {
            gridCells[centerCol, centerRow] = null;
            Destroy(centerTile.gameObject);
            lastActiveTilePosition = new Vector2Int(-1, -1);  // Reset if tile was destroyed
        }
        else
        {
            // After successful merge, update the last active position to this merged tile
            lastActiveTilePosition = new Vector2Int(centerCol, centerRow);
            Debug.Log($"Setting last active position to merged tile: {lastActiveTilePosition}");
        }

        // Apply gravity to all affected columns
        if (!affectedColumns.Contains(centerCol))
            affectedColumns.Add(centerCol);

        foreach (int col in affectedColumns)
        {
            ApplyGravity(col);
        }

        // Check for additional merges with the new value if it wasn't destroyed
        if (totalValue != 1)
        {
            StartCoroutine(CheckForMergesAfterPlacement(centerCol, centerRow));
        }

        // Check for game over
        GameOverManager2048 gameOverManager = FindObjectOfType<GameOverManager2048>();
        if (gameOverManager != null)
        {
            gameOverManager.ForceCheckGameOver();
        }
    }

    // Improve the EnsureNormalScale method to be more reliable
    private IEnumerator EnsureNormalScale(RectTransform rectTransform, Vector3 targetScale, float delay)
    {
        // Wait slightly longer than the full animation duration
        yield return new WaitForSeconds(delay);

        // If this object still exists, force it back to normal scale
        if (rectTransform != null && rectTransform.gameObject != null)
        {
            // Kill any ongoing animations first
            DOTween.Kill(rectTransform);
            rectTransform.localScale = targetScale;
        }

        yield return new WaitForSeconds(0.05f); // Small additional delay for safety

        // Double check one more time
        if (rectTransform != null && rectTransform.gameObject != null)
        {
            rectTransform.localScale = targetScale;
        }
    }

    // Add this new method to create floating text
    private void ShowFloatingText(int col, int row, int score, Color tileColor)
    {
        if (floatingTextPrefab == null) return;

        // Calculate the world position for the text (same as the tile)
        Vector3 tilePosition = GetWorldPositionForCell(col, row);

        // Instantiate the floating text prefab
        GameObject floatingTextObj = Instantiate(floatingTextPrefab, tilePosition, Quaternion.identity, transform);

        // Initialize the floating text component
        FloatingText floatingText = floatingTextObj.GetComponent<FloatingText>();
        if (floatingText != null)
        {
            floatingText.Initialize(score, tileColor);
        }
    }

    // Add this new method to track combos
    private void TrackCombo(int col, int row)
    {
        float currentTime = Time.time;

        // If this merge happened within the combo reset time, increment combo
        if (currentTime - lastMergeTime < comboResetTime)
        {
            currentCombo++;
        }
        else
        {
            // Reset combo if too much time has passed
            currentCombo = 1;
            comboTextShown = false; // Reset the flag when starting a new combo
        }

        // Update the last merge time
        lastMergeTime = currentTime;

        // Cancel any existing combo reset coroutine
        if (comboResetCoroutine != null)
        {
            StopCoroutine(comboResetCoroutine);
        }

        // Start a new coroutine to reset combo after the timeout
        comboResetCoroutine = StartCoroutine(ResetComboAfterDelay());

        // Only show combo text if combo is greater than 1 AND text hasn't been shown yet
        if (currentCombo > 1 && !comboTextShown)
        {
            // Set flag to prevent duplicate combo text
            comboTextShown = true;

            // Show combo text with a delay after score text
            StartCoroutine(ShowComboTextWithDelay(col, row));
        }
    }

    // Add this method to show combo text with delay
    private IEnumerator ShowComboTextWithDelay(int col, int row)
    {
        // Wait a bit to not overlap with the score text
        yield return new WaitForSeconds(comboDelay);

        if (comboTextPrefab != null)
        {
            // Get position
            Vector3 position = GetWorldPositionForCell(col, row);

            // Instantiate the combo text object
            GameObject comboTextObj = Instantiate(comboTextPrefab, position, Quaternion.identity, transform);

            // Initialize the combo text component
            ComboText comboText = comboTextObj.GetComponent<ComboText>();
            if (comboText != null)
            {
                comboText.Initialize(currentCombo);
            }
        }
    }

    // Add this method to reset combo after inactivity
    private IEnumerator ResetComboAfterDelay()
    {
        yield return new WaitForSeconds(comboResetTime);
        currentCombo = 0;
        comboTextShown = false; // Reset the flag when the combo resets
    }

    // Helper method to get world position for a grid cell
    private Vector3 GetWorldPositionForCell(int col, int row)
    {
        // Get the anchored position from existing method
        Vector2 anchoredPos = GetCellPosition(col, row);

        // Convert anchored position to world position
        RectTransform gridRectTransform = gridParent as RectTransform;
        Vector3 worldPos = gridRectTransform.TransformPoint(new Vector3(anchoredPos.x, anchoredPos.y, 0));

        return worldPos;
    }

    // Replace the old MergeTiles method with a call to the new method
    private void MergeTiles(int col1, int row1, int col2, int row2)
    {
        List<Vector2Int> tilesToMerge = new List<Vector2Int>
        {
            new Vector2Int(col2, row2)
        };
        MergeMultipleTiles(col1, row1, tilesToMerge);
    }

    // Method to move tiles up to fill empty spaces in a column
    public void ApplyGravity(int column)
    {
        // Create a list to store all moves that need to happen
        List<(int fromRow, int toRow)> moves = new List<(int fromRow, int toRow)>();

        // First pass: identify all empty cells and find tiles to move
        for (int row = 0; row < height; row++)
        {
            // If current cell is empty, find a tile below to move up
            if (gridCells[column, row] == null)
            {
                // Look for the next non-empty cell below
                for (int nextRow = row + 1; nextRow < height; nextRow++)
                {
                    if (gridCells[column, nextRow] != null)
                    {
                        // Record this move
                        moves.Add((nextRow, row));
                        break;
                    }
                }
            }
        }

        // Second pass: Execute all moves in order (from top to bottom)
        if (moves.Count > 0)
        {
            isApplyingGravity = true; // Set flag when starting gravity operations
            StartCoroutine(MoveTilesUp(column, moves));
        }
    }
    private IEnumerator MoveTilesUp(int column, List<(int fromRow, int toRow)> moves)
    {
        // Sort moves from top to bottom to avoid conflicts
        moves.Sort((a, b) => a.toRow.CompareTo(b.toRow));

        // Process each move
        foreach (var move in moves)
        {
            int fromRow = move.fromRow;
            int toRow = move.toRow;

            Tile2048 tile = gridCells[column, fromRow];
            if (tile == null) continue;

            // Update grid array
            gridCells[column, fromRow] = null;
            gridCells[column, toRow] = tile;

            // Get target position
            Vector2 targetPosition = GetCellPosition(column, toRow);

            // Check if this tile was at the lastActiveTilePosition
            if (lastActiveTilePosition.x == column && lastActiveTilePosition.y == fromRow)
            {
                lastActiveTilePosition = new Vector2Int(column, toRow);
                Debug.Log($"Updated lastActiveTilePosition to {lastActiveTilePosition} due to gravity");
            }

            // Animate movement
            RectTransform rectTransform = tile.GetComponent<RectTransform>();
            if (rectTransform == null) continue;

            Vector2 startPosition = rectTransform.anchoredPosition;
            float duration = 0.2f;
            float elapsedTime = 0;

            // Animate the movement
            while (elapsedTime < duration)
            {
                if (rectTransform == null || tile == null) break;

                rectTransform.anchoredPosition = Vector2.Lerp(
                    startPosition,
                    targetPosition,
                    elapsedTime / duration
                );
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            if (rectTransform != null)
                rectTransform.anchoredPosition = targetPosition;
        }

        // After all movements and merges are complete
        isApplyingGravity = false;

        // Verify column integrity after all movements and merges
        yield return StartCoroutine(VerifyColumnIntegrity(column));

        // Check for game over after all movement and merges
        GameOverManager2048 gameOverManager = FindObjectOfType<GameOverManager2048>();
        if (gameOverManager != null)
        {
            gameOverManager.ForceCheckGameOver();
        }
    }

    // New method to ensure there are no gaps in columns
    private IEnumerator VerifyColumnIntegrity(int column)
    {
        bool needsFixing = false;

        // Check for gaps (empty cells with non-empty cells above them)
        for (int row = height - 2; row >= 0; row--)
        {
            // If this cell has a tile but the one below is empty, we have a gap
            if (gridCells[column, row] != null && gridCells[column, row + 1] == null)
            {
                needsFixing = true;
                break;
            }
        }

        // If we found gaps, apply gravity again
        if (needsFixing)
        {
            Debug.Log($"Found gaps in column {column}, reapplying gravity");
            yield return new WaitForSeconds(0.1f);
            ApplyGravity(column);
        }
    }

    // Remove or keep the old MoveTileUp method for backwards compatibility
    private IEnumerator MoveTileUp(int column, int fromRow, int toRow)
    {
        // This method is now effectively replaced by MoveTilesUp
        // Adding code to delegate to the new system
        List<(int, int)> moves = new List<(int, int)>();
        moves.Add((fromRow, toRow));
        yield return StartCoroutine(MoveTilesUp(column, moves));
    }

    // Check for merges at a specific position
    private IEnumerator CheckForMergesAtPosition(int column, int row)
    {
        if (gridCells[column, row] == null) yield break;

        bool merged = CheckConnectedTiles(column, row);

        // If we merged, wait a bit to let animations complete
        if (merged)
        {
            yield return new WaitForSeconds(0.2f);
        }
    }

    // Add this new method to check for merges after swapping tiles
    public IEnumerator CheckForMergesAfterSwap(Vector2Int pos1, Vector2Int pos2)
    {
        yield return new WaitForSeconds(0.1f); // Short delay to ensure animations complete

        // Check for merges at both positions
        bool merged1 = CheckConnectedTiles(pos1.x, pos1.y);
        yield return new WaitForSeconds(0.2f); // Delay between checks

        bool merged2 = false;
        // Only check second position if the first one didn't cause a chain reaction that affected it
        if (gridCells[pos2.x, pos2.y] != null)
        {
            merged2 = CheckConnectedTiles(pos2.x, pos2.y);
            yield return new WaitForSeconds(0.2f);
        }

        // If no merges happened directly, still check for gravity and additional matches
        if (!merged1 && !merged2)
        {
            // Apply gravity to all columns after swapping
            for (int col = 0; col < width; col++)
            {
                ApplyGravity(col);
                yield return new WaitForSeconds(0.1f);
            }
        }
    }

    // Find the first empty row in the specified column
    public int GetEmptyRowInColumn(int column)
    {
        if (column < 0 || column >= width)
        {
            Debug.LogError($"Invalid column: {column}");
            return -1;
        }

        for (int row = 0; row < height; row++)
        {
            if (gridCells[column, row] == null)
            {
                return row;
            }
        }

        return -1; // No empty cells found
    }

    // Get the position for a grid cell
    public Vector2 GetCellPosition(int x, int y)
    {
        // Calculate the total grid width and height
        float gridWidth = width * (tileSizeX + spacing) - spacing;
        float gridHeight = height * (tileSizeY + spacing) - spacing;

        // Calculate position relative to the center of the grid
        float posX = -gridWidth / 2 + x * (tileSizeX + spacing) + tileSizeX / 2;
        float posY = gridHeight / 2 - y * (tileSizeY + spacing) - tileSizeY / 2;

        return new Vector2(posX, posY);
    }

    // Get the Transform for the grid parent
    public Transform GetGridParent()
    {
        return gridParent;
    }

    // Method to get a tile at a specific position
    public Tile2048 GetTileAt(int column, int row)
    {
        if (column < 0 || column >= width || row < 0 || row >= height)
        {
            return null;
        }

        return gridCells[column, row];
    }

    // Method to clear a tile at a specific position
    public void ClearTileAt(int column, int row)
    {
        if (column < 0 || column >= width || row < 0 || row >= height)
        {
            Debug.LogError($"Invalid cell coordinates: {column}, {row}");
            return;
        }

        gridCells[column, row] = null;
    }
}