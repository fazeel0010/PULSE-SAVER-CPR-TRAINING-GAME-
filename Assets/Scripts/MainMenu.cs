using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RescueMateMainMenu : MonoBehaviour
{
    [SerializeField] private GameObject howToPanel;
    [SerializeField] private GameObject scoresPanel;
    [SerializeField] private Text bestScoreText;
    [SerializeField] private Text scoreStatsText;

    private void Start()
    {
        if (howToPanel != null)
            howToPanel.SetActive(false);

        if (scoresPanel != null)
            scoresPanel.SetActive(false);

        RefreshScores();
    }

    public void StartTraining()
    {
        SceneManager.LoadScene("CPRGame");
    }

    public void OpenHowTo()
    {
        if (howToPanel != null)
            howToPanel.SetActive(true);
    }

    public void CloseHowTo()
    {
        if (howToPanel != null)
            howToPanel.SetActive(false);
    }

    public void OpenScores()
    {
        RefreshScores();

        if (scoresPanel != null)
            scoresPanel.SetActive(true);
    }

    public void CloseScores()
    {
        if (scoresPanel != null)
            scoresPanel.SetActive(false);
    }

    public void ResetScores()
    {
        RescueMateSaveManager.Reset();
        RefreshScores();
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void RefreshScores()
    {
        RescueMateSaveData d = RescueMateSaveManager.Load();

        if (bestScoreText != null)
            bestScoreText.text = "BEST SCORE  " + d.bestScore + " / 100";

        if (scoreStatsText != null)
        {
            scoreStatsText.text =
                "Last Score: " + d.lastScore + " / 100\n" +
                "Attempts: " + d.attempts + "\n" +
                "Patients Saved: " + d.patientsSaved + "\n" +
                "Failed Rescues: " + d.failedRescues + "\n" +
                "Last Result: " + d.lastResult +
                (string.IsNullOrEmpty(d.lastPlayed) ? "" : "\nLast Played: " + d.lastPlayed);
        }
    }
}