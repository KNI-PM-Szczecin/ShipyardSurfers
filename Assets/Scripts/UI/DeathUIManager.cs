using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class DeathUIManager : MonoBehaviour
{
#if UNITY_EDITOR
    public SceneAsset MenuScene;
#endif

    [HideInInspector]
    [SerializeField]
    private string MenuSceneName;

    [Header("Score")]
    [SerializeField] private TMP_Text _scoreText;
    [SerializeField] private TMP_Text _bestScoreText;
    [SerializeField] private GameObject _newRecordBadge;

    [Header("Actions")]
    [SerializeField] private GameObject _actionsRow;
    [SerializeField] private GameObject _scoreSavePanel;
    [SerializeField] private TMP_InputField _scoreSaveInput;
    [SerializeField] private Button _saveConfirmButton;

    private float _score;

    private void OnValidate()
    {
#if UNITY_EDITOR
        if (MenuScene != null)
        {
            MenuSceneName = MenuScene.name;
        }
#endif
    }

    private void OnEnable()
    {
        _score = GameManager.instance != null ? GameManager.instance.Score.Value : 0f;
        PresentScore(_score, BestScore());
        ShowActions();

        if (_scoreSaveInput != null) _scoreSaveInput.onValueChanged.AddListener(OnNameChanged);
    }

    private void OnDisable()
    {
        if (_scoreSaveInput != null) _scoreSaveInput.onValueChanged.RemoveListener(OnNameChanged);
    }

    public void PresentScore(float score, float bestScore)
    {
        if (_scoreText != null) _scoreText.text = ScoreFormatter.Format(score);
        if (_bestScoreText != null) _bestScoreText.text = ScoreFormatter.Format(Mathf.Max(bestScore, score));
        if (_newRecordBadge != null) _newRecordBadge.SetActive(score > bestScore && score > 0f);
    }

    public void MainMenu()
    {
        if (string.IsNullOrEmpty(MenuSceneName))
        {
            Debug.LogWarning($"{nameof(DeathUIManager)}: menu scene is not assigned");
            return;
        }

        SceneManager.LoadScene(MenuSceneName);
    }

    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void EnableSaveScoreInput()
    {
        if (_actionsRow != null) _actionsRow.SetActive(false);
        if (_scoreSavePanel != null) _scoreSavePanel.SetActive(true);

        if (_scoreSaveInput != null)
        {
            _scoreSaveInput.text = string.Empty;
            _scoreSaveInput.ActivateInputField();
        }

        OnNameChanged(string.Empty);
    }

    public void CancelSaveScoreInput()
    {
        ShowActions();
    }

    public void SaveScore()
    {
        string playerName = _scoreSaveInput != null ? _scoreSaveInput.text.Trim() : string.Empty;
        if (string.IsNullOrEmpty(playerName)) return;

        HighScoreManager.SaveHighScore(new HighScoreEntry
        {
            Score = _score,
            Name = playerName,
        });

        MainMenu();
    }

    private void ShowActions()
    {
        if (_scoreSavePanel != null) _scoreSavePanel.SetActive(false);
        if (_actionsRow != null) _actionsRow.SetActive(true);
    }

    private void OnNameChanged(string value)
    {
        if (_saveConfirmButton != null) _saveConfirmButton.interactable = !string.IsNullOrWhiteSpace(value);
    }

    private static float BestScore()
    {
        List<HighScoreEntry> scores = HighScoreManager.GetHighScores();
        float best = 0f;
        foreach (HighScoreEntry entry in scores)
        {
            if (entry.Score > best) best = entry.Score;
        }

        return best;
    }
}
