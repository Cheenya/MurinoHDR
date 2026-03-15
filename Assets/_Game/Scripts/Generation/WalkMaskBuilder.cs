using UnityEngine;

namespace MurinoHDR.Generation
{

public static class WalkMaskBuilder
{
    public static bool[] BuildWalkable(GridMap2D grid, int inflateRadiusCells)
    {
        var blockedInflated = grid.InflateBlocked(inflateRadiusCells);
        var walkable = new bool[grid.Width * grid.Height];
        for (var i = 0; i < walkable.Length; i++)
        {
            walkable[i] = grid.Floor[i] && !blockedInflated[i];
        }

        return walkable;
    }
}
}

