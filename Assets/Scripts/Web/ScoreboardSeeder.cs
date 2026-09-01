using UnityEngine;

public static class ScoreboardSeeder
{
    public static void SeedIfEmpty()
    {
        if (HighScoreManager.GetHighScores().Count > 0) return;

        var seed = new (string name, float score)[]
        {
            ("Bosman", 742), ("sztorm_91", 688), ("Mira", 654), ("KubaW", 601),
            ("Anka", 566), ("dzwigowy", 531), ("Heniek", 498), ("Majtek", 455),
            ("Ola_P", 407), ("spawacz", 371), ("Rdza", 322), ("Fala", 280),
            ("mlody", 233), ("Gapa", 187)
        };

        foreach (var (name, score) in seed)
            HighScoreManager.SaveHighScore(new HighScoreEntry { Name = name, Score = score });

        Debug.Log("ScoreboardSeeder: seeded scoreboard with sample entries");
    }
}
