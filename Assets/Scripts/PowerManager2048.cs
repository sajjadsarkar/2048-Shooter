using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PowerUpsManager2048 : MonoBehaviour
{
    [SerializeField] private Button destroyButton;
    [SerializeField] private Image destroyButtonImage;
    [SerializeField] private TextMeshProUGUI destroyButtonText;

    [SerializeField] private Button swapButton;
    [SerializeField] private Image swapButtonImage;
    [SerializeField] private TextMeshProUGUI swapButtonText;

    [SerializeField] private int hammerUsesRemaining = 1;
    [SerializeField] private int undoUsesRemaining = 1;

    private bool hammerModeActive;
    private ShootingManager2048 shootingManager;

    public static PowerUpsManager2048 Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        shootingManager = FindObjectOfType<ShootingManager2048>();

        if (destroyButton != null)
        {
            destroyButton.onClick.AddListener(ToggleDestroyMode);
        }

        if (swapButton != null)
        {
            swapButton.onClick.AddListener(UndoLastMove);
        }

        UpdateButtonUI();
    }

    public void ToggleDestroyMode()
    {
        if (hammerUsesRemaining <= 0)
        {
            return;
        }

        hammerModeActive = !hammerModeActive;
        UpdateButtonUI();
    }

    public void UndoLastMove()
    {
        if (undoUsesRemaining <= 0 || shootingManager == null || !shootingManager.UndoLastMove())
        {
            return;
        }

        undoUsesRemaining--;
        UpdateButtonUI();
    }

    public bool IsDestroyModeActive()
    {
        return hammerModeActive;
    }

    public bool IsSwapModeActive()
    {
        return false;
    }

    public void TileDestroyed()
    {
        hammerModeActive = false;
        hammerUsesRemaining--;
        UpdateButtonUI();
    }

    public void SetSelectedTile(GameObject tile)
    {
    }

    public void ClearSelectedTile()
    {
    }

    public void TilesSwapped()
    {
    }

    private void UpdateButtonUI()
    {
        if (destroyButtonImage != null)
        {
            destroyButtonImage.color = hammerModeActive ? Color.red : Color.white;
        }

        if (swapButtonImage != null)
        {
            swapButtonImage.color = Color.white;
        }

        if (destroyButtonText != null)
        {
            destroyButtonText.text = hammerUsesRemaining.ToString();
        }

        if (swapButtonText != null)
        {
            swapButtonText.text = undoUsesRemaining.ToString();
        }
    }
}
