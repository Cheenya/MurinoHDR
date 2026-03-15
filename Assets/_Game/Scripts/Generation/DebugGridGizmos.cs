using UnityEngine;

namespace MurinoHDR.Generation
{

[ExecuteAlways]
public sealed class DebugGridGizmos : MonoBehaviour
{
    public enum DebugViewMode
    {
        Occupancy = 0,
        Walkable = 1,
        Reachable = 2,
        Zones = 3,
    }

    [SerializeField] private DebugViewMode _viewMode = DebugViewMode.Reachable;
    [SerializeField] private bool _drawMarkers = true;
    [SerializeField] private float _gizmoHeight = 0.05f;

    private GridMap2D _grid;
    private ValidationReport _report;
    private FloorResult _result;

    public void Configure(FloorResult result, ValidationReport report)
    {
        _result = result;
        _report = report;
        _grid = report != null ? report.Grid : null;
    }

    private void OnDrawGizmosSelected()
    {
        if (_grid == null || _report == null)
        {
            return;
        }

        DrawCells();
        if (_drawMarkers)
        {
            DrawMarkers();
        }
    }

    private void DrawCells()
    {
        for (var y = 0; y < _grid.Height; y++)
        {
            for (var x = 0; x < _grid.Width; x++)
            {
                var index = _grid.Index(x, y);
                if (!ShouldDraw(index))
                {
                    continue;
                }

                Gizmos.color = GetColor(index);
                var center = _grid.CellToWorldCenter(new Vector2Int(x, y)) + Vector3.up * _gizmoHeight;
                Gizmos.DrawCube(center, new Vector3(_grid.CellSize * 0.9f, 0.03f, _grid.CellSize * 0.9f));
            }
        }
    }

    private bool ShouldDraw(int index)
    {
        switch (_viewMode)
        {
            case DebugViewMode.Occupancy:
                return _grid.BlockedRaw[index] || _grid.Door[index] || _grid.Window[index];
            case DebugViewMode.Walkable:
                return _report.Walkable != null && _report.Walkable[index];
            case DebugViewMode.Reachable:
                return _report.Reachable != null && (_report.Reachable[index] || (_report.Walkable != null && _report.Walkable[index]));
            case DebugViewMode.Zones:
                return _report.ZoneId != null && _report.ZoneId[index] >= 0;
            default:
                return false;
        }
    }

    private Color GetColor(int index)
    {
        switch (_viewMode)
        {
            case DebugViewMode.Occupancy:
                if (_grid.Window[index]) return new Color(0.3f, 0.7f, 1f, 0.55f);
                if (_grid.Door[index]) return new Color(0.2f, 1f, 0.4f, 0.6f);
                return new Color(1f, 0.25f, 0.25f, 0.65f);
            case DebugViewMode.Walkable:
                return new Color(0.2f, 0.85f, 0.35f, 0.45f);
            case DebugViewMode.Reachable:
                if (_report.Reachable != null && _report.Reachable[index]) return new Color(0.2f, 0.8f, 1f, 0.45f);
                return new Color(0.8f, 0.25f, 0.25f, 0.35f);
            case DebugViewMode.Zones:
                var zone = _report.ZoneId[index];
                Random.InitState(zone * 7919 + 17);
                return new Color(Random.value, Random.value, Random.value, 0.55f);
            default:
                return Color.white;
        }
    }

    private void DrawMarkers()
    {
        if (_result == null)
        {
            return;
        }

        Gizmos.color = Color.green;
        Gizmos.DrawSphere(_result.SpawnWorld + Vector3.up * 0.2f, 0.18f);

        Gizmos.color = Color.yellow;
        for (var i = 0; i < _result.Pickups.Count; i++)
        {
            Gizmos.DrawSphere(_result.Pickups[i].WorldPos + Vector3.up * 0.2f, 0.12f);
        }

        Gizmos.color = new Color(1f, 0.45f, 0.15f, 1f);
        for (var i = 0; i < _result.Exits.Count; i++)
        {
            Gizmos.DrawCube(_result.Exits[i].WorldPos + Vector3.up * 0.3f, Vector3.one * 0.25f);
        }

        Gizmos.color = new Color(0.55f, 0.8f, 1f, 1f);
        for (var i = 0; i < _result.Doors.Count; i++)
        {
            Gizmos.DrawWireCube(_result.Doors[i].WorldPos + Vector3.up * 1.1f, new Vector3(0.2f, 2.2f, 0.2f));
        }
    }
}
}

