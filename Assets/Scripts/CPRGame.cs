using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RescueMateCPRGame : MonoBehaviour
{
    [Header("CPR Rules")]
    [SerializeField] private int totalCompressions = 30;
    [SerializeField] private float minBpm = 100f;
    [SerializeField] private float maxBpm = 120f;
    [Range(0.5f, 1f)]
    [SerializeField] private float requiredAccuracy = 0.80f;

    [Header("Patient Health")]
    [SerializeField] private int startingHealth = 60;
    [SerializeField] private int correctHealthGain = 2;
    [SerializeField] private int wrongHealthLoss = 5;

    [Header("Scene References")]
    [SerializeField] private Transform torso;
    [SerializeField] private Transform chestTarget;
    [SerializeField] private Renderer[] patientRenderers;
    [SerializeField] private Material skinMaterial;
    [SerializeField] private Material shirtMaterial;
    [SerializeField] private Material pantsMaterial;
    [SerializeField] private Material targetMaterial;

    [Header("HUD References")]
    [SerializeField] private Text statusText;
    [SerializeField] private Text scoreText;
    [SerializeField] private Text streakText;
    [SerializeField] private Text bpmText;
    [SerializeField] private Text feedbackText;
    [SerializeField] private Text healthText;
    [SerializeField] private Text progressText;
    [SerializeField] private Text comboText;
    [SerializeField] private Image bpmIndicator;
    [SerializeField] private Image healthFill;
    [SerializeField] private Image progressFill;

    [Header("Controls")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button compressButton;
    [SerializeField] private Text countdownText;

    [Header("Result Panel")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private Text resultTitle;
    [SerializeField] private Text resultStars;
    [SerializeField] private Text resultScore;
    [SerializeField] private Text resultDetails;

    private int compressionCount;
    private int correctCompressions;
    private int health;
    private int score;
    private int streak;
    private int bestStreak;

    private float lastCompressionTime = -1f;
    private bool missionStarted;
    private bool acceptingInput;
    private bool missionFinished;

    private Vector3 torsoBaseScale;
    private Vector3 torsoBasePosition;
    private Vector3 targetBaseScale;
    private float compressionAnimationTimer;

    private const float CompressionAnimationDuration = 0.18f;

    private static readonly Color Good = new Color(0.10f, 0.78f, 0.42f);
    private static readonly Color Bad = new Color(0.88f, 0.10f, 0.16f);
    private static readonly Color Warn = new Color(1.00f, 0.72f, 0.12f);
    private static readonly Color White = new Color(0.97f, 0.98f, 1.00f);

    private void Start()
    {
        if (torso != null)
        {
            torsoBaseScale = torso.localScale;
            torsoBasePosition = torso.localPosition;
        }

        if (chestTarget != null)
            targetBaseScale = chestTarget.localScale;

        ResetMission();
    }

    private void Update()
    {
        AnimatePatient();

        if (missionStarted && acceptingInput && !missionFinished && Input.GetKeyDown(KeyCode.Space))
            PerformCompression();
    }

    public void StartMission()
    {
        if (missionStarted)
            return;

        compressionCount = 0;
        correctCompressions = 0;
        health = startingHealth;
        score = 0;
        streak = 0;
        bestStreak = 0;
        lastCompressionTime = -1f;

        missionStarted = true;
        acceptingInput = false;
        missionFinished = false;

        if (resultPanel != null)
            resultPanel.SetActive(false);

        startButton.gameObject.SetActive(false);
        compressButton.gameObject.SetActive(true);
        compressButton.interactable = false;

        statusText.text = "GET READY";
        statusText.color = Warn;
        feedbackText.text = "Position your hands over the chest.";
        feedbackText.color = White;

        RestorePatientMaterials();
        UpdateHUD();
        StartCoroutine(Countdown());
    }

    private IEnumerator Countdown()
    {
        countdownText.gameObject.SetActive(true);

        string[] sequence = { "3", "2", "1", "GO!" };
        for (int i = 0; i < sequence.Length; i++)
        {
            countdownText.text = sequence[i];
            countdownText.color = i == sequence.Length - 1 ? Good : White;
            countdownText.rectTransform.localScale = Vector3.one * 1.20f;

            float timer = 0f;
            while (timer < 0.65f)
            {
                timer += Time.deltaTime;
                float t = Mathf.Clamp01(timer / 0.65f);
                countdownText.rectTransform.localScale =
                    Vector3.one * Mathf.Lerp(1.20f, 0.92f, t);
                yield return null;
            }
        }

        countdownText.gameObject.SetActive(false);
        acceptingInput = true;
        compressButton.interactable = true;

        statusText.text = "RESCUE IN PROGRESS";
        statusText.color = White;
        feedbackText.text = "Press SPACE or click PUMP CHEST.";
    }

    public void PerformCompression()
    {
        if (!missionStarted || !acceptingInput || missionFinished)
            return;

        compressionCount++;
        compressionAnimationTimer = CompressionAnimationDuration;

        if (lastCompressionTime < 0f)
        {
            feedbackText.text = "First compression registered. Find the rhythm.";
            feedbackText.color = White;
            bpmText.text = "BPM  --";
            bpmIndicator.color = Warn;
        }
        else
        {
            float interval = Time.time - lastCompressionTime;
            float bpm = 60f / Mathf.Max(interval, 0.01f);
            bool correct = bpm >= minBpm && bpm <= maxBpm;

            bpmText.text = "BPM  " + Mathf.RoundToInt(bpm);

            if (correct)
            {
                correctCompressions++;
                streak++;
                bestStreak = Mathf.Max(bestStreak, streak);
                health = Mathf.Clamp(health + correctHealthGain, 0, 100);

                feedbackText.text = "PERFECT RHYTHM";
                feedbackText.color = Good;
                bpmIndicator.color = Good;
                statusText.text = "PATIENT STABILIZING";
                statusText.color = Good;
            }
            else
            {
                streak = 0;
                health = Mathf.Clamp(health - wrongHealthLoss, 0, 100);

                feedbackText.text = bpm > maxBpm
                    ? "TOO FAST - SLOW DOWN"
                    : "TOO SLOW - SPEED UP";

                feedbackText.color = Bad;
                bpmIndicator.color = Bad;
                statusText.text = "PATIENT CRITICAL";
                statusText.color = Bad;
            }
        }

        lastCompressionTime = Time.time;
        CalculateLiveScore();
        UpdateHUD();

        if (health <= 0)
        {
            FinishMission(false);
            return;
        }

        if (compressionCount >= totalCompressions)
        {
            int measured = Mathf.Max(1, totalCompressions - 1);
            float accuracy = correctCompressions / (float)measured;
            bool saved = accuracy >= requiredAccuracy && health >= 35;
            FinishMission(saved);
        }
    }

    private void CalculateLiveScore()
    {
        int measured = Mathf.Max(1, compressionCount - 1);
        float accuracy = correctCompressions / (float)measured;
        float healthPart = health / 100f;
        float progressPart = compressionCount / (float)totalCompressions;

        score = Mathf.Clamp(
            Mathf.RoundToInt(
                accuracy * 70f +
                healthPart * 20f +
                progressPart * 10f),
            0,
            100);
    }

    private void FinishMission(bool saved)
    {
        missionFinished = true;
        acceptingInput = false;
        compressButton.interactable = false;
        compressButton.gameObject.SetActive(false);

        int measured = Mathf.Max(1, totalCompressions - 1);
        int accuracyPercent = Mathf.RoundToInt(
            Mathf.Clamp01(correctCompressions / (float)measured) * 100f);

        int finalScore = Mathf.Clamp(
            Mathf.RoundToInt(
                accuracyPercent * 0.72f +
                health * 0.20f +
                Mathf.Min(bestStreak, 10) * 0.8f),
            0,
            100);

        score = finalScore;
        UpdateHUD();

        RescueMateSaveManager.RecordResult(finalScore, saved);

        resultPanel.SetActive(true);

        if (saved)
        {
            resultTitle.text = "PATIENT SAVED";
            resultTitle.color = Good;
            statusText.text = "MISSION SUCCESS";
            statusText.color = Good;
            feedbackText.text = "You stabilized the patient.";
            feedbackText.color = Good;
            TintPatient(Good, 0.28f);
        }
        else
        {
            resultTitle.text = "PATIENT DIED";
            resultTitle.color = Bad;
            statusText.text = "MISSION FAILED";
            statusText.color = Bad;
            feedbackText.text = "The CPR rhythm was not accurate enough.";
            feedbackText.color = Bad;
            TintPatient(Bad, 0.28f);
        }

        resultStars.text = GetStars(finalScore);
        resultScore.text = "FINAL SCORE  " + finalScore + " / 100";
        resultDetails.text =
            "Accuracy: " + accuracyPercent + "%\n" +
            "Patient Health: " + health + "%\n" +
            "Best Streak: x" + bestStreak + "\n" +
            "Correct Rhythm: " + correctCompressions + " / " + measured;
    }

    public void RetryMission()
    {
        ResetMission();
    }

    public void BackToMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    private void ResetMission()
    {
        StopAllCoroutines();

        compressionCount = 0;
        correctCompressions = 0;
        health = startingHealth;
        score = 0;
        streak = 0;
        bestStreak = 0;
        lastCompressionTime = -1f;
        missionStarted = false;
        acceptingInput = false;
        missionFinished = false;

        if (resultPanel != null)
            resultPanel.SetActive(false);

        if (countdownText != null)
            countdownText.gameObject.SetActive(false);

        startButton.gameObject.SetActive(true);
        compressButton.gameObject.SetActive(false);
        compressButton.interactable = false;

        statusText.text = "READY";
        statusText.color = White;
        feedbackText.text = "Press START MISSION when you are ready.";
        feedbackText.color = White;
        bpmText.text = "BPM  --";
        bpmIndicator.color = Warn;

        RestorePatientMaterials();
        UpdateHUD();
    }

    private void UpdateHUD()
    {
        scoreText.text = "SCORE  " + score;
        streakText.text = "STREAK  x" + streak;
        healthText.text = "PATIENT HEALTH  " + health + "%";
        progressText.text = "CPR PROGRESS  " + compressionCount + " / " + totalCompressions;
        comboText.text = bestStreak >= 3 ? "COMBO x" + bestStreak : "";

        healthFill.fillAmount = Mathf.Clamp01(health / 100f);
        progressFill.fillAmount = Mathf.Clamp01(compressionCount / (float)totalCompressions);

        if (health >= 70)
            healthFill.color = Good;
        else if (health >= 40)
            healthFill.color = Warn;
        else
            healthFill.color = Bad;
    }

    private void AnimatePatient()
    {
        if (torso != null)
        {
            if (compressionAnimationTimer > 0f)
            {
                compressionAnimationTimer -= Time.deltaTime;
                float t = 1f - Mathf.Clamp01(
                    compressionAnimationTimer / CompressionAnimationDuration);
                float press = Mathf.Sin(t * Mathf.PI);

                Vector3 scale = torsoBaseScale;
                scale.y *= Mathf.Lerp(1f, 0.66f, press);
                torso.localScale = scale;

                Vector3 pos = torsoBasePosition;
                pos.y -= 0.05f * press;
                torso.localPosition = pos;
            }
            else
            {
                torso.localScale = torsoBaseScale;
                torso.localPosition = torsoBasePosition;
            }
        }

        if (chestTarget != null)
        {
            float pulse = 1f + Mathf.Sin(Time.time * 5f) * 0.05f;
            chestTarget.localScale = targetBaseScale * pulse;
        }
    }

    private string GetStars(int value)
    {
        if (value >= 90) return "★★★";
        if (value >= 70) return "★★☆";
        if (value >= 50) return "★☆☆";
        return "☆☆☆";
    }

    private void TintPatient(Color tint, float amount)
    {
        if (patientRenderers == null)
            return;

        foreach (Renderer r in patientRenderers)
        {
            if (r == null || r.transform == chestTarget)
                continue;

            Material m = new Material(r.sharedMaterial);
            m.color = Color.Lerp(m.color, tint, amount);
            r.material = m;
        }
    }

    private void RestorePatientMaterials()
    {
        if (patientRenderers == null)
            return;

        foreach (Renderer r in patientRenderers)
        {
            if (r == null)
                continue;

            string n = r.gameObject.name;

            if (n == "Torso")
                r.sharedMaterial = shirtMaterial;
            else if (n == "Pelvis" || n.Contains("Leg"))
                r.sharedMaterial = pantsMaterial;
            else if (n == "ChestTarget")
                r.sharedMaterial = targetMaterial;
            else
                r.sharedMaterial = skinMaterial;
        }
    }
}