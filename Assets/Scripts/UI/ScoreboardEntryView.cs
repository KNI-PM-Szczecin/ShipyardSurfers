using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ScoreboardEntryView : MonoBehaviour
{
    private const string EMPTY_NAME = "---";

    [SerializeField] private TMP_Text _positionText;
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _scoreText;
    [SerializeField] private Image _rankBadge;
    [SerializeField] private Image _rowBackground;

    [Header("Style")]
    [SerializeField] private Color[] _medalColors;
    [SerializeField] private Color _defaultBadgeColor = Color.gray;
    [SerializeField] private Color _medalTextColor = Color.black;
    [SerializeField] private Color _defaultTextColor = Color.white;
    [SerializeField] private Color _emptyTextColor = Color.gray;
    [SerializeField] private Color _nameColor = Color.white;
    [SerializeField] private Color _scoreColor = Color.white;
    [SerializeField] private Color _evenRowColor = new Color(1f, 1f, 1f, 0f);
    [SerializeField] private Color _oddRowColor = new Color(1f, 1f, 1f, 0.05f);

    public void Bind(int position, HighScoreEntry entry)
    {
        bool isEmpty = string.IsNullOrEmpty(entry.Name) || entry.Name == EMPTY_NAME;

        bool hasMedal = !isEmpty && _medalColors != null && position >= 1 && position <= _medalColors.Length;
        if (_positionText != null)
        {
            _positionText.text = position.ToString();
            _positionText.color = hasMedal ? _medalTextColor : _defaultTextColor;
        }
        if (_nameText != null)
        {
            _nameText.text = isEmpty ? EMPTY_NAME : entry.Name;
            _nameText.color = isEmpty ? _emptyTextColor : _nameColor;
        }

        if (_scoreText != null)
        {
            _scoreText.text = isEmpty ? "-" : ScoreFormatter.Format(entry.Score);
            _scoreText.color = isEmpty ? _emptyTextColor : _scoreColor;
        }

        if (_rankBadge != null) _rankBadge.color = BadgeColor(position, isEmpty);
        if (_rowBackground != null) _rowBackground.color = position % 2 == 0 ? _evenRowColor : _oddRowColor;
    }

    private Color BadgeColor(int position, bool isEmpty)
    {
        if (isEmpty || _medalColors == null) return _defaultBadgeColor;

        int index = position - 1;
        return index >= 0 && index < _medalColors.Length ? _medalColors[index] : _defaultBadgeColor;
    }
}
