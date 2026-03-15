using System;
using System.Collections.Generic;
using UnityEngine;

namespace MurinoHDR.Generation
{

[CreateAssetMenu(menuName = "Murino/Generation/Room Type Def", fileName = "RoomTypeDef")]
public sealed class RoomTypeDef : ScriptableObject
{
    [SerializeField] private RoomType _type;
    [SerializeField] private RoomTags _tags;
    [SerializeField] private Vector2 _sizeMinMaxMetersX = new Vector2(4f, 8f);
    [SerializeField] private Vector2 _sizeMinMaxMetersZ = new Vector2(4f, 8f);
    [SerializeField] [Range(0f, 1f)] private float _facadePreference = 0.5f;
    [SerializeField] [Range(0f, 1f)] private float _corePreference = 0.5f;
    [SerializeField] private bool _requiresWindow;
    [SerializeField] private int _minPerFloor;
    [SerializeField] private int _maxPerFloor = 1;
    [SerializeField] private float _weightOfficeHeavy = 1f;
    [SerializeField] private float _weightTechHeavy = 1f;
    [SerializeField] private float _weightStorageHeavy = 1f;
    [SerializeField] private float _weightRepresentative = 1f;
    [SerializeField] private List<WeightedPropPattern> _patterns = new List<WeightedPropPattern>();

    public RoomType Type => _type;
    public RoomTags Tags => _tags;
    public Vector2 SizeMinMaxMetersX => _sizeMinMaxMetersX;
    public Vector2 SizeMinMaxMetersZ => _sizeMinMaxMetersZ;
    public float FacadePreference => _facadePreference;
    public float CorePreference => _corePreference;
    public bool RequiresWindow => _requiresWindow;
    public int MinPerFloor => _minPerFloor;
    public int MaxPerFloor => Mathf.Max(_minPerFloor, _maxPerFloor);
    public IReadOnlyList<WeightedPropPattern> Patterns => _patterns;

    public void Configure(
        RoomType type,
        RoomTags tags,
        Vector2 sizeMinMaxMetersX,
        Vector2 sizeMinMaxMetersZ,
        float facadePreference,
        float corePreference,
        bool requiresWindow,
        int minPerFloor,
        int maxPerFloor,
        float weightOfficeHeavy,
        float weightTechHeavy,
        float weightStorageHeavy,
        float weightRepresentative,
        IList<PropPatternDef> patterns)
    {
        _type = type;
        _tags = tags;
        _sizeMinMaxMetersX = sizeMinMaxMetersX;
        _sizeMinMaxMetersZ = sizeMinMaxMetersZ;
        _facadePreference = Mathf.Clamp01(facadePreference);
        _corePreference = Mathf.Clamp01(corePreference);
        _requiresWindow = requiresWindow;
        _minPerFloor = Mathf.Max(0, minPerFloor);
        _maxPerFloor = Mathf.Max(_minPerFloor, maxPerFloor);
        _weightOfficeHeavy = Mathf.Max(0f, weightOfficeHeavy);
        _weightTechHeavy = Mathf.Max(0f, weightTechHeavy);
        _weightStorageHeavy = Mathf.Max(0f, weightStorageHeavy);
        _weightRepresentative = Mathf.Max(0f, weightRepresentative);
        _patterns.Clear();
        if (patterns == null)
        {
            return;
        }

        for (var i = 0; i < patterns.Count; i++)
        {
            if (patterns[i] == null)
            {
                continue;
            }

            _patterns.Add(new WeightedPropPattern
            {
                Pattern = patterns[i],
                Weight = Mathf.Max(0.01f, patterns[i].Weight),
            });
        }
    }

    public float GetStyleWeight(FloorStyle style)
    {
        switch (style)
        {
            case FloorStyle.TechHeavy:
                return _weightTechHeavy;
            case FloorStyle.OpenSpaceHeavy:
                return _weightOfficeHeavy;
            case FloorStyle.Representative:
                return _weightRepresentative;
            case FloorStyle.CabinetHeavy:
            default:
                return _weightStorageHeavy;
        }
    }

    public PropPatternDef PickPattern(System.Random rng)
    {
        if (_patterns == null || _patterns.Count == 0)
        {
            return null;
        }

        var total = 0f;
        for (var i = 0; i < _patterns.Count; i++)
        {
            total += Mathf.Max(0.01f, _patterns[i].Weight);
        }

        var roll = (float)rng.NextDouble() * total;
        for (var i = 0; i < _patterns.Count; i++)
        {
            roll -= Mathf.Max(0.01f, _patterns[i].Weight);
            if (roll <= 0f)
            {
                return _patterns[i].Pattern;
            }
        }

        return _patterns[_patterns.Count - 1].Pattern;
    }
}

[Serializable]
public sealed class WeightedPropPattern
{
    public PropPatternDef Pattern;
    public float Weight = 1f;
}
}
