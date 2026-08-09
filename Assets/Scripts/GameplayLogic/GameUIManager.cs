using TMPro;
using UnityEngine;

public class GameUIManager : MonoBehaviour
{
    public TMP_Text Text;

    void Start()
    {
        if (Text != null)
        {
            Text.text = "0";
        }else
        {
            print("WARNING: Score text in GameUIManager is empty!");
        }

        GameManager.instance.Score.Subscribe(UpdateScore);
    }

    public void UpdateScore(float score)
    {
        if (Text == null) { return; }

        Text.text = ((int)score).ToString();
    }
}
