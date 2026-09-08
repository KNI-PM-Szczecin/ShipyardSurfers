using System;
using TMPro;
using UnityEngine;

public class GameUIManager : MonoBehaviour
{
    [Header("Gameplay")]
    public TMP_Text ScoreText;
    [Space]
    [Header("EndScreen")]
    public GameObject EndScreen;
    public GameObject GameplayPanel;

    void Start()
    {
        if (ScoreText != null)
        {
            ScoreText.text = "0";
        }else
        {
            print("WARNING: Score text in GameUIManager is empty!");
        }

        GameManager.instance.Score.Subscribe(UpdateScore);
        EventBus.DeathEvent += ShowDeathScreen;
    }

    public void UpdateScore(float score)
    {
        if (ScoreText == null) { return; }

        ScoreText.text = ScoreFormatter.Format(score);
    }

    public void ShowDeathScreen()
    {
        EndScreen.SetActive(true);
        GameplayPanel.SetActive(false);
    }

    public void OnDisable()
    {
        GameManager.instance.Score.Unsubscribe(UpdateScore);
        EventBus.DeathEvent -= ShowDeathScreen;
    }
}
