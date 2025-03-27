using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text;
using System;

public class ScoreManager : MonoBehaviour
{
    public int currentScore;

    [SerializeField] private TMP_Text scoreText;  // UI de pontuação

    [Header("Pontuação")]
    public int pointsPerMatch = 10; // Pontos por match de 3 gemas
    public int pointsPerCombo = 20; // Pontos extras por combo

    public event Action<int> OnScoreUpdated; // Evento para avisar outros sistemas

    private StringBuilder scoreStringBuilder = new StringBuilder(20); // Para otimizar texto

    void Start()
    {
        currentScore = 0;
        UpdateScoreUI();
    }

    public void AddPoints(int points)
    {
        currentScore += points;
        UpdateScoreUI();
    }

    public void AddComboPoints(int comboCount)
    {
        int comboBonus = pointsPerCombo * (comboCount - 1);
        currentScore += pointsPerMatch + comboBonus;
        UpdateScoreUI();
    }

    public void ResetScore()
    {
        currentScore = 0;
        UpdateScoreUI();
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreStringBuilder.Clear();
            scoreStringBuilder.Append("Pontuação: ").Append(currentScore);
            scoreText.text = scoreStringBuilder.ToString();
        }

        OnScoreUpdated?.Invoke(currentScore);
    }

    public int GetScore() => currentScore;
}
