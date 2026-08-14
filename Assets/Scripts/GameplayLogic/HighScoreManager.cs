using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public struct HighScoreEntry : IComparable<HighScoreEntry>
{
    public float Score;
    public string Name;

    public int CompareTo(HighScoreEntry other)
    {
        return Score.CompareTo(other.Score);
    }
}

[System.Serializable]
public class HighScoreEntryContainer
{
    public List<HighScoreEntry> highScoreEntries;

    public HighScoreEntryContainer(List<HighScoreEntry> entries)
    {
        highScoreEntries = entries;
    }
}

public static class HighScoreManager
{
    const string HIGH_SCORE_TABLE_KEY = "Scoreboard";

    public static List<HighScoreEntry> GetHighScores()
    {
        string ppEntry = PlayerPrefs.GetString(HIGH_SCORE_TABLE_KEY);
        if (ppEntry != "")
        {
            return JsonUtility.FromJson<HighScoreEntryContainer>(ppEntry).highScoreEntries;
        }

        return new List<HighScoreEntry>();
    }

    public static void SaveHighScore(HighScoreEntry entry)
    {
        List<HighScoreEntry> currentScores = GetHighScores();
        currentScores.Add(entry);
        currentScores.Sort();
        currentScores.Reverse();
        string newScores = JsonUtility.ToJson(new HighScoreEntryContainer(currentScores));
        PlayerPrefs.SetString(HIGH_SCORE_TABLE_KEY, newScores);
    }
}
