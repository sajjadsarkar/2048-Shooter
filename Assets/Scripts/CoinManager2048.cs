using UnityEngine;
using TMPro;
using DG.Tweening;
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

        // Initialize coins to 0 (game-based)
        coins = 0;
    }

    private void Start()
    {
        // Initial display update
        AnimatedNumberText.SetImmediate(coinText, coins);
    }

    public void ResetCoins()
    {
        coins = 0;
        AnimatedNumberText.SetImmediate(coinText, coins);
        OnCoinsChanged?.Invoke(coins);
        Debug.Log("Coins reset to 0 for a new game.");
    }

    public void AddCoins(int amount)
    {
        if (amount <= 0) return;

        coins += amount;
        AnimatedNumberText.Animate(coinText, coins);

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
        AnimatedNumberText.Animate(coinText, coins);

        // Trigger event
        OnCoinsChanged?.Invoke(coins);

        Debug.Log($"Spent {amount} coins. Remaining: {coins}");
        return true;
    }

    public int GetCoins()
    {
        return coins;
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
        // Only award coins for merges of 64 or higher to keep average coins per game around 20-30
        if (mergedValue < 64) return;

        // Scale down: log2(64) is 6, so subtract 5 to get 1 coin. 128 -> 2 coins, etc.
        int baseCoins = Mathf.Max(1, Mathf.FloorToInt(Mathf.Log(mergedValue, 2)) - 5);
        int comboBonus = combo > 2 ? 1 : 0; // Minimal combo bonus
        int coinsToAdd = baseCoins + comboBonus;

        AddCoins(coinsToAdd);
    }
}
