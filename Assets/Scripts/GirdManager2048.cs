using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

public class GridManager2048 : MonoBehaviour
{
    [SerializeField] private int width = 4;
    [SerializeField] private int height = 4;
    [SerializeField] private GameObject tilePrefab;
    [SerializeField] private Transform gridParent;
    public float tileSizeX = 100f;
    public float tileSizeY = 100f;
    [SerializeField] private float spacing = 10f;
    [SerializeField] private ScoreManager2048 scoreManager;
    [SerializeField] private GameObject floatingTextPrefab;
    [SerializeField] private GameObject comboTextPrefab;
    [SerializeField] private float comboDelay = 0.3f;
    [SerializeField] private float comboResetTime = 1.5f;

    private int currentCombo = 0;
    private float lastMergeTime;
    private Coroutine comboResetCoroutine;
    private bool comboTextShown = false;

    // Modified to store tile references instead of just GameObjects
    private Tile2048[,] gridCells;

    // Track if gravity operations are in progress
    // Counter instead of bool — multiple columns can run gravity simultaneously;
    // only report "done" when ALL of them have finished.
    private int activeGravityOps = 0;

    // True while a board-resolution pass (merges + resulting gravity) is running.
    // Surfaced through IsApplyingGravity so the shooter waits for merges too.
    private bool isResolving = false;

    // Only one resolution pass runs at a time; further requests are queued.
    private bool resolveQueued = false;

    public bool IsApplyingGravity => activeGravityOps > 0 || isResolving;

    public int Width => width;
    public int Height => height;
    public float TileSizeX => tileSizeX;
    public float TileSizeY => tileSizeY;
    public float Spacing => spacing;

    private Vector2Int lastActiveTilePosition = new Vector2Int(-1, -1);

    // Shared neighbour directions: down, up, left, right.
    private static readonly Vector2Int[] Directions = new Vector2Int[]
    {
        new Vector2Int(0, -1), // down
        new Vector2Int(0, 1),  // up
        new Vector2Int(-1, 0), // left
        new Vector2Int(1, 0)   // right
    };

    private GameOverManager2048 cachedGameOver;

    private void Start()
    {
        if (scoreManager == null)
        {
            scoreManager = FindObjectOfType<ScoreManager2048>();
        }

        cachedGameOver = FindObjectOfType<GameOverManager2048>();

        GenerateGrid();
        InitializeGridCells();
    }

