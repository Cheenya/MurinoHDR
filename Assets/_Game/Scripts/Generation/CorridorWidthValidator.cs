using System.Collections.Generic;
using UnityEngine;

namespace MurinoHDR.Generation
{

public static class CorridorWidthValidator
{
    public static List<ValidationError> ValidateMinWidth(
        GridMap2D grid,
        bool[] walkable,
        int minMainWidthCells,
        int minSecondaryWidthCells,
        int stride,
        bool allowSingleCellTechChokepoints)
    {
        var errors = new List<ValidationError>();
        if (grid == null)
        {
            return errors;
        }

        var step = Mathf.Max(1, stride);
        for (var y = 0; y < grid.Height; y += step)
        {
            for (var x = 0; x < grid.Width; x += step)
            {
                var index = grid.Index(x, y);
                if (!walkable[index])
                {
                    continue;
                }

                var tags = grid.Tags[index];
                var isMainCorridor = (tags & GridCellTags.MainCorridor) != 0;
                var isSecondaryCorridor = (tags & GridCellTags.SecondaryCorridor) != 0 || ((tags & GridCellTags.Corridor) != 0 && !isMainCorridor);
                if (!isMainCorridor && !isSecondaryCorridor)
                {
                    continue;
                }

                var width = EstimateLocalWidth(grid, walkable, x, y);
                var minWidth = isMainCorridor ? minMainWidthCells : minSecondaryWidthCells;
                if (width < minWidth)
                {
                    errors.Add(new ValidationError
                    {
                        Code = ValidationErrorCode.CorridorTooNarrow,
                        Severity = ValidationSeverity.Error,
                        Message = string.Format("Коридор уже минимума ({0} < {1} клеток)", width, minWidth),
                        Cell = new Vector2Int(x, y),
                        WorldPos = grid.CellToWorldCenter(new Vector2Int(x, y)),
                        ExpectedMinWidthCells = minWidth,
                        ActualWidthCells = width,
                        SuggestedFix = SuggestedFix.WidenCorridor,
                    });
                }

                if (!allowSingleCellTechChokepoints && width <= 1)
                {
                    errors.Add(new ValidationError
                    {
                        Code = ValidationErrorCode.ForbiddenChokepoint,
                        Severity = ValidationSeverity.Error,
                        Message = "Найден запрещённый one-cell chokepoint",
                        Cell = new Vector2Int(x, y),
                        WorldPos = grid.CellToWorldCenter(new Vector2Int(x, y)),
                        ActualWidthCells = width,
                        SuggestedFix = SuggestedFix.WidenCorridor,
                    });
                }
            }
        }

        return errors;
    }

    private static int EstimateLocalWidth(GridMap2D grid, bool[] walkable, int x, int y)
    {
        var horizontal = 1 + ScanAxis(grid, walkable, x, y, -1, 0) + ScanAxis(grid, walkable, x, y, 1, 0);
        var vertical = 1 + ScanAxis(grid, walkable, x, y, 0, -1) + ScanAxis(grid, walkable, x, y, 0, 1);
        return Mathf.Min(horizontal, vertical);
    }

    private static int ScanAxis(GridMap2D grid, bool[] walkable, int startX, int startY, int stepX, int stepY)
    {
        var count = 0;
        var x = startX + stepX;
        var y = startY + stepY;
        while (grid.InBounds(x, y) && walkable[grid.Index(x, y)])
        {
            count++;
            x += stepX;
            y += stepY;
        }

        return count;
    }
}
}

