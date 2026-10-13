using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class Tile2048 : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI numberText;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image strokeImage;
    [SerializeField] private bool enableLandingBounce = true;

    public bool EnableLandingBounce { get => enableLandingBounce; set => enableLandingBounce = value; }

    private int value;

    // Monotonically increasing counter so we can always pick the newest
    // tile in a connected group as the merge center (deterministic, no conflicts).
    private static int globalCreationCounter = 0;
    public int CreationOrder { get; private set; }

    // Tile (fill) colors for powers of 2 -> index = log2(value) - 1
    private static readonly Color[] tileColors = new Color[11];
    // Matching stroke colors for each power of 2
    private static readonly Color[] strokeColors = new Color[11];

    static Tile2048()
    {
        int[] t =
        {
            0x2796FD, 0xFF9A36, 0xFD6280, 0x19C8A6, 0xA044FC, 0xFDC713,
            0x05D4FF, 0xFC4C4F, 0x73D91C, 0xFC4093, 0xFDB71D
        };
        for (int i = 0; i < 11; i++)
        {
            tileColors[i] = Hex(t[i]);
            strokeColors[i] = Color.Lerp(tileColors[i], Color.black, 0.12f);
        }
    }

    private static Color Hex(int hex)
    {
        return new Color(
            ((hex >> 16) & 0xFF) / 255f,
            ((hex >> 8) & 0xFF) / 255f,
            (hex & 0xFF) / 255f);
    }

    // Predefined tile colors for multiples of 3
    private static readonly Color[] multipleOfThreeColors = new Color[]
    {
        new Color(0.95f, 0.7f, 0.47f),     // 3: Soft Orange
        new Color(0.96f, 0.58f, 0.39f),    // 6: Coral
        new Color(0.96f, 0.49f, 0.37f),    // 12: Reddish Orange
        new Color(0.96f, 0.37f, 0.23f),    // 24: Red
        new Color(0.78f, 0.62f, 0.15f),    // 48: Strong Yellow
        new Color(0.78f, 0.56f, 0.12f),    // 96: Deeper Yellow
        new Color(0.78f, 0.48f, 0.10f),    // 192: Gold
        new Color(0.78f, 0.42f, 0.08f),    // 384: Dark Gold
        new Color(0.78f, 0.35f, 0.05f),    // 768: Deep Gold
    };

    public int Value
    {
        get => value;
        set { this.value = value; UpdateTileAppearance(); }
    }

    private void Awake()
    {
        CreationOrder = globalCreationCounter++;

        if (!numberText) numberText = GetComponentInChildren<TextMeshProUGUI>();
        if (!backgroundImage) backgroundImage = GetComponent<Image>();
        if (!strokeImage)
        {
            // Stroke is a child Image (other than the tile's own background)
            foreach (var img in GetComponentsInChildren<Image>())
            {
                if (img != backgroundImage) { strokeImage = img; break; }
            }
        }
    }

    public void Initialize(int startValue = 2) => Value = startValue;

    private void UpdateTileAppearance()
    {
        numberText.text = value.ToString();
        (Color tile, Color stroke) = GetTileColors(value);
        backgroundImage.color = tile;
        if (strokeImage) strokeImage.color = stroke;
        numberText.color = Color.white;
    }

    private (Color tile, Color stroke) GetTileColors(int value)
    {
        // Power of 2 -> use the exact tile + stroke pair
        if (IsPowerOfTwo(value))
        {
            int index = Mathf.FloorToInt(Mathf.Log(value, 2)) - 1;
            if (index < tileColors.Length) return (tileColors[index], strokeColors[index]);
        }

        // Multiple of 3 -> use predefined tile color, derive a similar stroke
        if (value % 3 == 0)
        {
            int index = Mathf.FloorToInt(Mathf.Log(value / 3, 2)) - 1;
            if (index < multipleOfThreeColors.Length)
            {
                Color tile = multipleOfThreeColors[index];
                return (tile, DeriveStroke(tile));
            }
        }

        // Anything else -> generate a unique tile + slightly different stroke
        return GenerateColorPair(value);
    }

    private bool IsPowerOfTwo(int value)
    {
        return (value != 0) && ((value & (value - 1)) == 0);
    }

    // Build a stroke color that is similar to the tile but slightly shifted
    private Color DeriveStroke(Color tile)
    {
        Color.RGBToHSV(tile, out float h, out float s, out float v);
        float strokeH = (h + 0.04f) % 1f;
        float strokeS = Mathf.Clamp01(s + 0.10f);
        float strokeV = Mathf.Clamp01(v - 0.10f);
        return Color.HSVToRGB(strokeH, strokeS, strokeV);
    }

    private (Color tile, Color stroke) GenerateColorPair(int value)
    {
        float hue = (value * 0.0015f) % 1.0f;
        float saturation = 0.85f;
        float brightness = 0.9f - (value * 0.0005f);
        Color tile = Color.HSVToRGB(hue, saturation, brightness);
        return (tile, DeriveStroke(tile));
    }

    public void PlayLandingBounce()
    {
        if (!enableLandingBounce) return;

        RectTransform rt = GetComponent<RectTransform>();
        if (rt != null)
        {
            DOTween.Kill(rt);
            rt.localScale = Vector3.one;

            Sequence seq = DOTween.Sequence();
            // Soft Squash (Y squashes down, X expands slightly for a squishy/soft feel)
            seq.Append(rt.DOScale(new Vector3(1.05f, 0.92f, 1f), 0.06f).SetEase(Ease.OutSine));
            // Soft Stretch (Y stretches up, X shrinks slightly)
            seq.Append(rt.DOScale(new Vector3(0.97f, 1.04f, 1f), 0.06f).SetEase(Ease.InOutSine));
            // Smooth Settle
            seq.Append(rt.DOScale(Vector3.one, 0.08f).SetEase(Ease.OutSine));
            seq.SetTarget(rt);
            seq.Play();
        }
    }
}