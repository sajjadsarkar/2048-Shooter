using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Tile2048 : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI numberText;
    [SerializeField] private Image backgroundImage;

    private int value;

    // Predefined colors for powers of 2
    private static readonly Color[] powerOfTwoColors = new Color[]
    {
        new Color(0f, 0.505f, 0.71f),     // 2: #0081b5
        new Color(0.98f, 0.498f, 0.255f),  // 4: #fa7f41
        new Color(0.894f, 0.322f, 0.533f), // 8: #e45288
        new Color(0.72f, 0.168f, 0.97f),   // 16: #b82bf8
        new Color(0.635f, 0.306f, 0.47f),  // 32: #a24e78
        new Color(0.734f, 0.77f, 0.024f),  // 64: #bbc406
        new Color(0.988f, 0.259f, 0.388f), // 128: #fc4263
        new Color(0.55f, 0.15f, 0.85f),    // 256: Violet
        new Color(0.20f, 0.60f, 0.85f),    // 512: Cyan Blue
        new Color(0.98f, 0.72f, 0.20f),    // 1024: Gold
        new Color(0.85f, 0.20f, 0.72f),    // 2048: Magenta
    };

    // Predefined colors for multiples of 3
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
        if (!numberText) numberText = GetComponentInChildren<TextMeshProUGUI>();
        if (!backgroundImage) backgroundImage = GetComponent<Image>();
    }

    public void Initialize(int startValue = 2) => Value = startValue;

    private void UpdateTileAppearance()
    {
        numberText.text = value.ToString();
        backgroundImage.color = GetTileColor(value);
        numberText.color = GetReadableTextColor(backgroundImage.color);
    }

    private Color GetTileColor(int value)
    {
        // Check if the value is a power of 2
        if (IsPowerOfTwo(value))
        {
            int index = Mathf.FloorToInt(Mathf.Log(value, 2)) - 1;
            if (index < powerOfTwoColors.Length) return powerOfTwoColors[index];
        }

        // Check if the value is a multiple of 3
        if (value % 3 == 0)
        {
            int index = Mathf.FloorToInt(Mathf.Log(value / 3, 2)) - 1;
            if (index < multipleOfThreeColors.Length) return multipleOfThreeColors[index];
        }

        // For all other numbers, generate a unique color
        return GenerateUniqueColor(value);
    }

    private bool IsPowerOfTwo(int value)
    {
        return (value != 0) && ((value & (value - 1)) == 0);
    }

    private Color GenerateUniqueColor(int value)
    {
        // Use value to calculate a unique hue
        float hue = (value * 0.0015f) % 1.0f; // Adjust multiplier for finer hue shifts
        float saturation = 0.85f; // High saturation for vibrant colors
        float brightness = 0.9f - (value * 0.0005f); // Slightly reduce brightness for higher values

        return Color.HSVToRGB(hue, saturation, brightness);
    }

    private Color GetReadableTextColor(Color bg)
    {
        return (bg.r * 0.299f + bg.g * 0.587f + bg.b * 0.114f) > 0.6f
            ? Color.black : Color.white;
    }
}