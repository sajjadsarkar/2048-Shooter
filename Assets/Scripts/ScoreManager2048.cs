using UnityEngine;
using TMPro;

public class ScoreManager2048 : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI bestScoreText; // Reference to best score text element

    private int currentScore = 0;
    private int bestScore = 0;
    private const string BestScoreKey = "2048_BestScore"; // Key for PlayerPrefs

    private void Start()
    {
        LoadBestScore();
        UpdateScoreDisplay();
        UpdateBestScoreDisplay();
    }

    public void AddScore(int points)
    {
        currentScore += points;
        UpdateScoreDisplay();

        // Check if current score is a new best
        if (currentScore > bestScore)
        {
            bestScore = currentScore;
            UpdateBestScoreDisplay();
            SaveBestScore();
        }

        // Log the score change (optional, for debugging)
        Debug.Log($"Score increased by {points}. New score: {currentScore}");
    }

    private void UpdateScoreDisplay()
    {
        if (scoreText != null)
        {
            // Use NumberFormatter to format the score for better readability
            scoreText.text = NumberFormatter.FormatNumber(currentScore);
        }
    }

    private void UpdateBestScoreDisplay()
    {
        if (bestScoreText != null)
        {
            bestScoreText.text = NumberFormatter.FormatNumber(bestScore);
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

    // Reset score to zero (optional method for game reset)
    public void ResetScore()
    {
        currentScore = 0;
        UpdateScoreDisplay();
    }

    // Reset best score (could be used in settings menu)
    public void ResetBestScore()
    {
        bestScore = 0;
        UpdateBestScoreDisplay();
        SaveBestScore();
    }
}
