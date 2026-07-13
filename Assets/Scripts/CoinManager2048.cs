using UnityEngine;
using TMPro;
using System;

public class CoinManager2048 : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI coinText;
    [SerializeField] private GameObject coinEarnedPrefab; // Optional animation prefab

    private int coins;
    private const string CoinsKey = "2048_Coins";

    // Event for UI updates
    public static event Action<int> OnCoinsChanged;

    // Singleton instance
    public static CoinManager2048 Instance { get; private set; }

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Load saved coins
        LoadCoins();
    }

    private void Start()
    {
        // Initial display update
        UpdateCoinDisplay();
    }

    public void AddCoins(int amount)
    {
        if (amount <= 0) return;

        coins += amount;
        UpdateCoinDisplay();
        SaveCoins();

        // Trigger event
        OnCoinsChanged?.Invoke(coins);

        // Optional: Show animation
        DisplayCoinEarned(amount);

        Debug.Log($"Added {amount} coins. Total: {coins}");
    }

    public bool SpendCoins(int amount)
    {
        if (amount <= 0) return true;

        // Check if player has enough coins
        if (coins < amount)
        {
            Debug.Log($"Not enough coins! Have {coins}, need {amount}");
            return false;
        }

        // Deduct coins
        coins -= amount;
        UpdateCoinDisplay();
        SaveCoins();

        // Trigger event
        OnCoinsChanged?.Invoke(coins);

        Debug.Log($"Spent {amount} coins. Remaining: {coins}");
        return true;
    }

    public int GetCoins()
    {
        return coins;
    }

    private void LoadCoins()
    {
        coins = PlayerPrefs.GetInt(CoinsKey, 0);
    }

    private void SaveCoins()
    {
        PlayerPrefs.SetInt(CoinsKey, coins);
        PlayerPrefs.Save();
    }

    private void UpdateCoinDisplay()
    {
        if (coinText != null)
        {
            coinText.text = coins.ToString();
        }
    }

    private void DisplayCoinEarned(int amount)
    {
        if (coinEarnedPrefab != null && coinText != null)
        {
            GameObject coinPopup = Instantiate(coinEarnedPrefab, coinText.transform.parent);
            CoinPopup popup = coinPopup.GetComponent<CoinPopup>();

            if (popup != null)
            {
                popup.Initialize(amount);
            }
        }
    }

    // Add coins based on merged value
    public void AddCoinsFromMerge(int mergedValue, int combo = 1)
    {
        // Simple formula: log2(value) with combo multiplier
        int baseCoins = Mathf.Max(1, Mathf.FloorToInt(Mathf.Log(mergedValue, 2)));
        int comboBonus = combo > 1 ? combo / 2 : 0;
        int coinsToAdd = baseCoins + comboBonus;

        AddCoins(coinsToAdd);
    }
}
