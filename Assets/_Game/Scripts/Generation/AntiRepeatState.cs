using System.Collections.Generic;
using UnityEngine;

namespace MurinoHDR.Generation
{

public sealed class AntiRepeatState
{
    private readonly Dictionary<PropCategory, Queue<string>> _roomRecent = new Dictionary<PropCategory, Queue<string>>();
    private readonly Dictionary<PropCategory, Queue<string>> _floorRecent = new Dictionary<PropCategory, Queue<string>>();
    private readonly Dictionary<PropCategory, int> _roomCounts = new Dictionary<PropCategory, int>();
    private readonly Dictionary<PropCategory, int> _floorCounts = new Dictionary<PropCategory, int>();

    public void BeginRoom()
    {
        _roomRecent.Clear();
        _roomCounts.Clear();
    }

    public bool IsRecentInRoom(PropCategory category, string key)
    {
        Queue<string> queue;
        return _roomRecent.TryGetValue(category, out queue) && queue.Contains(key);
    }

    public bool IsRecentInFloor(PropCategory category, string key)
    {
        Queue<string> queue;
        return _floorRecent.TryGetValue(category, out queue) && queue.Contains(key);
    }

    public int GetRoomCount(PropCategory category)
    {
        int value;
        return _roomCounts.TryGetValue(category, out value) ? value : 0;
    }

    public int GetFloorCount(PropCategory category)
    {
        int value;
        return _floorCounts.TryGetValue(category, out value) ? value : 0;
    }

    public void Register(PropCategory category, string key, int roomHistory, int floorHistory)
    {
        RegisterRecent(_roomRecent, category, key, roomHistory);
        RegisterRecent(_floorRecent, category, key, floorHistory);
        _roomCounts[category] = GetRoomCount(category) + 1;
        _floorCounts[category] = GetFloorCount(category) + 1;
    }

    private static void RegisterRecent(Dictionary<PropCategory, Queue<string>> store, PropCategory category, string key, int limit)
    {
        Queue<string> queue;
        if (!store.TryGetValue(category, out queue))
        {
            queue = new Queue<string>();
            store[category] = queue;
        }

        if (limit <= 0)
        {
            return;
        }

        queue.Enqueue(key ?? string.Empty);
        while (queue.Count > limit)
        {
            queue.Dequeue();
        }
    }
}
}
