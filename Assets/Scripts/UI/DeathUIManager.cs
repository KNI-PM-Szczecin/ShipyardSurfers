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

    private const string RECORD_CAPTION = "Rekord";
    private const string BEATEN_RECORD_CAPTION = "Poprzedni rekord";
    private const string UNKNOWN_HOLDER = "Anonim";

    [Header("Score")]
    [SerializeField] private TMP_Text _scoreText;
    [SerializeField] private GameObject _bestRow;
    [SerializeField] private TMP_Text _bestCaptionText;
    [SerializeField] private TMP_Text _bestNameText;
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
        bool hasRecord = TryGetRecord(out HighScoreEntry record);
        PresentScore(_score, record, hasRecord);
        ShowActions();

        if (_scoreSaveInput != null) _scoreSaveInput.onValueChanged.AddListener(OnNameChanged);
    }

    private void OnDisable()
    {
        if (_scoreSaveInput != null) _scoreSaveInput.onValueChanged.RemoveListener(OnNameChanged);
    }

    public void PresentScore(float score, HighScoreEntry record, bool hasRecord)
    {
        if (_scoreText != null) _scoreText.text = ScoreFormatter.Format(score);

        bool beatenRecord = hasRecord && score > record.Score;
        if (_newRecordBadge != null) _newRecordBadge.SetActive(score > 0f && (!hasRecord || beatenRecord));
        if (_bestRow != null) _bestRow.SetActive(hasRecord);
        if (!hasRecord) return;

        if (_bestCaptionText != null) _bestCaptionText.text = beatenRecord ? BEATEN_RECORD_CAPTION : RECORD_CAPTION;
        if (_bestNameText != null) _bestNameText.text = string.IsNullOrWhiteSpace(record.Name) ? UNKNOWN_HOLDER : record.Name;
        if (_bestScoreText != null) _bestScoreText.text = ScoreFormatter.Format(record.Score);
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

    private static bool TryGetRecord(out HighScoreEntry record)
    {
        record = default;
        bool found = false;

        foreach (HighScoreEntry entry in HighScoreManager.GetHighScores())
        {
            if (found && entry.Score <= record.Score) continue;

            record = entry;
            found = true;
        }

        return found;
    }
}
