using System.Collections.Generic;
using UnityEngine;

public class HighScoreUIManager : MonoBehaviour
{
    private const int DEFAULT_ROW_COUNT = 10;

    [SerializeField] private ScoreboardEntryView _entryPrefab;
    [SerializeField] private Transform _container;
    [SerializeField] private int _rowCount = DEFAULT_ROW_COUNT;

    private void Awake()
    {
        Populate(HighScoreManager.GetHighScores());
    }

    public void Populate(List<HighScoreEntry> highScores)
    {
        if (_entryPrefab == null || _container == null) return;

        for (int i = 0; i < _rowCount; i++)
        {
            HighScoreEntry entry = i < highScores.Count ? highScores[i] : default;
            ScoreboardEntryView view = Instantiate(_entryPrefab, _container);
            view.Bind(i + 1, entry);
        }
    }
}
