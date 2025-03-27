using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TimerManager : MonoBehaviour
{
    [SerializeField] private float startTime = 60f; // Tempo inicial em segundos
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text finalScore;
    [SerializeField] private GameObject finalPanel;
    [SerializeField] private GameObject match3Panel;
    private ScoreManager scoreManager;

    private float currentTime;
    private bool isRunning = false;

    private void Awake()
    {
        scoreManager = FindAnyObjectByType<ScoreManager>();
    }
    void Start()
    {
        ResetTimer();
        StartTimer();
    }

    void Update()
    {
        if (isRunning && currentTime > 0)
        {
            currentTime -= Time.deltaTime;
            UpdateTimerUI();

            if (currentTime <= 0)
            {
                currentTime = 0;
                EndTimer();
            }
        }
    }

    private void UpdateTimerUI()
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(currentTime / 60);
            int seconds = Mathf.FloorToInt(currentTime % 60);
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }
    }

    private void EndTimer()
    {
        isRunning = false;
        finalPanel.SetActive(true);
        match3Panel.SetActive(false);
        finalScore.text = "Pontuação: " + scoreManager.currentScore.ToString();
        timerText.text = null;
    }

    public void StartTimer()
    {
        isRunning = true;
    }

    public void PauseTimer()
    {
        isRunning = false;
    }

    public void ResetTimer()
    {
        currentTime = startTime;
        UpdateTimerUI();
        finalPanel.SetActive(false); // Garante que o painel esteja desativado
        isRunning = false;
    }
}
