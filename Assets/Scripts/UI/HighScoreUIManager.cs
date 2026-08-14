using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class HighScoreUIManager : MonoBehaviour
{
    public GameObject HighScorePrefab;
    public Transform HighScorePanel;

    private void Awake()
    {
        List<HighScoreEntry> highScoreList = HighScoreManager.GetHighScores();
        print(highScoreList);
        generateScoreboard(highScoreList);
    }

    private void generateScoreboard(List<HighScoreEntry> highScoreList)
    {
        for (int i = 0; i < 10; i++)
        {
            if (i < highScoreList.Count)
            {
                generateSegment(highScoreList[i], i);
            }
            else
            {
                generateSegment(new HighScoreEntry { Name = "---", Score = 0 }, i);
            }
        }
    }

    private void generateSegment(HighScoreEntry entry, int position)
    {
        GameObject newEntry = Instantiate(HighScorePrefab, HighScorePanel);
        newEntry.transform.Find("Score").GetComponent<TMP_Text>().text = Mathf.Round(entry.Score).ToString();
        newEntry.transform.Find("Name").GetComponent<TMP_Text>().text = entry.Name;
        newEntry.transform.Find("Position").GetComponent<TMP_Text>().text = (position+1).ToString() + ".";
    }
}
