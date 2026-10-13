using UnityEngine;
using TMPro;
using DG.Tweening;

public class ScoreManager2048 : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI bestScoreText; // Reference to best score text element

    private int currentScore = 0;
    private int bestScore = 0;
    private const string BestScoreKey = "2048_BestScore"; // Key for PlayerPrefs
    private const string BestBlockKey = "2048_BestBlock"; // Key for PlayerPrefs, tracks the highest tile value ever reached

    private void Start()
    {
        LoadBestScore();
        AnimatedNumberText.SetImmediate(scoreText, currentScore);
        AnimatedNumberText.SetImmediate(bestScoreText, bestScore);
    }

    public void AddScore(int points)
    {
        currentScore += points;
        UpdateBestBlock(points);

        // Animate score count-up (0 -> target on first gain)
        AnimatedNumberText.Animate(scoreText, currentScore);

        // Check if current score is a new best
        if (currentScore > bestScore)
        {
            bestScore = currentScore;
            AnimatedNumberText.Animate(bestScoreText, bestScore);
            SaveBestScore();
        }
    }

    private void LoadBestScore()
    {
        bestScore = PlayerPrefs.GetInt(BestScoreKey, 0);
    }

    private void SaveBestScore()
    {
        PlayerPrefs.SetInt(BestScoreKey, bestScore);
        PlayerPrefs.Save();
    }

    public int GetScore()
    {
        return currentScore;
    }

    public int GetBestScore()
    {
        return bestScore;
    }

    /// <summary>
    /// Compares the given tile value against the saved best block and persists it via
    /// PlayerPrefs if it is a new record. Call this when a run ends (e.g. on game over).
    /// </summary>
    public void UpdateBestBlock(int highestTileValue)
    {
        if (highestTileValue <= GetSavedBestBlock())
            return;

        PlayerPrefs.SetInt(BestBlockKey, highestTileValue);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Reads the all-time best score from local save without requiring a ScoreManager2048 instance.
    /// Used by menus that need to display the record before gameplay starts.
    /// </summary>
    public static int GetSavedBestScore()
    {
        return PlayerPrefs.GetInt(BestScoreKey, 0);
    }

    /// <summary>
    /// Reads the all-time highest block value reached from local save without requiring a
    /// ScoreManager2048 instance. Used by menus that need to display the record before gameplay starts.
    /// </summary>
    public static int GetSavedBestBlock()
    {
        return PlayerPrefs.GetInt(BestBlockKey, 0);
    }

    public void SetScoreForUndo(int score)
    {
        currentScore = score;
        AnimatedNumberText.SetImmediate(scoreText, currentScore);
    }


    // Reset score to zero (optional method for game reset)
    public void ResetScore()
    {
        currentScore = 0;
        AnimatedNumberText.SetImmediate(scoreText, currentScore);
    }

    // Reset best score (could be used in settings menu)
    public void ResetBestScore()
    {
        bestScore = 0;
        AnimatedNumberText.SetImmediate(bestScoreText, bestScore);
        SaveBestScore();
    }
}
