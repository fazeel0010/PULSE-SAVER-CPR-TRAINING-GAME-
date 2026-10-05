using System;
using System.IO;
using UnityEngine;

[Serializable]
public class RescueMateSaveData
{
    public int bestScore;
    public int lastScore;
    public int attempts;
    public int patientsSaved;
    public int failedRescues;
    public string lastResult = "No attempt yet";
    public string lastPlayed = "";
}

public static class RescueMateSaveManager
{
    private static RescueMateSaveData data;

    public static string SavePath => Path.Combine(Application.persistentDataPath, "rescuemate_scores.json");

    public static RescueMateSaveData Load()
    {
        if (data != null)
            return data;

        data = new RescueMateSaveData();

        try
        {
            if (File.Exists(SavePath))
            {
                string json = File.ReadAllText(SavePath);
                RescueMateSaveData loaded = JsonUtility.FromJson<RescueMateSaveData>(json);
                if (loaded != null)
                    data = loaded;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("RescueMate: score file could not be loaded. " + e.Message);
        }

        return data;
    }

    public static void RecordResult(int score, bool saved)
    {
        RescueMateSaveData d = Load();
        d.lastScore = score;
        d.bestScore = Mathf.Max(d.bestScore, score);
        d.attempts++;

        if (saved)
        {
            d.patientsSaved++;
            d.lastResult = "Patient Saved";
        }
        else
        {
            d.failedRescues++;
            d.lastResult = "Patient Died";
        }

        d.lastPlayed = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        Save();
    }

    public static void Reset()
    {
        data = new RescueMateSaveData();
        Save();
    }

    public static void Save()
    {
        try
        {
            File.WriteAllText(SavePath, JsonUtility.ToJson(Load(), true));
        }
        catch (Exception e)
        {
            Debug.LogWarning("RescueMate: score file could not be saved. " + e.Message);
        }
    }
}