using System;
using System.Collections.Generic;
using UnityEngine;

namespace MurinoHDR.Generation
{

public enum ExitPathType
{
    Elevator = 0,
    Shaft = 1,
    Stairs = 2,
}

public enum ExitPathState
{
    Locked = 0,
    InProgress = 1,
    Open = 2,
}

[Serializable]
public sealed class ExitPathStatus
{
    [SerializeField] private ExitPathType _pathType;
    [SerializeField] private string _displayName = string.Empty;
    [SerializeField] private string _requirementText = string.Empty;
    [SerializeField] private ExitPathState _state = ExitPathState.Locked;

    public ExitPathType PathType => _pathType;
    public string DisplayName => _displayName;
    public string RequirementText => _requirementText;
    public ExitPathState State => _state;

    public void Configure(ExitPathType pathType, string displayName, string requirementText)
    {
        _pathType = pathType;
        _displayName = displayName;
        _requirementText = requirementText;
        _state = ExitPathState.Locked;
    }

    public void SetState(ExitPathState state)
    {
        _state = state;
    }
}

public sealed class FloorGoalController : MonoBehaviour
{
    [SerializeField] private List<ExitPathStatus> _paths = new List<ExitPathStatus>();

    public event Action GoalsChanged;

    public IReadOnlyList<ExitPathStatus> Paths => _paths;

    private void Awake()
    {
        EnsureDefaults();
    }

    public void EnsureDefaults()
    {
        if (_paths.Count == 3)
        {
            return;
        }

        _paths.Clear();

        var elevator = new ExitPathStatus();
        elevator.Configure(ExitPathType.Elevator, "Лифт", "Ключ-карта + починенный предохранитель");
        _paths.Add(elevator);

        var shaft = new ExitPathStatus();
        shaft.Configure(ExitPathType.Shaft, "Шахта", "Ломик + верёвка");
        _paths.Add(shaft);

        var stairs = new ExitPathStatus();
        stairs.Configure(ExitPathType.Stairs, "Лестница", "Отмычка");
        _paths.Add(stairs);
    }

    public ExitPathStatus GetPath(ExitPathType pathType)
    {
        for (var i = 0; i < _paths.Count; i++)
        {
            if (_paths[i].PathType == pathType)
            {
                return _paths[i];
            }
        }

        return null;
    }

    public void MarkInProgress(ExitPathType pathType)
    {
        SetState(pathType, ExitPathState.InProgress);
    }

    public void MarkOpen(ExitPathType pathType)
    {
        SetState(pathType, ExitPathState.Open);
    }

    public bool HasOpenPath()
    {
        for (var i = 0; i < _paths.Count; i++)
        {
            if (_paths[i].State == ExitPathState.Open)
            {
                return true;
            }
        }

        return false;
    }

    public string BuildObjectiveSummary()
    {
        var lines = new List<string>();
        for (var i = 0; i < _paths.Count; i++)
        {
            var path = _paths[i];
            lines.Add(string.Format("{0}: {1}", path.DisplayName, path.State));
            lines.Add(string.Format("  {0}", path.RequirementText));
        }

        return string.Join("\n", lines);
    }

    private void SetState(ExitPathType pathType, ExitPathState newState)
    {
        var path = GetPath(pathType);
        if (path == null || path.State == newState)
        {
            return;
        }

        path.SetState(newState);
        GoalsChanged?.Invoke();
        Debug.Log(string.Format("[GEN] {0} -> {1}", path.DisplayName, newState));
    }
}
}
