using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PowerUpsManager2048 : MonoBehaviour
{
    [SerializeField] private Button destroyButton;
    [SerializeField] private Image destroyButtonImage;
    [SerializeField] private TextMeshProUGUI destroyButtonText;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color activeColor = Color.red;
    [SerializeField] private string normalDestroyText = "Destroy Tile";
    [SerializeField] private string activeDestroyText = "Select Tile";

    // New fields for swap functionality
    [SerializeField] private Button swapButton;
    [SerializeField] private Image swapButtonImage;
    [SerializeField] private TextMeshProUGUI swapButtonText;
    [SerializeField] private Color swapActiveColor = Color.green;
    [SerializeField] private string normalSwapText = "Swap Tiles";
    [SerializeField] private string activeSwapText = "Select Tiles";

    // Add coin costs for powerups
    [SerializeField] private int destroyTileCost = 10;
    [SerializeField] private int swapTileCost = 15;

    // Cost display text components
    [SerializeField] private TextMeshProUGUI destroyCostText;
    [SerializeField] private TextMeshProUGUI swapCostText;

    // Not enough coins message
    [SerializeField] private GameObject notEnoughCoinsMessage;
    [SerializeField] private float messageDisplayTime = 2.0f;

    private bool destroyModeActive = false;
    private bool swapModeActive = false;
    private GameObject firstSelectedTile = null;

    public static PowerUpsManager2048 Instance { get; private set; }

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        // Add listener to the destroy button
        if (destroyButton != null)
        {
            destroyButton.onClick.AddListener(ToggleDestroyMode);
        }
        else
        {
            Debug.LogError("Destroy button not assigned to PowerUpsManager2048!");
        }

        // Add listener to the swap button
        if (swapButton != null)
        {
            swapButton.onClick.AddListener(ToggleSwapMode);
        }
        else
        {
            Debug.LogWarning("Swap button not assigned to PowerUpsManager2048!");
        }

        // Initialize UI
        UpdateButtonUI();
        UpdateCostUI();
    }

    // Update cost displays
    private void UpdateCostUI()
    {
        if (destroyCostText != null)
        {
            destroyCostText.text = destroyTileCost.ToString();
        }

        if (swapCostText != null)
        {
            swapCostText.text = swapTileCost.ToString();
        }
    }

    public void ToggleDestroyMode()
    {
        // If destroy mode is already active, just deactivate it
        if (destroyModeActive)
        {
            destroyModeActive = false;
            UpdateButtonUI();
            return;
        }

        // Check if player has enough coins
        if (CoinManager2048.Instance != null && !CoinManager2048.Instance.SpendCoins(destroyTileCost))
        {
            ShowNotEnoughCoinsMessage();
            return;
        }

        // If swap mode is active, deactivate it first
        if (swapModeActive)
        {
            swapModeActive = false;
            ClearSelectedTile();
        }

        destroyModeActive = true;
        UpdateButtonUI();

        Debug.Log("Destroy Mode: Active");
    }

    public void ToggleSwapMode()
    {
        // If swap mode is already active, just deactivate it
        if (swapModeActive)
        {
            swapModeActive = false;
            ClearSelectedTile();
            UpdateButtonUI();
            return;
        }

        // Check if player has enough coins
        if (CoinManager2048.Instance != null && !CoinManager2048.Instance.SpendCoins(swapTileCost))
        {
            ShowNotEnoughCoinsMessage();
            return;
        }

        // If destroy mode is active, deactivate it first
        if (destroyModeActive)
        {
            destroyModeActive = false;
        }

        swapModeActive = true;
        // Clear any selected tile when toggling swap mode
        ClearSelectedTile();
        UpdateButtonUI();

        Debug.Log("Swap Mode: Active");
    }

    private void ShowNotEnoughCoinsMessage()
    {
        if (notEnoughCoinsMessage != null)
        {
            notEnoughCoinsMessage.SetActive(true);
            StartCoroutine(HideMessageAfterDelay(messageDisplayTime));
        }
        else
        {
            Debug.Log("Not enough coins!");
        }
    }

    private System.Collections.IEnumerator HideMessageAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (notEnoughCoinsMessage != null)
        {
            notEnoughCoinsMessage.SetActive(false);
        }
    }

    public bool IsDestroyModeActive()
    {
        return destroyModeActive;
    }

    public bool IsSwapModeActive()
    {
        return swapModeActive;
    }

    public void TileDestroyed()
    {
        // Turn off destroy mode after a tile is destroyed
        destroyModeActive = false;
        UpdateButtonUI();

        Debug.Log("Tile destroyed, deactivating destroy mode");
    }

    public void SetSelectedTile(GameObject tile)
    {
        if (swapModeActive)
        {
            if (firstSelectedTile == null)
            {
                // First selection
                firstSelectedTile = tile;
                Debug.Log("First tile selected for swap");
            }
            else if (firstSelectedTile != tile)
            {
                // Second selection, attempt to swap
                TileSwapHandler handler = firstSelectedTile.GetComponent<TileSwapHandler>();
                if (handler != null)
                {
                    handler.TrySwapWith(tile);
                }

                // Clear selection and deactivate swap mode after attempt
                ClearSelectedTile();
                swapModeActive = false;
                UpdateButtonUI();
            }
        }
    }

    public void ClearSelectedTile()
    {
        firstSelectedTile = null;
    }

    public void TilesSwapped()
    {
        // Turn off swap mode after tiles are swapped
        swapModeActive = false;
        ClearSelectedTile();
        UpdateButtonUI();

        Debug.Log("Tiles swapped, deactivating swap mode");
    }

    private void UpdateButtonUI()
    {
        // Update destroy button UI
        if (destroyButtonImage != null)
        {
            destroyButtonImage.color = destroyModeActive ? activeColor : normalColor;
        }

        if (destroyButtonText != null)
        {
            destroyButtonText.text = destroyModeActive ? activeDestroyText : normalDestroyText;
        }

        // Update swap button UI
        if (swapButtonImage != null)
        {
            swapButtonImage.color = swapModeActive ? swapActiveColor : normalColor;
        }

        if (swapButtonText != null)
        {
            swapButtonText.text = swapModeActive ? activeSwapText : normalSwapText;
        }
    }

    // Getter methods for costs
    public int GetDestroyTileCost() => destroyTileCost;
    public int GetSwapTileCost() => swapTileCost;
}
