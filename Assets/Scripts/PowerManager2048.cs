using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Serialization;
using DG.Tweening;

public class PowerUpsManager2048 : MonoBehaviour
{
    [SerializeField] private Button destroyButton;
    [SerializeField] private Image destroyButtonImage;
    [SerializeField] private TextMeshProUGUI destroyButtonText;

    [SerializeField] private Button swapButton;
    [SerializeField] private Image swapButtonImage;
    [SerializeField] private TextMeshProUGUI swapButtonText;

    [SerializeField] private int hammerUsesRemaining = 1;
    [FormerlySerializedAs("undoUsesRemaining")]
    [SerializeField] private int swapUsesRemaining = 1;

    private static readonly Color Active = new Color(1f, 1f, 0.6f);
    private static readonly Color Dim = new Color(0.5f, 0.5f, 0.5f, 0.8f);

    private bool hammerModeActive;
    private bool hammerBusy;
    private bool swapModeActive;
    private GameObject selectedTile;
    private RectTransform hammerCursor;
    private ShootingManager2048 shootingManager;

    public static PowerUpsManager2048 Instance { get; private set; }

    public Sprite HammerSprite => destroyButtonImage != null ? destroyButtonImage.sprite : null;

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
            swapButton.onClick.AddListener(ToggleSwapMode);
        }

        UpdateButtonUI();
    }

    private void Update()
    {
        if (hammerCursor == null || !(hammerModeActive || swapModeActive)) return;

        var canvas = destroyButton.GetComponentInParent<Canvas>().rootCanvas;
        var cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)canvas.transform, Input.mousePosition, cam, out var local);
        hammerCursor.anchoredPosition = local;
    }

    public void ToggleDestroyMode()
    {
        if (hammerUsesRemaining <= 0)
        {
            Punch(destroyButton.transform, -12f);
            return;
        }

        hammerModeActive = !hammerModeActive;
        if (hammerModeActive) CancelSwap();
        UpdateButtonUI();
        if (!hammerModeActive) Punch(destroyButton.transform, 0f);
    }

    private static void Punch(Transform t, float shakeAngle)
    {
        t.DOComplete();
        if (shakeAngle == 0f) t.DOPunchScale(Vector3.one * 0.25f, 0.3f, 6, 0.6f);
        else t.DOPunchRotation(new Vector3(0, 0, shakeAngle), 0.3f, 12, 0.5f);
    }

    private void SetHammerCursor(bool on)
    {
        if (on && hammerCursor == null && HammerSprite != null)
        {
            var go = new GameObject("HammerCursor", typeof(RectTransform), typeof(Image));
            hammerCursor = (RectTransform)go.transform;
            hammerCursor.SetParent(destroyButton.GetComponentInParent<Canvas>().rootCanvas.transform, false);
            hammerCursor.sizeDelta = Vector2.one * 130f;
            hammerCursor.pivot = new Vector2(0.15f, 0.15f);
            var img = go.GetComponent<Image>();
            img.preserveAspect = true;
            img.raycastTarget = false;
        }

        if (hammerCursor == null) return;

        hammerCursor.GetComponent<Image>().sprite = swapModeActive ? swapButtonImage.sprite : HammerSprite;
        hammerCursor.pivot = swapModeActive ? new Vector2(0.5f, 0.5f) : new Vector2(0.15f, 0.15f);

        hammerCursor.DOKill();
        hammerCursor.gameObject.SetActive(on);
        if (on)
        {
            hammerCursor.localScale = Vector3.zero;
            hammerCursor.localRotation = Quaternion.Euler(0, 0, 20f);
            hammerCursor.DOScale(1f, 0.2f).SetEase(Ease.OutBack);
            hammerCursor.DORotate(new Vector3(0, 0, -10f), 0.5f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo);
        }
    }

    public void ToggleSwapMode()
    {
        if (swapUsesRemaining <= 0)
        {
            Punch(swapButton.transform, -12f);
            return;
        }

        if (swapModeActive) CancelSwap();
        else
        {
            swapModeActive = true;
            hammerModeActive = false;
        }

        UpdateButtonUI();
    }

    private void CancelSwap()
    {
        swapModeActive = false;
        Highlight(selectedTile, false);
        selectedTile = null;
    }

    private static void Highlight(GameObject tile, bool on)
    {
        if (tile == null) return;
        tile.transform.DOKill();
        tile.transform.localScale = Vector3.one;
        if (on) tile.transform.localScale = Vector3.one * 1.15f;
    }

    public bool IsDestroyModeActive()
    {
        return hammerModeActive;
    }

    public bool IsSwapModeActive()
    {
        return swapModeActive;
    }

    public bool InputLocked => hammerModeActive || hammerBusy || swapModeActive;

    public void HammerFinished()
    {
        hammerBusy = false;
    }

    public void TileDestroyed()
    {
        hammerBusy = true;
        hammerModeActive = false;
        hammerUsesRemaining--;
        UpdateButtonUI();
    }

    public void SetSelectedTile(GameObject tile)
    {
        if (selectedTile == null || selectedTile == tile)
        {
            Highlight(selectedTile, false);
            selectedTile = selectedTile == tile ? null : tile;
            Highlight(selectedTile, true);
            return;
        }

        var first = selectedTile;
        Highlight(first, false);
        selectedTile = tile;
        first.GetComponent<TileSwapHandler>().TrySwapWith(tile);
        if (selectedTile == tile) Highlight(tile, true);
    }

    public void ClearSelectedTile()
    {
        selectedTile = null;
    }

    public void TilesSwapped()
    {
        Highlight(selectedTile, false);
        CancelSwap();
        swapUsesRemaining--;
        UpdateButtonUI();
    }

    private void UpdateButtonUI()
    {
        SetHammerCursor(hammerModeActive || swapModeActive);

        if (destroyButtonImage != null)
        {
            destroyButtonImage.color = hammerUsesRemaining <= 0 ? Dim : hammerModeActive ? Active : Color.white;
            destroyButtonImage.transform.DOKill();
            destroyButtonImage.transform.localScale = Vector3.one;
            if (hammerModeActive) destroyButtonImage.transform.DOScale(1.12f, 0.45f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo);
        }

        if (swapButtonImage != null)
        {
            swapButtonImage.color = swapUsesRemaining <= 0 ? Dim : swapModeActive ? Active : Color.white;
        }

        if (destroyButtonText != null)
        {
            destroyButtonText.text = hammerUsesRemaining.ToString();
        }

        if (swapButtonText != null)
        {
            swapButtonText.text = swapUsesRemaining.ToString();
        }
    }
}
