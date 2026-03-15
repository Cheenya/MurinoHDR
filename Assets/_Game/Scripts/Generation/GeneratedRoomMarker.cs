using UnityEngine;

namespace MurinoHDR.Generation
{

public sealed class GeneratedRoomMarker : MonoBehaviour
{
    [SerializeField] private string _instanceId = string.Empty;
    [SerializeField] private RoomCategory _category;
    [SerializeField] private Vector2Int _gridSize;
    [SerializeField] private Vector3 _worldCenter;

    public string InstanceId => _instanceId;
    public RoomCategory Category => _category;
    public Vector2Int GridSize => _gridSize;
    public Vector3 WorldCenter => _worldCenter;

    public void Configure(string instanceId, RoomCategory category, Vector2Int gridSize, Vector3 worldCenter)
    {
        _instanceId = instanceId;
        _category = category;
        _gridSize = gridSize;
        _worldCenter = worldCenter;
    }
}
}
