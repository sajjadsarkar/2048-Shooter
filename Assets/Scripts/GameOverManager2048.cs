using UnityEngine;
using System.Collections;
using TMPro;

public class GameOverManager2048 : MonoBehaviour
{
    [SerializeField] private GridManager2048 gridManager;
    [SerializeField] private ShootingManager2048 shootingManager;
    [SerializeField] private GameObject gameOverPanel; // Assign in inspector
    [SerializeField] private TextMeshProUGUI gameOverScoreText; // Assign in inspector
    [SerializeField] private TextMeshProUGUI gameOverCoinsText; // Assign in inspector
    [SerializeField] private int baseGameOverCoinReward = 5;
    [SerializeField] private ScoreManager2048 scoreManager; // Reference to score manager

    [Header("Highest Tile Display")]
    [SerializeField] private GameObject tilePrefab;           // Same Tile prefab used by the grid
    [SerializeField] private Transform highestTileContainer;  // A RectTransform at (0,0,0) on the game over panel

    private bool isGameOver = false;
    private GameObject spawnedHighestTile; // Keep reference so we can destroy on restart

    private void Start()
    {
        // Find references if not assigned
        if (gridManager == null)
        {
            gridManager = FindObjectOfType<GridManager2048>();
        }

        if (shootingManager == null)
        {
            shootingManager = FindObjectOfType<ShootingManager2048>();
        }

        if (scoreManager == null)
        {
            scoreManager = FindObjectOfType<ScoreManager2048>();
        }

        // Ensure game over panel is hidden at start
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
    }

    // Public method to check for game over - can be called directly after tile placement
    public void CheckForGameOver()
    {
        if (!isGameOver && IsGridFullWithNoMoves())
        {
            GameOver();
        }
    }

    private bool IsGridFullWithNoMoves()
    {
        Debug.Log("Checking if game is over...");

        // Check if any cells are empty
        for (int x = 0; x < gridManager.Width; x++)
        {
            if (gridManager.GetEmptyRowInColumn(x) >= 0)
            {
                Debug.Log($"Found empty cell in column {x}, game is not over");
                return false; // Found an empty cell, game is not over
            }
        }

        Debug.Log("Grid is completely full, checking for possible merges...");

        // If we get here, the grid is full
        // Now check if the preview tile can merge with ANY bottom row tile (not just in the same column)
        int previewValue = shootingManager.GetNextTileValue();
        Debug.Log($"Preview tile value: {previewValue}");

        for (int x = 0; x < gridManager.Width; x++)
        {
            // Check bottom row tile (highest row index is at the bottom of the grid)
            Tile2048 bottomTile = gridManager.GetTileAt(x, gridManager.Height - 1);
            if (bottomTile != null && bottomTile.Value == previewValue)
            {
                Debug.Log($"Found mergeable tile in column {x}, game is not over");
                return false; // Found a mergeable tile, game is not over
            }
        }

        Debug.Log("No possible merges found. GAME OVER confirmed!");
        return true; // Grid is full and no merges possible
    }

    public void ForceCheckGameOver()
    {
        // Add a small delay to ensure all tile movements and merges are complete
        StartCoroutine(DelayedGameOverCheck());
    }

    private IEnumerator DelayedGameOverCheck()
    {
        // Wait for any ongoing animations or operations to complete
        yield return new WaitForSeconds(0.5f);
        CheckForGameOver();
    }

    private void GameOver()
    {
        isGameOver = true;
        Debug.Log("GAME OVER! Grid is full and no more moves possible.");

        if (gameOverPanel != null)
        {
            // Set the score text
            if (gameOverScoreText != null && scoreManager != null)
            {
                gameOverScoreText.text = "Score: " + scoreManager.GetScore().ToString();
            }

            // Calculate coin reward based on score
            int coinReward = CalculateCoinReward();

            // Set coin reward text
            if (gameOverCoinsText != null)
            {
                gameOverCoinsText.text = "+" + coinReward.ToString() + " coins";
            }

            // Award coins
            if (CoinManager2048.Instance != null)
            {
                CoinManager2048.Instance.AddCoins(coinReward);
            }

            // Show highest tile reached using the real tile prefab
            ShowHighestTile();

            // Show the panel
            gameOverPanel.SetActive(true);
        }
    }

    private void ShowHighestTile()
    {
        // Destroy any previously spawned tile (e.g. from a previous game)
        if (spawnedHighestTile != null)
        {
            Destroy(spawnedHighestTile);
            spawnedHighestTile = null;
        }

        if (tilePrefab == null || highestTileContainer == null || gridManager == null)
        {
            Debug.LogWarning("GameOverManager2048: tilePrefab or highestTileContainer not assigned. Cannot show highest tile.");
            return;
        }

        int highestValue = gridManager.GetHighestTileValue();
        if (highestValue <= 0)
        {
            Debug.LogWarning("GameOverManager2048: No tiles found on the grid.");
            return;
        }

        // Instantiate the tile as a child of the container (positioned at 0,0,0 on the game over canvas)
        spawnedHighestTile = Instantiate(tilePrefab, highestTileContainer);

        // Reset local transform so it sits exactly at the container's anchor point
        RectTransform rt = spawnedHighestTile.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
        }

        // Initialize the tile with the highest value (applies correct color + number via Tile2048)
        Tile2048 tile = spawnedHighestTile.GetComponent<Tile2048>();
        if (tile != null)
        {
            tile.EnableLandingBounce = false; // No bounce animation on display tile
            tile.Initialize(highestValue);
        }

        Debug.Log($"GameOverManager2048: Displayed highest tile with value {highestValue}.");
    }

    private int CalculateCoinReward()
    {
        if (scoreManager == null) return baseGameOverCoinReward;

        // Basic calculation: base reward + logarithmic bonus based on score
        int scoreBonus = Mathf.FloorToInt(Mathf.Log(Mathf.Max(1, scoreManager.GetScore()), 10));
        return baseGameOverCoinReward + scoreBonus;
    }

    // Call this from restart button
    public void RestartGame()
    {
        // Hide game over panel
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        // Destroy the displayed highest tile so it doesn't linger
        if (spawnedHighestTile != null)
        {
            Destroy(spawnedHighestTile);
            spawnedHighestTile = null;
        }

        ResetGameOver();

        // Reset score if needed
        if (scoreManager != null)
        {
            scoreManager.ResetScore();
        }

        // Reset coins for the new game session
        if (CoinManager2048.Instance != null)
        {
            CoinManager2048.Instance.ResetCoins();
        }

        // Call reset on the grid manager (you'll need to implement this)
        if (gridManager != null)
        {
            // Assuming you have or will add a ResetGrid method to GridManager2048
            // gridManager.ResetGrid();
        }
    }

    // Public method to reset the game over state (can be called when restarting the game)
    public void ResetGameOver()
    {
        isGameOver = false;
    }
}
