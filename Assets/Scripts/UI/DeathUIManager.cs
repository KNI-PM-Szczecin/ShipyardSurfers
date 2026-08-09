using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DeathUIManager : MonoBehaviour
{
    #if UNITY_EDITOR
        public SceneAsset MenuScene;
    #endif

    [HideInInspector]
    [SerializeField]
    private string MenuSceneName;
    private void OnValidate()
    {
        #if UNITY_EDITOR
            if (MenuScene != null)
            {
                MenuSceneName = MenuScene.name;
            }
        #endif
    }

    [SerializeField]
    private GameObject ScoreSavePanel;

    [SerializeField]
    private TMP_Text ScoreText;
    private float _score;

    [SerializeField]
    private TMP_InputField UserNameInput;

    private void OnEnable()
    {
        _score = GameManager.instance.Score.Value;
        ScoreText.text = ((int)_score).ToString();
    }

    public void MainMenu()
    {
        if (MenuSceneName != "")
        {
            SceneManager.LoadScene(MenuSceneName);
        }
        else
        {
            print("WARNINIG: Menu scene in DeathUIManager is null!");
        }
        
    }

    public void EnableSaveScoreInput()
    {
        ScoreSavePanel.SetActive(true);
    }

    public void SaveScore()
    {
        // ScoreManager.SaveScore(value, username)
        MainMenu();
    }
}
