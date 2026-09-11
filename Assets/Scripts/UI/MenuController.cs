using System.Collections;
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
    private Button _creditsButton;
    [SerializeField]
    private Button _calibrationButton;
    [SerializeField]
    private Button _quitButton;
    [SerializeField]
    private Button _closeSettingsButton;
    [SerializeField]
    private Button _closeCreditsButton;

    [Space]
    [SerializeField]
    private GameObject _settingsPanel;
    [SerializeField]
    private GameObject _creditsPanel;
    [SerializeField]
    private GameObject _menuPanel;

    [Space]
    [SerializeField, Tooltip("Optional: snaps the crane rope when the game starts and delays the scene load until the container has fallen")]
    private ContainerDrop _containerDrop;

#if UNITY_EDITOR
    [Space]
    public SceneAsset GameScene;
    public SceneAsset CalibrationScene;
#endif

    [HideInInspector]
    [SerializeField]
    private string GameSceneName;
    [HideInInspector]
    [SerializeField]
    private string CalibrationSceneName;

    private void OnValidate()
    {
#if UNITY_EDITOR
        if (GameScene != null)
        {
            GameSceneName = GameScene.name;
        }

        if (CalibrationScene != null)
        {
            CalibrationSceneName = CalibrationScene.name;
        }
#endif
    }

    private bool AreReferencesMissing => _startGameButton == null || _settingsButton == null || _quitButton == null
                                         || _closeSettingsButton == null;

    private void Awake()
    {
        if (AreReferencesMissing)
        {
            print("ERROR: buttons not pinned to menu controller");
            return;
        }

        _startGameButton.onClick.AddListener(StartGame);
        _settingsButton.onClick.AddListener(OpenSettings);
        _quitButton.onClick.AddListener(QuitGame);
        _closeSettingsButton.onClick.AddListener(CloseSettings);

        if (_creditsButton != null) _creditsButton.onClick.AddListener(OpenCredits);
        if (_closeCreditsButton != null) _closeCreditsButton.onClick.AddListener(CloseCredits);
        if (_calibrationButton != null) _calibrationButton.onClick.AddListener(OpenCalibration);
    }

    private void OnDestroy()
    {
        if (AreReferencesMissing) { return; }

        _startGameButton.onClick.RemoveListener(StartGame);
        _settingsButton.onClick.RemoveListener(OpenSettings);
        _quitButton.onClick.RemoveListener(QuitGame);
        _closeSettingsButton.onClick.RemoveListener(CloseSettings);

        if (_creditsButton != null) _creditsButton.onClick.RemoveListener(OpenCredits);
        if (_closeCreditsButton != null) _closeCreditsButton.onClick.RemoveListener(CloseCredits);
        if (_calibrationButton != null) _calibrationButton.onClick.RemoveListener(OpenCalibration);
    }

    private void OpenSettings() => ShowPanel(_settingsPanel);

    private void CloseSettings() => ShowPanel(_menuPanel);

    private void OpenCredits() => ShowPanel(_creditsPanel);

    private void CloseCredits() => ShowPanel(_menuPanel);

    private void ShowPanel(GameObject panel)
    {
        if (panel == null) return;

        if (_menuPanel != null) _menuPanel.SetActive(panel == _menuPanel);
        if (_settingsPanel != null) _settingsPanel.SetActive(panel == _settingsPanel);
        if (_creditsPanel != null) _creditsPanel.SetActive(panel == _creditsPanel);
    }

    private void QuitGame()
    {
        Application.Quit();
        print("Debug: exit game");
    }

    private void StartGame()
    {
        if (_containerDrop == null || _containerDrop.HasDropped)
        {
            LoadScene(GameSceneName, nameof(GameSceneName));
            return;
        }

        _containerDrop.Drop();
        SetMenuInteractable(false);
        StartCoroutine(LoadSceneAfter(GameSceneName, nameof(GameSceneName), _containerDrop.DropSeconds));
    }

    private IEnumerator LoadSceneAfter(string sceneName, string label, float seconds)
    {
        yield return new WaitForSeconds(seconds);
        LoadScene(sceneName, label);
    }

    private void SetMenuInteractable(bool interactable)
    {
        foreach (Button button in new[] { _startGameButton, _settingsButton, _creditsButton, _calibrationButton, _quitButton })
        {
            if (button != null) button.interactable = interactable;
        }
    }

    private void OpenCalibration() => LoadScene(CalibrationSceneName, nameof(CalibrationSceneName));

    private void LoadScene(string sceneName, string label)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            print($"WARNINIG: {label} in MenuController is null!");
            return;
        }

        SceneManager.LoadScene(sceneName);
    }
}
