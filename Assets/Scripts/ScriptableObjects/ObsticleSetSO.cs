using UnityEngine;

public enum ObsticleType
{
    Empty,
    Jump,
    Slide,
    Blockade,
    Ramp
}

[System.Serializable]
public struct ObstacleCell
{
    public ObsticleType Type;

    [Range(1, 3)]
    public int BlockadeLength;

    public bool HasCoin;
}

[CreateAssetMenu(menuName = "ScriptableObjects/ObsticleSetSO", order = 1)]
public class ObsticleSetSO : ScriptableObject
{
    public const int Columns = 3;
    public const int Rows = 15;

    [HideInInspector]
    public ObstacleCell[] Grid = new ObstacleCell[Columns * Rows];

    public ObstacleCell GetCell(int x, int y)
    {
        if (x < 0 || x >= Columns || y < 0 || y >= Rows)
            return new ObstacleCell { Type = ObsticleType.Empty };

        return Grid[y * Columns + x];
    }

    private void OnValidate()
    {
        if (Grid == null || Grid.Length != Columns * Rows)
        {
            Grid = new ObstacleCell[Columns * Rows];
            for (int i = 0; i < Grid.Length; i++)
            {
                Grid[i].BlockadeLength = 1;
            }
        }
    }
}
