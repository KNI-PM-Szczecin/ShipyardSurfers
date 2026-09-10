public static class ObsticleSetAnalyzer
{
    public static bool ContinuesPreviousObstacle(ObsticleSetSO set, int x, int y)
    {
        for (int back = 1; back <= ObsticleSetSO.MAX_BLOCKADE_LENGTH; back++)
        {
            ObstacleCell previous = set.GetCell(x, y - back);

            if (previous.Type == ObsticleType.Ramp) return back == 1;
            if (previous.Type == ObsticleType.Blockade) return previous.BlockadeLength >= back;
        }

        return false;
    }

    public static bool ContinuesLeftObstacle(ObsticleSetSO set, int x, int y)
    {
        if (set.GetCell(x, y).Type != ObsticleType.Ramp) return false;

        return set.GetCell(x - 1, y).Type == ObsticleType.Ramp;
    }

    public static bool IsCoveredByBlockade(ObsticleSetSO set, int x, int y)
    {
        for (int back = 1; back < ObsticleSetSO.MAX_BLOCKADE_LENGTH; back++)
        {
            ObstacleCell previous = set.GetCell(x, y - back);

            if (previous.Type == ObsticleType.Blockade) return previous.BlockadeLength > back;
        }

        return false;
    }

    public static bool IsPartOfBlockade(ObsticleSetSO set, int x, int y)
    {
        return set.GetCell(x, y).Type == ObsticleType.Blockade || IsCoveredByBlockade(set, x, y);
    }

    public static bool IsRoofReachable(ObsticleSetSO set, int x, int y)
    {
        int row = y;

        while (ObsticleSetSO.IsInside(x, row))
        {
            if (IsCoveredByBlockade(set, x, row))
            {
                row--;
                continue;
            }

            if (set.GetCell(x, row).Type != ObsticleType.Blockade) return false;

            ObstacleCell previous = set.GetCell(x, row - 1);
            if (previous.Type == ObsticleType.Ramp) return true;
            if (!IsPartOfBlockade(set, x, row - 1)) return false;

            row--;
        }

        return false;
    }

    public static bool IsLanePassable(ObsticleSetSO set, int x, int y)
    {
        if (IsPartOfBlockade(set, x, y)) return IsRoofReachable(set, x, y);

        return true;
    }
}
