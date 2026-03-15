using System;
using System.Collections.Generic;
using UnityEngine;

namespace MurinoHDR.Generation
{

[Serializable]
public sealed class RoomZones
{
    public int RoomId;
    public RoomType DetailedType;
    public Bounds RoomBoundsWorld;
    public readonly List<Rect> DoorPocketsWorldXZ = new List<Rect>();
    public Rect WindowStripWorldXZ;
    public bool HasWindowStrip;
    public Rect WalkSpineWorldXZ;
    public readonly List<Rect> PropZonesWorldXZ = new List<Rect>();
    public readonly List<Vector3> AnchorPoints = new List<Vector3>();
}
}
