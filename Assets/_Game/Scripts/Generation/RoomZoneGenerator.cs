using System.Collections.Generic;
using UnityEngine;

namespace MurinoHDR.Generation
{

public static class RoomZoneGenerator
{
    private const float DefaultPocketMargin = 0.2f;

    public static RoomZones Generate(RoomInstance room, IList<DoorInstance> roomDoors, ValidationConfig config, PropPatternDef pattern)
    {
        var zones = new RoomZones();
        zones.RoomId = room.RoomId;
        zones.DetailedType = room.DetailedType;
        zones.RoomBoundsWorld = room.WorldBounds;
        zones.WalkSpineWorldXZ = room.WalkSpineRectXZ;

        var doorPocketDepth = pattern != null ? pattern.Clearance.DoorPocketDepthMeters : 1.2f;
        var windowStripDepth = pattern != null ? pattern.Clearance.WindowStripDepthMeters : 0.6f;

        if (roomDoors != null)
        {
            for (var i = 0; i < roomDoors.Count; i++)
            {
                var pocket = BuildDoorPocket(room.WorldBounds, roomDoors[i], doorPocketDepth);
                if (pocket.width > 0.01f && pocket.height > 0.01f)
                {
                    zones.DoorPocketsWorldXZ.Add(pocket);
                }
            }
        }

        Rect windowStrip;
        if (TryBuildWindowStrip(room, windowStripDepth, out windowStrip))
        {
            zones.WindowStripWorldXZ = windowStrip;
            zones.HasWindowStrip = true;
        }

        BuildPropZones(zones);
        BuildAnchorPoints(zones);
        return zones;
    }

    private static Rect BuildDoorPocket(Bounds roomBounds, DoorInstance door, float depth)
    {
        var halfWidth = door.ClearWidthMeters * 0.5f + DefaultPocketMargin;
        if (door.Orientation == DoorOrientation.Horizontal)
        {
            var isNorth = Mathf.Abs(door.WorldPos.z - roomBounds.max.z) < Mathf.Abs(door.WorldPos.z - roomBounds.min.z);
            return isNorth
                ? Rect.MinMaxRect(door.WorldPos.x - halfWidth, roomBounds.max.z - depth, door.WorldPos.x + halfWidth, roomBounds.max.z)
                : Rect.MinMaxRect(door.WorldPos.x - halfWidth, roomBounds.min.z, door.WorldPos.x + halfWidth, roomBounds.min.z + depth);
        }

        var isEast = Mathf.Abs(door.WorldPos.x - roomBounds.max.x) < Mathf.Abs(door.WorldPos.x - roomBounds.min.x);
        return isEast
            ? Rect.MinMaxRect(roomBounds.max.x - depth, door.WorldPos.z - halfWidth, roomBounds.max.x, door.WorldPos.z + halfWidth)
            : Rect.MinMaxRect(roomBounds.min.x, door.WorldPos.z - halfWidth, roomBounds.min.x + depth, door.WorldPos.z + halfWidth);
    }

    private static bool TryBuildWindowStrip(RoomInstance room, float depth, out Rect strip)
    {
        strip = new Rect();
        if ((room.Tags & RoomTags.FacadeRoom) == 0)
        {
            return false;
        }

        var bounds = room.WorldBounds;
        switch (room.FacadeSide)
        {
            case RoomSide.North:
                strip = Rect.MinMaxRect(bounds.min.x, bounds.max.z - depth, bounds.max.x, bounds.max.z);
                return true;
            case RoomSide.South:
                strip = Rect.MinMaxRect(bounds.min.x, bounds.min.z, bounds.max.x, bounds.min.z + depth);
                return true;
            case RoomSide.East:
                strip = Rect.MinMaxRect(bounds.max.x - depth, bounds.min.z, bounds.max.x, bounds.max.z);
                return true;
            default:
                strip = Rect.MinMaxRect(bounds.min.x, bounds.min.z, bounds.min.x + depth, bounds.max.z);
                return true;
        }
    }

    private static void BuildPropZones(RoomZones zones)
    {
        var bounds = zones.RoomBoundsWorld;
        var main = Rect.MinMaxRect(bounds.min.x + 0.35f, bounds.min.z + 0.35f, bounds.max.x - 0.35f, bounds.max.z - 0.35f);
        zones.PropZonesWorldXZ.Clear();
        zones.PropZonesWorldXZ.Add(main);

        if (zones.WalkSpineWorldXZ.width > 0.01f && zones.WalkSpineWorldXZ.height > 0.01f)
        {
            SubtractRect(zones.PropZonesWorldXZ, zones.WalkSpineWorldXZ);
        }

        if (zones.HasWindowStrip)
        {
            SubtractRect(zones.PropZonesWorldXZ, zones.WindowStripWorldXZ);
        }

        for (var i = 0; i < zones.DoorPocketsWorldXZ.Count; i++)
        {
            SubtractRect(zones.PropZonesWorldXZ, zones.DoorPocketsWorldXZ[i]);
        }
    }

    private static void BuildAnchorPoints(RoomZones zones)
    {
        for (var i = 0; i < zones.PropZonesWorldXZ.Count; i++)
        {
            var rect = zones.PropZonesWorldXZ[i];
            zones.AnchorPoints.Add(new Vector3(rect.center.x, 0f, rect.center.y));
            zones.AnchorPoints.Add(new Vector3(rect.xMin + 0.5f, 0f, rect.yMin + 0.5f));
            zones.AnchorPoints.Add(new Vector3(rect.xMax - 0.5f, 0f, rect.yMax - 0.5f));
        }
    }

    private static void SubtractRect(List<Rect> source, Rect cut)
    {
        for (var i = source.Count - 1; i >= 0; i--)
        {
            var rect = source[i];
            if (!rect.Overlaps(cut))
            {
                continue;
            }

            source.RemoveAt(i);
            var left = Rect.MinMaxRect(rect.xMin, rect.yMin, Mathf.Min(cut.xMin, rect.xMax), rect.yMax);
            var right = Rect.MinMaxRect(Mathf.Max(cut.xMax, rect.xMin), rect.yMin, rect.xMax, rect.yMax);
            var bottom = Rect.MinMaxRect(Mathf.Max(rect.xMin, cut.xMin), rect.yMin, Mathf.Min(rect.xMax, cut.xMax), Mathf.Min(cut.yMin, rect.yMax));
            var top = Rect.MinMaxRect(Mathf.Max(rect.xMin, cut.xMin), Mathf.Max(cut.yMax, rect.yMin), Mathf.Min(rect.xMax, cut.xMax), rect.yMax);

            TryAdd(source, left);
            TryAdd(source, right);
            TryAdd(source, bottom);
            TryAdd(source, top);
        }
    }

    private static void TryAdd(List<Rect> rects, Rect rect)
    {
        if (rect.width <= 0.4f || rect.height <= 0.4f)
        {
            return;
        }

        rects.Add(rect);
    }
}
}
