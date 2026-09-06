using System.Collections.Generic;

public static class ObsticleSetValidator
{
    public static IReadOnlyList<string> Validate(ObsticleSetSO set)
    {
        var issues = new List<string>();

        for (int y = 0; y < ObsticleSetSO.ROWS; y++)
        {
            bool rowPassable = false;

            for (int x = 0; x < ObsticleSetSO.COLUMNS; x++)
            {
                ValidateCell(set, x, y, issues);
                rowPassable |= ObsticleSetAnalyzer.IsLanePassable(set, x, y);
            }

            if (!rowPassable) issues.Add($"Row {y}: no passable lane.");
        }

        return issues;
    }

    private static void ValidateCell(ObsticleSetSO set, int x, int y, List<string> issues)
    {
        ObstacleCell cell = set.GetCell(x, y);
        bool covered = ObsticleSetAnalyzer.IsCoveredByBlockade(set, x, y);

        if (covered && cell.Type != ObsticleType.Empty)
        {
            issues.Add($"Row {y}, lane {x}: obstacle overlaps a blockade from a previous row.");
        }

        switch (cell.Type)
        {
            case ObsticleType.Ramp:
                if (set.GetCell(x, y + 1).Type != ObsticleType.Blockade)
                {
                    issues.Add($"Row {y}, lane {x}: ramp must be directly followed by a blockade.");
                }
                break;

            case ObsticleType.Blockade:
                if (y + cell.BlockadeLength > ObsticleSetSO.ROWS)
                {
                    issues.Add($"Row {y}, lane {x}: blockade extends past the last row.");
                }
                break;
        }

        if (cell.HasCoin && ObsticleSetAnalyzer.IsPartOfBlockade(set, x, y) && !ObsticleSetAnalyzer.IsRoofReachable(set, x, y))
        {
            issues.Add($"Row {y}, lane {x}: coin sits on a blockade roof with no ramp leading to it.");
        }
    }
}
