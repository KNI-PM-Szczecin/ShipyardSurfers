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

    [Range(1, ObsticleSetSO.MAX_BLOCKADE_LENGTH)]
    public int BlockadeLength;

    public bool HasCoin;
}

[CreateAssetMenu(menuName = "ScriptableObjects/ObsticleSetSO", order = 1)]
public class ObsticleSetSO : ScriptableObject
{
    public const int COLUMNS = 3;
    public const int ROWS = 25;
    public const int MAX_BLOCKADE_LENGTH = 3;

    [HideInInspector]
    public ObstacleCell[] Grid = new ObstacleCell[COLUMNS * ROWS];

    public static bool IsInside(int x, int y) => x >= 0 && x < COLUMNS && y >= 0 && y < ROWS;

    public ObstacleCell GetCell(int x, int y)
    {
        if (!IsInside(x, y))
            return new ObstacleCell { Type = ObsticleType.Empty, BlockadeLength = 1 };

        return Grid[y * COLUMNS + x];
    }

    private void OnValidate()
    {
        if (Grid != null && Grid.Length == COLUMNS * ROWS) return;

        Grid = new ObstacleCell[COLUMNS * ROWS];
        for (int i = 0; i < Grid.Length; i++)
        {
            Grid[i].BlockadeLength = 1;
        }
    }
}
