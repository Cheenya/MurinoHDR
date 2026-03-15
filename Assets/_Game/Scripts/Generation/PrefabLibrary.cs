using System;
using UnityEngine;

namespace MurinoHDR.Generation
{

[Serializable]
public sealed class PropPrefabEntry
{
    public string Category = string.Empty;
    public GameObject Prefab;
}

[CreateAssetMenu(menuName = "Murino/Generation/Prefab Library", fileName = "PrefabLibrary")]
public sealed class PrefabLibrary : ScriptableObject
{
    [SerializeField] private GameObject _wallPrefab;
    [SerializeField] private GameObject _floorPrefab;
    [SerializeField] private GameObject _ceilingPrefab;
    [SerializeField] private GameObject _doorPrefab;
    [SerializeField] private GameObject _windowPrefab;
    [SerializeField] private PropPrefabEntry[] _propPrefabs = Array.Empty<PropPrefabEntry>();
    [SerializeField] private GameObject _exitMarkerPrefab;
    [SerializeField] private GameObject _pickupMarkerPrefab;
    [SerializeField] private GameObject _debugMarkerPrefab;

    public GameObject WallPrefab => _wallPrefab;
    public GameObject FloorPrefab => _floorPrefab;
    public GameObject CeilingPrefab => _ceilingPrefab;
    public GameObject DoorPrefab => _doorPrefab;
    public GameObject WindowPrefab => _windowPrefab;
    public PropPrefabEntry[] PropPrefabs => _propPrefabs;
    public GameObject ExitMarkerPrefab => _exitMarkerPrefab;
    public GameObject PickupMarkerPrefab => _pickupMarkerPrefab;
    public GameObject DebugMarkerPrefab => _debugMarkerPrefab;
}
}
