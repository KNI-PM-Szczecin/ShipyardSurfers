using System;
using UnityEngine;

[Serializable]
public class ScoreSnapshot
{
    public bool hasCurrent;
    public float current;
    public string[] names;
    public float[] scores;

    public static string CaptureJson()
    {
        var snapshot = new ScoreSnapshot();

        if (GameManager.instance != null)
        {
            snapshot.hasCurrent = true;
            snapshot.current = GameManager.instance.Score.Value;
        }

        var entries = HighScoreManager.GetHighScores();
        snapshot.names = new string[entries.Count];
        snapshot.scores = new float[entries.Count];
        for (int i = 0; i < entries.Count; i++)
        {
            snapshot.names[i] = entries[i].Name;
            snapshot.scores[i] = entries[i].Score;
        }

        return JsonUtility.ToJson(snapshot);
    }
}
