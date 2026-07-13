using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

public class ShootingManager2048 : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [SerializeField] private GridManager2048 gridManager; // Reference to the grid manager
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float projectileSpeed = 500f;
    [SerializeField] private Vector2 previewOffset = new Vector2(0, -150f); // Offset for preview position
    [SerializeField] private Transform previewParent; // Parent transform for the preview tile
    [SerializeField] private float holdDelay = 0.2f; // Time before showing preview when holding

    // Dynamic value generation parameters
    [SerializeField] private int minValue = 2;
    [SerializeField] private float lowerValueBias = 0.7f; // Higher values make lower values more likely

    [SerializeField] private float pathHighlightAlpha = 0.3f; // Alpha transparency for path highlight

    private bool isShooting = false;
    private GameObject previewTile; // Reference to the preview tile
    private int nextTileValue; // Value of the next tile to shoot
    public int highestTileValue = 2; // Track the highest value on the board

    // Add a reference to the game over manager
    private GameOverManager2048 gameOverManager;

    // Ghost preview variables
    private bool isHolding = false;
    private int currentPreviewColumn = -1;
    private GameObject ghostTile; // Ghost tile that shows where tile will land
    private Coroutine holdCoroutine;

    // New variables for preview tile dragging
    private bool isPreviewDragging = false;
    private Vector2 previewOriginalPosition;

    private void Start()
    {
        // If no grid manager is assigned, try to find one in the scene
        if (gridManager == null)
        {
            gridManager = FindObjectOfType<GridManager2048>();
            if (gridManager == null)
            {
                Debug.LogError("ShootingManager2048 requires a reference to a GridManager2048 component!");
            }
        }

        // Find the game over manager
        gameOverManager = FindObjectOfType<GameOverManager2048>();

        // Create the initial preview tile
        CreatePreviewTile();
    }

    private void CreatePreviewTile()
    {
        // If there's already a preview tile, destroy it
        if (previewTile != null)
        {
            Destroy(previewTile);
        }

        // Generate a random value for the next tile
        nextTileValue = GetRandomStartingValue();

        // Determine which parent to use for the preview tile
        Transform parent = previewParent != null ? previewParent : gridManager.GetGridParent();

        // Create the preview tile
        previewTile = Instantiate(projectilePrefab, parent);
        RectTransform rt = previewTile.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(gridManager.TileSizeX, gridManager.TileSizeY);

        // Position the preview tile at the bottom center of the grid
        Vector2 bottomCenter = new Vector2(0, -(gridManager.Height * (gridManager.TileSizeY + gridManager.Spacing) / 2)) + previewOffset;
        rt.anchoredPosition = bottomCenter;

        // Initialize the tile with the next value
        Tile2048 tile = previewTile.GetComponent<Tile2048>();
        if (tile == null)
        {
            tile = previewTile.AddComponent<Tile2048>();
        }
        tile.Initialize(nextTileValue);

        // Add PreviewTileDragger component to the preview tile
        PreviewTileDragger dragger = previewTile.GetComponent<PreviewTileDragger>();
        if (dragger == null)
        {
            dragger = previewTile.AddComponent<PreviewTileDragger>();
        }

        // Add "Preview" to the name for debugging
        previewTile.name = "PreviewTile_" + nextTileValue;

        // Start with a small scale and animate to full size
        rt.localScale = new Vector3(0.2f, 0.2f, 1f);
        StartCoroutine(AnimatePreviewTileSpawn(rt));
    }

    // Add new coroutine for the preview tile spawn animation
    private IEnumerator AnimatePreviewTileSpawn(RectTransform rectTransform)
    {
        float duration = 0.2f; // Animation duration in seconds
        float elapsedTime = 0f;
        Vector3 startScale = new Vector3(0.2f, 0.2f, 1f);
        Vector3 targetScale = Vector3.one;

        while (elapsedTime < duration)
        {
            // Ensure the RectTransform still exists
            if (rectTransform == null)
                yield break;

            // Calculate smoothed scale value (using smoothstep for nicer easing)
            float t = elapsedTime / duration;
            float smoothT = t * t * (3f - 2f * t); // Smoothstep formula

            // Apply the scale
            rectTransform.localScale = Vector3.Lerp(startScale, targetScale, smoothT);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure we end at exactly the target scale
        if (rectTransform != null)
            rectTransform.localScale = targetScale;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (isShooting) return;

        isHolding = true;
        if (holdCoroutine != null)
        {
            StopCoroutine(holdCoroutine);
        }
        holdCoroutine = StartCoroutine(HoldCoroutine(eventData));
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isHolding || isShooting) return;

        UpdatePreviewPosition(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (isShooting) return;

        if (isHolding && ghostTile != null && currentPreviewColumn >= 0)
        {
            // If we were holding and have a valid preview, shoot at that column
            ShootAtColumn(currentPreviewColumn);
        }
        else if (!isHolding)
        {
            // Handle regular click (not a hold) using existing click logic
            HandleClick(eventData);
        }

        // Clean up
        isHolding = false;
        DestroyGhostTile();
        if (holdCoroutine != null)
        {
            StopCoroutine(holdCoroutine);
            holdCoroutine = null;
        }
    }

    private void HandleClick(PointerEventData eventData)
    {
        Vector2 localPoint;
        RectTransform gridRect = gridManager.GetGridParent().GetComponent<RectTransform>();

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            gridRect,
            eventData.position,
            eventData.pressEventCamera,
            out localPoint))
        {
            // Calculate the total width of the grid
            float gridWidth = gridManager.Width * (gridManager.TileSizeX + gridManager.Spacing) - gridManager.Spacing;

            // Adjust the click position relative to the grid's top-left corner
            float adjustedX = localPoint.x + gridWidth / 2;

            // Calculate the column
            int column = Mathf.FloorToInt(adjustedX / (gridManager.TileSizeX + gridManager.Spacing));

            // Validate the column is within bounds
            if (column >= 0 && column < gridManager.Width)
            {
                ShootAtColumn(column);
            }
        }
    }

    private IEnumerator HoldCoroutine(PointerEventData eventData)
    {
        // Wait a short delay before showing the preview
        yield return new WaitForSeconds(holdDelay);

        if (isHolding && !isShooting)
        {
            // Create ghost tile and show preview
            CreateGhostTile();
            UpdatePreviewPosition(eventData);
        }
    }

    private void UpdatePreviewPosition(PointerEventData eventData)
    {
        Vector2 localPoint;
        RectTransform gridRect = gridManager.GetGridParent().GetComponent<RectTransform>();

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            gridRect,
            eventData.position,
            eventData.pressEventCamera,
            out localPoint))
        {
            // Calculate the column using the same logic as in HandleClick
            float gridWidth = gridManager.Width * (gridManager.TileSizeX + gridManager.Spacing) - gridManager.Spacing;
            float adjustedX = localPoint.x + gridWidth / 2;
            int column = Mathf.FloorToInt(adjustedX / (gridManager.TileSizeX + gridManager.Spacing));

            // If column changed, update the preview position
            if (column != currentPreviewColumn && column >= 0 && column < gridManager.Width)
            {
                currentPreviewColumn = column;
                UpdateGhostTilePosition(column);
            }
        }
    }

    private void CreateGhostTile()
    {
        // Destroy existing ghost tile if any
        DestroyGhostTile();

        // Create a new ghost tile similar to the preview tile
        ghostTile = Instantiate(projectilePrefab, gridManager.GetGridParent());
        RectTransform rt = ghostTile.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(gridManager.TileSizeX, gridManager.TileSizeY);

        // Initialize the tile with the next value
        Tile2048 tile = ghostTile.GetComponent<Tile2048>();
        if (tile == null)
        {
            tile = ghostTile.AddComponent<Tile2048>();
        }
        tile.Initialize(nextTileValue);

        // Add "Ghost" to the name for debugging
        ghostTile.name = "GhostTile_" + nextTileValue;

        // Make it semi-transparent
        CanvasGroup canvasGroup = ghostTile.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0.5f;
    }

    private void UpdateGhostTilePosition(int column)
    {
        if (ghostTile == null) return;

        // Find the empty row in this column
        int targetRow = gridManager.GetEmptyRowInColumn(column);

        // If we found an empty slot
        if (targetRow >= 0)
        {
            // Position the ghost tile at the target position
            Vector2 targetPosition = gridManager.GetCellPosition(column, targetRow);
            RectTransform ghostRect = ghostTile.GetComponent<RectTransform>();
            if (ghostRect != null)
            {
                ghostRect.anchoredPosition = targetPosition;
            }
        }
        else
        {
            // Column is full, check if we can merge with the top tile
            int directMergeRow = FindDirectMergeRowInFullColumn(column, nextTileValue);
            if (directMergeRow >= 0)
            {
                // We can merge - show ghost tile at the merge position
                Vector2 targetPosition = gridManager.GetCellPosition(column, directMergeRow);
                RectTransform ghostRect = ghostTile.GetComponent<RectTransform>();
                if (ghostRect != null)
                {
                    ghostRect.anchoredPosition = targetPosition;
                }

                // Make the ghost tile semi-transparent to indicate a merge
                CanvasGroup canvasGroup = ghostTile.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 0.7f; // Slightly more visible to indicate merge
                }
            }
            else
            {
                // If column is full and no merge possible, hide the ghost tile
                DestroyGhostTile();
            }
        }
    }

    private void DestroyGhostTile()
    {
        if (ghostTile != null)
        {
            Destroy(ghostTile);
            ghostTile = null;
        }
        currentPreviewColumn = -1;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isShooting) return;

        Vector2 localPoint;
        RectTransform gridRect = gridManager.GetGridParent().GetComponent<RectTransform>();

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            gridRect,
            eventData.position,
            eventData.pressEventCamera,
            out localPoint))
        {
            // Calculate the total width of the grid
            float gridWidth = gridManager.Width * (gridManager.TileSizeX + gridManager.Spacing) - gridManager.Spacing;

            // Adjust the click position relative to the grid's top-left corner
            float adjustedX = localPoint.x + gridWidth / 2;

            // Calculate the column
            int column = Mathf.FloorToInt(adjustedX / (gridManager.TileSizeX + gridManager.Spacing));

            // Validate the column is within bounds
            if (column >= 0 && column < gridManager.Width)
            {
                ShootAtColumn(column);
            }
        }
    }

    // New public method to shoot at a specific column
    public void ShootAtColumn(int column)
    {
        if (isShooting) return;

        // Validate the column is within bounds
        if (column >= 0 && column < gridManager.Width)
        {
            Debug.Log($"Shooting at column: {column}");

            // Find the first empty row in this column
            int targetRow = gridManager.GetEmptyRowInColumn(column);

            // Get the value from the preview tile
            int valueToShoot = nextTileValue;

            // If column has empty space
            if (targetRow >= 0)
            {
                // Start the shooting coroutine with the preview tile's value
                StartCoroutine(ShootProjectile(column, targetRow, valueToShoot));

                // Create a new preview tile for the next shot
                CreatePreviewTile();
            }
            else
            {
                // Column is full - check if we can directly merge with the top tile
                int directMergeRow = FindDirectMergeRowInFullColumn(column, valueToShoot);
                if (directMergeRow >= 0)
                {
                    // We can merge - shoot at the merge target
                    StartCoroutine(ShootProjectile(column, directMergeRow, valueToShoot, true));

                    // Create a new preview tile for the next shot
                    CreatePreviewTile();
                }
                else
                {
                    Debug.Log($"Column {column} is full and no merge possible!");
                }
            }
        }
    }

    // Update this helper method to find a direct merge target in a full column
    private int FindDirectMergeRowInFullColumn(int column, int valueToShoot)
    {
        // Check all tiles in the column for a match, not just the bottom one
        for (int row = gridManager.Height - 1; row >= 0; row--)
        {
            Tile2048 tile = gridManager.GetTileAt(column, row);
            if (tile != null && tile.Value == valueToShoot)
            {
                return row;
            }
        }

        return -1; // No mergeable tile found
    }

    // Update ShootProjectile to handle gravity during shooting
    private IEnumerator ShootProjectile(int column, int targetRow, int tileValue, bool forceDirectMerge = false)
    {
        isShooting = true;

        // Wait for any existing gravity operations to complete before starting
        yield return StartCoroutine(WaitForGravityToComplete());

        // Re-validate the target row after waiting - it might have changed
        if (!forceDirectMerge)
        {
            int newTargetRow = gridManager.GetEmptyRowInColumn(column);
            // Only update if we still have a valid row
            if (newTargetRow >= 0)
            {
                targetRow = newTargetRow;
            }
        }

        // Calculate the target position
        Vector2 targetPosition = gridManager.GetCellPosition(column, targetRow);

        // Check if there's a tile below the target with the same value or if forceDirectMerge is true
        int directMergeRow = -1;

        if (forceDirectMerge)
        {
            // If forceDirectMerge is true, we're already targeting the merge row
            directMergeRow = targetRow;
        }
        else if (targetRow < gridManager.Height - 1)
        {
            // Check tiles below the target row
            for (int row = targetRow + 1; row < gridManager.Height; row++)
            {
                Tile2048 existingTile = gridManager.GetTileAt(column, row);
                if (existingTile != null)
                {
                    // If the existing tile has the same value, we can directly merge with it
                    if (existingTile.Value == tileValue)
                    {
                        directMergeRow = row;
                    }
                    break; // Stop checking once we find any tile
                }
            }
        }

        // Calculate the starting position (same X as target, but at the bottom of the grid)
        Vector2 startPosition = GetBottomPositionForColumn(column);

        // Create the projectile at the start position
        GameObject projectile = Instantiate(projectilePrefab, gridManager.GetGridParent());
        RectTransform rt = projectile.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(gridManager.TileSizeX, gridManager.TileSizeY);
        rt.anchoredPosition = startPosition;

        // Initialize the tile with the specified value (from preview)
        Tile2048 tile = projectile.GetComponent<Tile2048>();
        if (tile == null)
        {
            tile = projectile.AddComponent<Tile2048>();
        }
        tile.Initialize(tileValue);

        // Create path highlight effect
        GameObject pathHighlight = CreatePathHighlight(startPosition, targetPosition, tile.GetComponent<Image>().color);

        // If we have a direct merge target, adjust the target position
        Vector2 finalPosition = directMergeRow >= 0 ?
            gridManager.GetCellPosition(column, directMergeRow) : targetPosition;

        // Move the projectile straight up to the target position
        while (Vector2.Distance(rt.anchoredPosition, finalPosition) > 0.1f)
        {
            // Add null check to ensure projectile still exists
            if (projectile == null || rt == null)
            {
                // Clean up and exit if projectile was destroyed
                if (pathHighlight != null) Destroy(pathHighlight);
                isShooting = false;
                yield break;
            }

            rt.anchoredPosition = Vector2.MoveTowards(
                rt.anchoredPosition,
                finalPosition,
                projectileSpeed * Time.deltaTime
            );

            // Update the path highlight's position to follow under the tile
            if (pathHighlight != null && rt != null)
            {
                UpdatePathHighlight(pathHighlight, startPosition, finalPosition, rt.anchoredPosition);
            }

            yield return null;
        }

        // Check if projectile still exists before setting final position
        if (projectile != null && rt != null)
        {
            rt.anchoredPosition = finalPosition;
        }

        // Destroy the path highlight
        if (pathHighlight != null)
        {
            Destroy(pathHighlight);
        }

        // Play tile placed sound
        if (AudioManager2048.Instance != null)
        {
            AudioManager2048.Instance.PlayTilePlacedSound();
        }

        // Wait for any gravity operations that may have started during flight
        yield return StartCoroutine(WaitForGravityToComplete());

        // Re-validate the target position one more time
        if (!forceDirectMerge && directMergeRow < 0)
        {
            int newTargetRow = gridManager.GetEmptyRowInColumn(column);
            if (newTargetRow >= 0 && newTargetRow != targetRow)
            {
                // Position changed, update final position
                targetRow = newTargetRow;
                Vector2 newPosition = gridManager.GetCellPosition(column, targetRow);
                if (projectile != null && rt != null)
                {
                    rt.anchoredPosition = newPosition;
                }
            }
            else if (newTargetRow < 0)
            {
                // Column is now full, check for direct merge
                directMergeRow = FindDirectMergeRowInFullColumn(column, tileValue);
                if (directMergeRow >= 0 && projectile != null && rt != null)
                {
                    Vector2 mergePosition = gridManager.GetCellPosition(column, directMergeRow);
                    rt.anchoredPosition = mergePosition;
                }
                else
                {
                    // No placement possible anymore
                    if (projectile != null) Destroy(projectile);
                    isShooting = false;
                    yield break;
                }
            }
        }

        // Ensure projectile still exists before proceeding with merge or placement
        if (projectile != null)
        {
            if (directMergeRow >= 0)
            {
                // Direct merge with the existing tile
                gridManager.DirectlyMergeTiles(column, directMergeRow, projectile);
            }
            else
            {
                // Add the projectile to the grid normally
                gridManager.PlaceProjectileInCell(column, targetRow, projectile);
            }

            // Force apply gravity to ensure tiles move properly
            gridManager.ApplyGravity(column);

            // Wait a short time to allow gravity animations to start
            yield return new WaitForSeconds(0.05f);

            // Check adjacent columns too - for chain reactions
            if (column > 0) gridManager.ApplyGravity(column - 1);
            if (column < gridManager.Width - 1) gridManager.ApplyGravity(column + 1);

            // Add a longer pause here to ensure tile has settled before any game over check
            yield return new WaitForSeconds(0.05f);

            // Check for game over after placing the tile
            if (gameOverManager != null)
            {
                // Use ForceCheckGameOver instead of direct CheckForGameOver to ensure animations finish
                gameOverManager.ForceCheckGameOver();
            }
        }

        // Add a short delay before allowing another shot
        yield return new WaitForSeconds(0.05f);

        isShooting = false;
    }

    // Helper method to wait for gravity operations to complete
    private IEnumerator WaitForGravityToComplete()
    {
        // Short initial wait to allow any gravity operations to begin
        yield return new WaitForSeconds(0.05f);

        // Wait until GridManager says gravity operations are complete
        while (gridManager.IsApplyingGravity)
        {
            yield return null; // Wait a frame
        }

        // Add a small buffer time to ensure stability
        yield return new WaitForSeconds(0.05f);
    }

    // Create a path highlight that shows the trajectory
    private GameObject CreatePathHighlight(Vector2 startPos, Vector2 endPos, Color tileColor)
    {
        GameObject highlight = new GameObject("PathHighlight");
        highlight.transform.SetParent(gridManager.GetGridParent(), false);

        // Add required components
        Image highlightImage = highlight.AddComponent<Image>();
        RectTransform rt = highlight.GetComponent<RectTransform>();

        // Make the highlight a vertical bar that spans from start to end position
        float height = Mathf.Abs(endPos.y - startPos.y);
        // Use gridManager.TileSizeX instead of pathHighlightWidth for the width
        rt.sizeDelta = new Vector2(gridManager.TileSizeX, height);

        // Position the highlight at the midpoint between start and end
        rt.anchoredPosition = new Vector2(startPos.x, startPos.y + height / 2);

        // Apply tile color with transparency
        Color highlightColor = tileColor;
        highlightColor.a = pathHighlightAlpha;
        highlightImage.color = highlightColor;

        // Set the highlight behind other UI elements but still visible
        highlightImage.raycastTarget = false;
        highlightImage.maskable = true;

        return highlight;
    }

    // Update the path highlight as the tile moves
    private void UpdatePathHighlight(GameObject highlight, Vector2 startPos, Vector2 endPos, Vector2 currentTilePos)
    {
        if (highlight == null) return;

        RectTransform rt = highlight.GetComponent<RectTransform>();

        // Calculate the remaining path length
        float remainingHeight = Mathf.Abs(endPos.y - currentTilePos.y);

        // Update the size to show only the path beneath the current tile position
        // Use gridManager.TileSizeX instead of pathHighlightWidth for the width
        rt.sizeDelta = new Vector2(gridManager.TileSizeX, remainingHeight);

        // Position the highlight at the midpoint between the current position and end position
        rt.anchoredPosition = new Vector2(
            currentTilePos.x,
            currentTilePos.y - (remainingHeight / 2)
        );
    }

    private int GetRandomStartingValue()
    {
        // Find the current highest value on the board
        highestTileValue = FindHighestTileValue();

        // Calculate the maximum possible value for new tiles (around half of the highest value)
        int maxPossibleValue = Mathf.Max(4, highestTileValue / 2);

        // Create a list of possible values (powers of 2 up to the max)
        List<int> possibleValues = new List<int>();
        List<float> weights = new List<float>();

        // Add all powers of 2 from minValue up to maxPossibleValue
        for (int value = minValue; value <= maxPossibleValue; value *= 2)
        {
            possibleValues.Add(value);

            // Calculate weight - higher values get lower probability
            // Using log base 2 to account for exponential growth
            float weight = Mathf.Pow(lowerValueBias, Mathf.Log(value, 2) - 1);
            weights.Add(weight);
        }

        // Choose a random value based on the weights
        float totalWeight = 0;
        foreach (float weight in weights)
        {
            totalWeight += weight;
        }

        float randomValue = Random.Range(0, totalWeight);
        float weightSum = 0;

        for (int i = 0; i < possibleValues.Count; i++)
        {
            weightSum += weights[i];
            if (randomValue <= weightSum)
            {
                return possibleValues[i];
            }
        }

        // Fallback to minimum value
        return minValue;
    }

    private int FindHighestTileValue()
    {
        int highest = 2; // Default minimum

        // Loop through the entire grid
        for (int x = 0; x < gridManager.Width; x++)
        {
            for (int y = 0; y < gridManager.Height; y++)
            {
                Tile2048 tile = gridManager.GetTileAt(x, y);
                if (tile != null && tile.Value > highest)
                {
                    highest = tile.Value;
                }
            }
        }

        return highest;
    }

    // Helper method to get the position at the bottom of a column
    private Vector2 GetBottomPositionForColumn(int column)
    {
        // Get the X position of the column using the grid manager
        Vector2 columnPosition = gridManager.GetCellPosition(column, 0);

        // Calculate the total height of the grid
        float gridHeight = gridManager.Height * (gridManager.TileSizeY + gridManager.Spacing) - gridManager.Spacing;

        // Position is at the same X as the column, but below the grid
        float bottomY = -(gridHeight / 2 + gridManager.TileSizeY);

        return new Vector2(columnPosition.x, bottomY);
    }

    // New methods to handle preview tile dragging
    public void OnPreviewTileDragStart()
    {
        if (isShooting) return;

        isPreviewDragging = true;

        if (previewTile != null)
        {
            // Store original position for resetting later
            previewOriginalPosition = previewTile.GetComponent<RectTransform>().anchoredPosition;

            // Create ghost tile
            CreateGhostTile();
        }
    }

    public void OnPreviewTileDragged(PointerEventData eventData)
    {
        if (!isPreviewDragging || isShooting) return;

        // Determine which column we're hovering over
        UpdateDragPreviewPosition(eventData);
    }

    public void OnPreviewTileDragEnd(PointerEventData eventData)
    {
        if (!isPreviewDragging) return;

        isPreviewDragging = false;

        // If we have a valid column, shoot at it
        if (currentPreviewColumn >= 0 && currentPreviewColumn < gridManager.Width)
        {
            ShootAtColumn(currentPreviewColumn);
        }

        // Reset preview tile position and clean up
        if (previewTile != null)
        {
            previewTile.GetComponent<RectTransform>().anchoredPosition = previewOriginalPosition;
        }

        DestroyGhostTile();
    }

    private void UpdateDragPreviewPosition(PointerEventData eventData)
    {
        // Calculate which column we're over using the event position
        Vector2 localPoint;
        RectTransform gridRect = gridManager.GetGridParent().GetComponent<RectTransform>();

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            gridRect,
            eventData.position,
            eventData.pressEventCamera,
            out localPoint))
        {
            // Calculate the total width of the grid
            float gridWidth = gridManager.Width * (gridManager.TileSizeX + gridManager.Spacing) - gridManager.Spacing;

            // Adjust the position relative to the grid's center
            float adjustedX = localPoint.x + gridWidth / 2;

            // Calculate the column
            int column = Mathf.FloorToInt(adjustedX / (gridManager.TileSizeX + gridManager.Spacing));

            // Validate and update the column
            if (column >= 0 && column < gridManager.Width && column != currentPreviewColumn)
            {
                currentPreviewColumn = column;
                UpdateGhostTilePosition(column);
            }
        }
    }

    // New method to get the current preview tile's value
    public int GetNextTileValue()
    {
        return nextTileValue;
    }
}