    private void GenerateGrid()
    {
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

            RectTransform rectTransform = touchArea.GetComponent<RectTransform>();

            // Make the touch area slightly narrower than the cell
            float touchWidth = tileSizeX * 1f;

            // Make the touch area extend the full height of the grid plus extra padding
            float touchHeight = gridHeight + tileSizeY * 2;

            rectTransform.sizeDelta = new Vector2(touchWidth, touchHeight);

            float posX = -gridWidth / 2 + x * (tileSizeX + spacing) + tileSizeX / 2;
            rectTransform.anchoredPosition = new Vector2(posX, 0);

            Image image = touchArea.GetComponent<Image>();

            Color transparentColor = new Color(0, 0, 0, 0);
            image.color = transparentColor;

            image.raycastTarget = true;

            ColumnTouchHandler handler = touchArea.AddComponent<ColumnTouchHandler>();
            handler.Initialize(x);
        }
    }

    private void InitializeGridCells()
    {
        gridCells = new Tile2048[width, height];
    }

    // =========================================================================
    // ENTRY POINTS
    // Each entry point only (1) updates the grid + lastActive position, then
    // (2) asks the single ResolveBoard routine to process merges. This is what
    // removes the old "conflict": there is now exactly ONE routine that decides
    // where a merge happens, and only ONE instance of it runs at a time.
    // =========================================================================

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

        gridCells[column, row] = tile;
        Debug.Log($"Placed new tile at ({column}, {row}) with value {tile.Value}");

        // The tile the player just shot anchors any merge it is part of.
        lastActiveTilePosition = new Vector2Int(column, row);

        RequestResolve();
        return true;
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

        int newValue = existingTile.Value + projectileTile.Value;
        Debug.Log($"Directly merging projectile (value: {projectileTile.Value}) with existing tile (value: {existingTile.Value})");

        Vector2 existingPosition = existingTile.GetComponent<RectTransform>().anchoredPosition;

        // The projectile becomes the surviving tile (it is the newest one, so it
        // naturally anchors the merge in the next resolution pass).
        projectileTile.Value = newValue;
        projectile.GetComponent<RectTransform>().anchoredPosition = existingPosition;

        RectTransform projectileRect = projectile.GetComponent<RectTransform>();
        if (projectileRect != null)
        {
            DOTween.Kill(projectileRect);
            Sequence seq = DOTween.Sequence();
            seq.Append(projectileRect.DOScale(1.3f, 0.1f).SetEase(Ease.OutQuad));
            seq.Append(projectileRect.DOScale(1.0f, 0.1f).SetEase(Ease.InQuad));
        }

        if (AudioManager2048.Instance != null)
        {
            AudioManager2048.Instance.PlayMergeSound(newValue, currentCombo);
        }

        if (scoreManager != null)
        {
            scoreManager.AddScore(newValue);
            ShowFloatingText(column, row, newValue, projectileTile.GetComponent<Image>().color);
            TrackCombo(column, row);
        }

        Destroy(existingTile.gameObject);
        gridCells[column, row] = projectileTile;

        lastActiveTilePosition = new Vector2Int(column, row);

        RequestResolve();
    }

    // Called after a swap (values are exchanged in place, GameObjects stay put).
    public IEnumerator CheckForMergesAfterSwap(Vector2Int pos1, Vector2Int pos2)
    {
        lastActiveTilePosition = pos1;
        RequestResolve();
        yield break;
    }

    // =========================================================================
    // SINGLE MERGE RESOLUTION ROUTINE
    // =========================================================================

    public void RequestResolve()
    {
        if (isResolving)
        {
            resolveQueued = true;
            return;
        }
        StartCoroutine(ResolveBoard());
    }

    private IEnumerator ResolveBoard()
    {
        isResolving = true;

        // Let any just-placed tile settle for a frame before scanning.
        yield return null;

        int safety = 0;
        while (safety++ < 64)
        {
            List<List<Vector2Int>> groups = CollectMergeGroups();
            if (groups.Count == 0) break;

            // Merge every detected group this pass. All merges use the SAME
            // deterministic centre rule, so there is no directional conflict.
            foreach (List<Vector2Int> group in groups)
            {
                if (group == null || group.Count < 2) continue;
                if (gridCells[group[0].x, group[0].y] == null) continue; // already consumed
                MergeGroup(group);
            }

            // Brief pause so the player can see each merge before the board settles.
            yield return new WaitForSeconds(0.14f);

            // Settle everything, then check again (chain reactions).
            for (int c = 0; c < width; c++)
            {
                ApplyGravity(c);
            }

            // Let the scheduled gravity coroutines actually start (they only
            // increment activeGravityOps on their first frame).
            yield return null;

            // Wait only for real gravity ops (isResolving is excluded on purpose).
            while (activeGravityOps > 0) yield return null;
            yield return new WaitForSeconds(0.1f);
        }

        isResolving = false;

        if (cachedGameOver != null)
        {
            cachedGameOver.ForceCheckGameOver();
        }

        // If more merges were requested while we were busy, run again.
        if (resolveQueued)
        {
            resolveQueued = false;
            StartCoroutine(ResolveBoard());
        }
    }

    // Flood-fill the whole grid and return every connected group of tiles that
    // share a value and contain at least 2 tiles. A tile belongs to exactly one
    // group, so no group can be processed twice.
    private List<List<Vector2Int>> CollectMergeGroups()
    {
        var visited = new HashSet<Vector2Int>();
        var groups = new List<List<Vector2Int>>();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2Int start = new Vector2Int(x, y);
                if (visited.Contains(start)) continue;

                Tile2048 startTile = gridCells[x, y];
                if (startTile == null)
                {
                    visited.Add(start);
                    continue;
                }

                int targetValue = startTile.Value;
                var group = new List<Vector2Int>();
                var queue = new Queue<Vector2Int>();
                queue.Enqueue(start);
                visited.Add(start);

                while (queue.Count > 0)
                {
                    Vector2Int current = queue.Dequeue();
                    group.Add(current);

                    foreach (Vector2Int dir in Directions)
                    {
                        Vector2Int next = current + dir;
                        if (next.x < 0 || next.x >= width || next.y < 0 || next.y >= height)
                            continue;
                        if (visited.Contains(next)) continue;

                        Tile2048 nextTile = gridCells[next.x, next.y];
                        if (nextTile != null && nextTile.Value == targetValue)
                        {
                            visited.Add(next);
                            queue.Enqueue(next);
                        }
                    }
                }

                if (group.Count >= 2) groups.Add(group);
            }
        }

        return groups;
    }

    // THE merge rule (deterministic -> no "where does it go" conflict):
    //   1) If the player's most recent action tile is in this group, merge into it.
    //      (a shot/merged tile anchors the result where the player acted)
    //   2) Otherwise use the NEWEST tile in the group (highest CreationOrder).
    private Vector2Int ChooseMergeCenter(List<Vector2Int> group)
    {
        if (lastActiveTilePosition.x >= 0 && group.Contains(lastActiveTilePosition))
        {
            return lastActiveTilePosition;
        }

        Vector2Int best = group[0];
        int bestOrder = gridCells[best.x, best.y] != null ? gridCells[best.x, best.y].CreationOrder : -1;

        foreach (Vector2Int pos in group)
        {
            int order = gridCells[pos.x, pos.y] != null ? gridCells[pos.x, pos.y].CreationOrder : -1;
            if (order > bestOrder)
            {
                bestOrder = order;
                best = pos;
            }
        }

        return best;
    }

    // Merge an entire connected group into a single centre tile.
    private void MergeGroup(List<Vector2Int> group)
    {
        Vector2Int center = ChooseMergeCenter(group);
        Tile2048 centerTile = gridCells[center.x, center.y];
        if (centerTile == null) return;

        int totalValue = 0;
        var others = new List<Vector2Int>();

        foreach (Vector2Int pos in group)
        {
            Tile2048 tile = gridCells[pos.x, pos.y];
            if (tile == null) continue;

            totalValue += tile.Value;
            if (pos != center) others.Add(pos);
        }

        // Remove the consumed tiles from the grid immediately (so they can't be
        // merged again) but animate them being absorbed before destroying them.
        foreach (Vector2Int pos in others)
        {
            Tile2048 tile = gridCells[pos.x, pos.y];
            gridCells[pos.x, pos.y] = null;
            if (tile != null) StartCoroutine(AbsorbTile(tile, center));
        }

        centerTile.Value = totalValue;
        Pulse(centerTile);
        PlayMergeFeedback(center.x, center.y, totalValue, centerTile);

        lastActiveTilePosition = center;
    }

    // Smoothly slide a consumed tile into the centre and shrink it away.
    private IEnumerator AbsorbTile(Tile2048 tile, Vector2Int center)
    {
        RectTransform rt = tile.GetComponent<RectTransform>();
        if (rt == null)
        {
            Destroy(tile.gameObject);
            yield break;
        }

        DOTween.Kill(rt);
        Vector2 start = rt.anchoredPosition;
        Vector2 target = GetCellPosition(center.x, center.y);
        float duration = 0.12f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (tile == null || rt == null) yield break;

            float t = elapsed / duration;
            rt.anchoredPosition = Vector2.Lerp(start, target, t);
            rt.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (tile != null) Destroy(tile.gameObject);
    }

    private void Pulse(Tile2048 tile)
    {
        RectTransform rt = tile.GetComponent<RectTransform>();
        if (rt == null) return;

        DOTween.Kill(rt);
        rt.localScale = Vector3.one;

        Sequence seq = DOTween.Sequence();
        seq.Append(rt.DOScale(1.3f, 0.1f).SetEase(Ease.OutQuad));
        seq.Append(rt.DOScale(1.0f, 0.1f).SetEase(Ease.InQuad));
        seq.OnComplete(() =>
        {
            if (rt != null) rt.localScale = Vector3.one;
        });
    }

    private void PlayMergeFeedback(int col, int row, int value, Tile2048 tile)
    {
        if (AudioManager2048.Instance != null)
        {
            AudioManager2048.Instance.PlayMergeSound(value, currentCombo);
        }

        if (scoreManager != null)
        {
            scoreManager.AddScore(value);
            ShowFloatingText(col, row, value, tile.GetComponent<Image>().color);

            if (CoinManager2048.Instance != null)
            {
                CoinManager2048.Instance.AddCoinsFromMerge(value, currentCombo);
            }

            TrackCombo(col, row);
        }
    }

    // =========================================================================
    // FLOATING TEXT / COMBO
    // =========================================================================

    private void ShowFloatingText(int col, int row, int score, Color tileColor)
    {
        if (floatingTextPrefab == null) return;

        Vector3 tilePosition = GetWorldPositionForCell(col, row);

        GameObject floatingTextObj = Instantiate(floatingTextPrefab, tilePosition, Quaternion.identity, transform);

        FloatingText floatingText = floatingTextObj.GetComponent<FloatingText>();
        if (floatingText != null)
        {
            floatingText.Initialize(score, tileColor);
        }
    }

    private void TrackCombo(int col, int row)
    {
        float currentTime = Time.time;

        if (currentTime - lastMergeTime < comboResetTime)
        {
            currentCombo++;
        }
        else
        {
            currentCombo = 1;
            comboTextShown = false;
        }

        lastMergeTime = currentTime;

        if (comboResetCoroutine != null)
        {
            StopCoroutine(comboResetCoroutine);
        }

        comboResetCoroutine = StartCoroutine(ResetComboAfterDelay());

        if (currentCombo > 1 && !comboTextShown)
        {
            comboTextShown = true;
            StartCoroutine(ShowComboTextWithDelay(col, row));
        }
    }

    private IEnumerator ShowComboTextWithDelay(int col, int row)
    {
        yield return new WaitForSeconds(comboDelay);

        if (comboTextPrefab != null)
        {
            Vector3 position = GetWorldPositionForCell(col, row);

            GameObject comboTextObj = Instantiate(comboTextPrefab, position, Quaternion.identity, transform);

            ComboText comboText = comboTextObj.GetComponent<ComboText>();
            if (comboText != null)
            {
                comboText.Initialize(currentCombo);
            }
        }
    }

    private IEnumerator ResetComboAfterDelay()
    {
        yield return new WaitForSeconds(comboResetTime);
        currentCombo = 0;
        comboTextShown = false;
    }

    private Vector3 GetWorldPositionForCell(int col, int row)
    {
        Vector2 anchoredPos = GetCellPosition(col, row);

        RectTransform gridRectTransform = gridParent as RectTransform;
        Vector3 worldPos = gridRectTransform.TransformPoint(new Vector3(anchoredPos.x, anchoredPos.y, 0));

        return worldPos;
    }

    // =========================================================================
    // GRAVITY
    // =========================================================================

    public void ApplyGravity(int column)
    {
        List<(int fromRow, int toRow)> moves = new List<(int fromRow, int toRow)>();

        for (int row = 0; row < height; row++)
        {
            if (gridCells[column, row] == null)
            {
                for (int nextRow = row + 1; nextRow < height; nextRow++)
                {
                    if (gridCells[column, nextRow] != null)
                    {
                        moves.Add((nextRow, row));
                        break;
                    }
                }
            }
        }

        if (moves.Count > 0)
        {
            activeGravityOps++;
            StartCoroutine(MoveTilesUp(column, moves));
        }
    }

    private IEnumerator MoveTilesUp(int column, List<(int fromRow, int toRow)> moves)
    {
        moves.Sort((a, b) => a.toRow.CompareTo(b.toRow));

        foreach (var move in moves)
        {
            int fromRow = move.fromRow;
            int toRow = move.toRow;

            Tile2048 tile = gridCells[column, fromRow];
            if (tile == null) continue;

            gridCells[column, fromRow] = null;
            gridCells[column, toRow] = tile;

            Vector2 targetPosition = GetCellPosition(column, toRow);

            if (lastActiveTilePosition.x == column && lastActiveTilePosition.y == fromRow)
            {
                lastActiveTilePosition = new Vector2Int(column, toRow);
            }

            RectTransform rectTransform = tile.GetComponent<RectTransform>();
            if (rectTransform == null) continue;

            Vector2 startPosition = rectTransform.anchoredPosition;
            float duration = 0.2f;
            float elapsedTime = 0;

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
            {
                rectTransform.anchoredPosition = targetPosition;
                tile.PlayLandingBounce();
            }
        }

        activeGravityOps = Mathf.Max(0, activeGravityOps - 1);

        yield return StartCoroutine(VerifyColumnIntegrity(column));

        if (cachedGameOver != null)
        {
            cachedGameOver.ForceCheckGameOver();
        }
    }

    private IEnumerator VerifyColumnIntegrity(int column)
    {
        bool needsFixing = false;

        for (int row = height - 2; row >= 0; row--)
        {
            if (gridCells[column, row] != null && gridCells[column, row + 1] == null)
            {
                needsFixing = true;
                break;
            }
        }

        if (needsFixing)
        {
            Debug.Log($"Found gaps in column {column}, reapplying gravity");
            yield return new WaitForSeconds(0.1f);
            ApplyGravity(column);
        }
    }

    // =========================================================================
    // QUERIES
    // =========================================================================

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

        return -1;
    }

    public Vector2 GetCellPosition(int x, int y)
    {
        float gridWidth = width * (tileSizeX + spacing) - spacing;
        float gridHeight = height * (tileSizeY + spacing) - spacing;

        float posX = -gridWidth / 2 + x * (tileSizeX + spacing) + tileSizeX / 2;
        float posY = gridHeight / 2 - y * (tileSizeY + spacing) - tileSizeY / 2;

        return new Vector2(posX, posY);
    }

    public Transform GetGridParent()
    {
        return gridParent;
    }

    public Tile2048 GetTileAt(int column, int row)
    {
        if (column < 0 || column >= width || row < 0 || row >= height)
        {
            return null;
        }

        return gridCells[column, row];
    }

    public void ClearTileAt(int column, int row)
    {
        if (column < 0 || column >= width || row < 0 || row >= height)
        {
            Debug.LogError($"Invalid cell coordinates: {column}, {row}");
            return;
        }

        gridCells[column, row] = null;
    }

    public int GetHighestTileValue()
    {
        int highest = 0;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (gridCells[x, y] != null && gridCells[x, y].Value > highest)
                {
                    highest = gridCells[x, y].Value;
                }
            }
        }
        return highest;
    }
}
