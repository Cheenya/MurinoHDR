using System;
using System.Collections.Generic;
using UnityEngine;

namespace MurinoHDR.Generation
{

public enum AnchorPlan
{
    AlongWall = 0,
    Corner = 1,
    IslandGrid = 2,
    Center = 3,
    NearWindow = 4,
    NearDoor = 5,
    CeilingLine = 6,
    FloorLine = 7,
}

public enum PlacementHint
{
    AlongWall = 0,
    Island = 1,
    Corner = 2,
    NearWindow = 3,
    Center = 4,
    NearDoor = 5,
}

[Serializable]
public sealed class ClearanceConfig
{
    public float MinAisleMeters = 0.7f;
    public float ComfortAisleMeters = 1f;
    public float DoorPocketDepthMeters = 1.2f;
    public float WindowStripDepthMeters = 0.6f;
}

[Serializable]
public sealed class PropRequirement
{
    public PropCategory Category;
    public int MinCount = 1;
    public int MaxCount = 1;
    public bool BlocksMovement;
    public PlacementHint PlacementHint = PlacementHint.AlongWall;
    public bool PreferNearWindow;
}

[Serializable]
public sealed class AntiRepeatPolicy
{
    public int NoRepeatLastNInRoom = 2;
    public int NoRepeatLastNInFloor = 3;
    public int MaxPerRoomPerCategory = 4;
}

[Serializable]
public sealed class PropPlacementRules
{
    public bool ForbidBlockingPropsInWindowStrip = true;
    public bool ForbidBlockingPropsInWalkSpine = true;
    public bool ForbidAnyPropsInDoorPocket = true;
    public bool AllowTallPropsNearWindow;
}

[CreateAssetMenu(menuName = "Murino/Generation/Prop Pattern Def", fileName = "PropPatternDef")]
public sealed class PropPatternDef : ScriptableObject
{
    [SerializeField] private string _patternId = string.Empty;
    [SerializeField] private float _weight = 1f;
    [SerializeField] private AnchorPlan _anchorPlan;
    [SerializeField] private ClearanceConfig _clearance = new ClearanceConfig();
    [SerializeField] private List<PropRequirement> _requiredSets = new List<PropRequirement>();
    [SerializeField] private List<PropRequirement> _optionalSets = new List<PropRequirement>();
    [SerializeField] private int _optionalBudget = 2;
    [SerializeField] private AntiRepeatPolicy _antiRepeat = new AntiRepeatPolicy();
    [SerializeField] private PropPlacementRules _placementRules = new PropPlacementRules();

    public string PatternId => _patternId;
    public float Weight => _weight;
    public AnchorPlan AnchorPlan => _anchorPlan;
    public ClearanceConfig Clearance => _clearance;
    public IReadOnlyList<PropRequirement> RequiredSets => _requiredSets;
    public IReadOnlyList<PropRequirement> OptionalSets => _optionalSets;
    public int OptionalBudget => Mathf.Max(0, _optionalBudget);
    public AntiRepeatPolicy AntiRepeat => _antiRepeat;
    public PropPlacementRules PlacementRules => _placementRules;

    public void Configure(
        string patternId,
        float weight,
        AnchorPlan anchorPlan,
        ClearanceConfig clearance,
        IList<PropRequirement> requiredSets,
        IList<PropRequirement> optionalSets,
        int optionalBudget,
        AntiRepeatPolicy antiRepeat,
        PropPlacementRules placementRules)
    {
        _patternId = patternId ?? string.Empty;
        _weight = Mathf.Max(0.01f, weight);
        _anchorPlan = anchorPlan;
        _clearance = clearance ?? new ClearanceConfig();
        _requiredSets = requiredSets != null ? new List<PropRequirement>(requiredSets) : new List<PropRequirement>();
        _optionalSets = optionalSets != null ? new List<PropRequirement>(optionalSets) : new List<PropRequirement>();
        _optionalBudget = Mathf.Max(0, optionalBudget);
        _antiRepeat = antiRepeat ?? new AntiRepeatPolicy();
        _placementRules = placementRules ?? new PropPlacementRules();
    }
}
}
