using UnityEngine;
using TMPro;

/// <summary>
/// Drives the main menu shown before a run starts. Displays the all-time high score and the
/// all-time highest block reached (both loaded from local save via ScoreManager2048), and
/// freezes gameplay behind the menu until the player presses Play.
/// </summary>
public class MainMenuManager2048 : MonoBehaviour
{
    [SerializeField] private GameObject mainMenuPanel; // Root panel to show/hide
    [SerializeField] private TextMeshProUGUI highScoreText; // Displays the saved all-time best score
    [SerializeField] private GameObject tilePrefab; // Same Tile prefab used by the grid
    [SerializeField] private Transform bestBlockContainer; // A RectTransform at (0,0,0) on the menu panel

    private GameObject spawnedBestBlockTile; // Keep reference so we can destroy it if the menu is shown again

    public static bool SkipMenu;

    private void Start()
    {
        Application.targetFrameRate = 60;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        if (SkipMenu)
        {
            SkipMenu = false;
            PlayGame();
            return;
        }
        ShowMenu();
    }

    /// <summary>
    /// Activates the menu panel, populates the saved records, and pauses gameplay.
    /// </summary>
    private void ShowMenu()
    {
        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(true);
        }

        if (highScoreText != null)
        {
            highScoreText.text = NumberFormatter.FormatNumber(ScoreManager2048.GetSavedBestScore());
        }

        ShowBestBlock();

        // Freeze gameplay (grid/preview/animations) until the player presses Play
        Time.timeScale = 0f;
    }

    /// <summary>
    /// Spawns a read-only copy of the highest block ever reached, reusing the real Tile2048
    /// prefab so the badge matches the in-game tile art exactly.
    /// </summary>
    private void ShowBestBlock()
    {
        if (spawnedBestBlockTile != null)
        {
            Destroy(spawnedBestBlockTile);
            spawnedBestBlockTile = null;
        }

        if (tilePrefab == null || bestBlockContainer == null)
        {
            Debug.LogWarning("MainMenuManager2048: tilePrefab or bestBlockContainer not assigned. Cannot show best block.");
            return;
        }

        int savedBestBlock = ScoreManager2048.GetSavedBestBlock();
        if (savedBestBlock <= 0)
        {
            return; // No record yet, leave the badge empty
        }

        spawnedBestBlockTile = Instantiate(tilePrefab, bestBlockContainer);

        RectTransform rt = spawnedBestBlockTile.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
        }

        Tile2048 tile = spawnedBestBlockTile.GetComponent<Tile2048>();
        if (tile != null)
        {
            tile.EnableLandingBounce = false; // No bounce animation on display tile
            tile.Initialize(savedBestBlock);
        }
    }

    // Call this from the Play button
    public void PlayGame()
    {
        Time.timeScale = 1f;

        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(false);
        }
    }
}
