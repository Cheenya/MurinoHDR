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

[Serializable]
public sealed class PrefabPoolEntry
{
    public PropCategory Category;
    public GameObject[] Prefabs = Array.Empty<GameObject>();
    public float[] Weights = Array.Empty<float>();
    public int MaxPerRoom = 8;
    public int MaxPerFloor = 24;
    public int AntiRepeatLastN = 2;
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
    [SerializeField] private PrefabPoolEntry[] _pools = Array.Empty<PrefabPoolEntry>();
    [SerializeField] private GameObject _exitMarkerPrefab;
    [SerializeField] private GameObject _pickupMarkerPrefab;
    [SerializeField] private GameObject _debugMarkerPrefab;

    public GameObject WallPrefab => _wallPrefab;
    public GameObject FloorPrefab => _floorPrefab;
    public GameObject CeilingPrefab => _ceilingPrefab;
    public GameObject DoorPrefab => _doorPrefab;
    public GameObject WindowPrefab => _windowPrefab;
    public PropPrefabEntry[] PropPrefabs => _propPrefabs;
    public PrefabPoolEntry[] Pools => _pools;
    public GameObject ExitMarkerPrefab => _exitMarkerPrefab;
    public GameObject PickupMarkerPrefab => _pickupMarkerPrefab;
    public GameObject DebugMarkerPrefab => _debugMarkerPrefab;

    public GameObject PickPrefab(PropCategory category, System.Random rng, AntiRepeatState state, int roomId)
    {
        for (var i = 0; i < _pools.Length; i++)
        {
            var pool = _pools[i];
            if (pool == null || pool.Category != category || pool.Prefabs == null || pool.Prefabs.Length == 0)
            {
                continue;
            }

            if (state != null)
            {
                if (state.GetRoomCount(category) >= Mathf.Max(1, pool.MaxPerRoom))
                {
                    return null;
                }

                if (state.GetFloorCount(category) >= Mathf.Max(1, pool.MaxPerFloor))
                {
                    return null;
                }
            }

            var candidates = new System.Collections.Generic.List<int>();
            for (var prefabIndex = 0; prefabIndex < pool.Prefabs.Length; prefabIndex++)
            {
                var prefab = pool.Prefabs[prefabIndex];
                if (prefab == null)
                {
                    continue;
                }

                var key = prefab.name;
                var antiRepeat = Mathf.Max(0, pool.AntiRepeatLastN);
                if (state != null && antiRepeat > 0 && (state.IsRecentInRoom(category, key) || state.IsRecentInFloor(category, key)))
                {
                    continue;
                }

                candidates.Add(prefabIndex);
            }

            if (candidates.Count == 0)
            {
                for (var prefabIndex = 0; prefabIndex < pool.Prefabs.Length; prefabIndex++)
                {
                    if (pool.Prefabs[prefabIndex] != null)
                    {
                        candidates.Add(prefabIndex);
                    }
                }
            }

            if (candidates.Count == 0)
            {
                return null;
            }

            var totalWeight = 0f;
            for (var iCandidate = 0; iCandidate < candidates.Count; iCandidate++)
            {
                var weightIndex = candidates[iCandidate];
                totalWeight += GetWeight(pool, weightIndex);
            }

            var roll = (float)rng.NextDouble() * Mathf.Max(0.01f, totalWeight);
            for (var iCandidate = 0; iCandidate < candidates.Count; iCandidate++)
            {
                var prefabIndex = candidates[iCandidate];
                roll -= GetWeight(pool, prefabIndex);
                if (roll > 0f)
                {
                    continue;
                }

                var picked = pool.Prefabs[prefabIndex];
                if (state != null && picked != null)
                {
                    state.Register(category, picked.name, pool.AntiRepeatLastN, pool.AntiRepeatLastN);
                }

                return picked;
            }

            var fallback = pool.Prefabs[candidates[0]];
            if (state != null && fallback != null)
            {
                state.Register(category, fallback.name, pool.AntiRepeatLastN, pool.AntiRepeatLastN);
            }

            return fallback;
        }

        return null;
    }

    private static float GetWeight(PrefabPoolEntry pool, int index)
    {
        if (pool.Weights == null || index < 0 || index >= pool.Weights.Length)
        {
            return 1f;
        }

        return Mathf.Max(0.01f, pool.Weights[index]);
    }
}
}
