using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MenuController : MonoBehaviour
{
    [SerializeField]
    private Button _startGameButton;
    [FormerlySerializedAs("_settignsButton")]
    [SerializeField]
    private Button _settingsButton;
    [SerializeField]
    private Button _quitButton;
    [SerializeField]
    private Button _closeSettingsButton;

    [Space]
    [SerializeField]
    private GameObject _settingsPanel;
    [SerializeField]
    private GameObject _menuPanel;

#if UNITY_EDITOR
    [Space]
    public SceneAsset GameScene;
#endif

    [HideInInspector]
    [SerializeField]
    private string GameSceneName;
    private void OnValidate()
    {
#if UNITY_EDITOR
        if (GameScene != null)
        {
            GameSceneName = GameScene.name;
        }
#endif
    }

    private bool areReferencesNotValid => _startGameButton == null || _settingsButton == null || _quitButton == null || _closeSettingsButton == null;

    private void Awake()
    {
        if (areReferencesNotValid)
        {
            print("ERROR: buttons not pinned to menu controller");
            return;
        }

        _startGameButton.onClick.AddListener(startGame);
        _settingsButton.onClick.AddListener(openSettings);
        _quitButton.onClick.AddListener(quitGame);
        _closeSettingsButton.onClick.AddListener(closeSettings);

    }

    private void openSettings()
    {
        _settingsPanel.SetActive(true);
        _menuPanel.SetActive(false);
    }

    private void closeSettings()
    {
        _settingsPanel.SetActive(false);
        _menuPanel.SetActive(true);
    }

    private void quitGame()
    {
        Application.Quit();
        print("Debug: exit game");
    }

    private void startGame()
    {
        if (GameSceneName != "")
        {
            SceneManager.LoadScene(GameSceneName);
        }
        else
        {
            print("WARNINIG: Game scene in MenuController is null!");
        }
    }

    private void OnDestroy()
    {
        if (areReferencesNotValid) { return; }

        _startGameButton.onClick.RemoveListener(startGame);
        _settingsButton.onClick.RemoveListener(openSettings);
        _quitButton.onClick.RemoveListener(quitGame);
        _closeSettingsButton.onClick.RemoveListener(closeSettings);
    }
}
